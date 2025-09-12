using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEditor;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using OpenAI.Chat;
using OpenAI.Audio;
using OpenAI;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Handles automatic transcription and translation of video clips into caption files.
/// </summary>
/// <remarks>
/// Workflow:
/// 1. When a <see cref="VideoPlayer"/> finishes preparing, this script:
///    - Extracts the audio track from the video using FFmpeg.
///    - Sends the audio to OpenAI Whisper for transcription (with timestamps).
///    - Saves the transcription to disk as a text file (per language).
/// 2. The original transcript is saved in the first language defined in <see cref="Manager_VideoPlayer.languages"/>.
/// 3. Additional translations are generated via GPT-4 and written into language-specific folders.
/// 
/// Required setup in Unity:
/// - Attach this component to a GameObject.
/// - Assign <see cref="videoPlayer"/> and <see cref="videoPlayerManager"/>.
/// - Set <see cref="directory"/> to the root path where transcript files will be written.
/// - Provide a valid <see cref="openAIKey"/> for API calls.
/// - Ensure FFmpeg is installed and accessible at <see cref="ffmpegPath"/>.
/// </remarks>
public class GenerateCaption : MonoBehaviour
{
    /// <summary>
    /// Unity VideoPlayer instance to transcribe/translate captions for.
    /// </summary>
    public VideoPlayer videoPlayer;

    /// <summary>
    /// Manager containing supported languages and caption utilities.
    /// </summary>
    public Manager_VideoPlayer videoPlayerManager;

    /// <summary>
    /// Root directory where transcript files will be written.
    /// Example: "Assets/_MITmech/Resources/Instruction Guides/Videos/NewClips/Transcripts/"
    /// </summary>
    public string directory;

    /// <summary>
    /// OpenAI API key (required for transcription/translation).
    /// </summary>
    [SerializeField] private string openAIKey; // leave empty to ignore api key and prevent error

    [Header("FFmpeg Settings")]
    /// <summary>
    /// Path to the FFmpeg executable (e.g., "ffmpeg" if in PATH).
    /// </summary>
    [SerializeField] private string ffmpegPath; // leave empty to ignore ffmpeg and prevent error

    private VideoClip lastClip;
    private List<float> recordedSamples = new List<float>();
    private string outputPath;
    private string ogTranscript;

    private void Start()
    {
        videoPlayer.prepareCompleted += OnVideoPrepared;

        // Default transcript path for the first language
        ogTranscript = directory + videoPlayerManager.languages[0] + "/" + videoPlayer.clip.name + ".txt";
    }

    /// <summary>
    /// Callback invoked when the video finishes preparing.
    /// Ensures transcript exists or generates one, then begins transcription/translation.
    /// </summary>
    private void OnVideoPrepared(VideoPlayer source)
    {
        // Begin transcription from generated MP3
        if (ffmpegPath != "" && !File.Exists(directory + videoPlayerManager.languages[0] + "/" + videoPlayer.clip.name + ".txt"))
        {
            ConvertToAudio(videoPlayer.clip);
        }   
        TranscribeAudio(Path.ChangeExtension(AssetDatabase.GetAssetPath(videoPlayer.clip), ".mp3"));

    }

    /// <summary>
    /// Writes transcripts for all languages defined in <see cref="Manager_VideoPlayer"/>.
    /// If the language is the first one, the original transcript is written directly.
    /// Otherwise, translation is requested via <see cref="TranslateAndWriteTo"/>.
    /// </summary>
    /// <param name="transcript">The original transcript text.</param>
    private void GenerateTextFiles(string transcript)
    {
        for (int i = 0; i < videoPlayerManager.languages.Length; i++)
        {
            string language = videoPlayerManager.languages[i];
            string langDirectory = directory + language + "/";
            string fileName = videoPlayer.clip.name + ".txt";
            string path = langDirectory + fileName;

            if (!Directory.Exists(langDirectory))
            {
                Directory.CreateDirectory(langDirectory);
            }

            if (File.Exists(path))
            {
                Debug.LogWarning("File already exists: " + path);
            }
            else
            {
                if (i == 0)
                {
                    File.WriteAllText(path, transcript);
                    Debug.Log("Transcript saved to: " + path);
                    AssetDatabase.Refresh();
                }
                else
                {
                    TranslateAndWriteTo(language, transcript, path);
                }
            }
        }
    }

    /// <summary>
    /// Uses GPT to translate a transcript into the target language and write it to a file.
    /// </summary>
    /// <param name="language">Target language for translation.</param>
    /// <param name="transcript">Original transcript text.</param>
    /// <param name="path">Destination file path.</param>
    private async void TranslateAndWriteTo(string language, string transcript, string path)
    {
        var client = new OpenAIClient(openAIKey);

        var request = new ChatRequest(
            model: "gpt-4o-mini",
            messages: new[]
            {
                new Message(Role.System, "You are a translation assistant."),
                new Message(Role.User, $"Translate the following timestamped transcript into {language}. " +
                                       $"Return nothing else except the translated transcript, preserving all timestamps:\n\n{transcript}")
            },
            temperature: 0
        );

        var response = await client.ChatEndpoint.GetCompletionAsync(request);
        File.WriteAllText(path, response.Choices[0].Message);
        Debug.Log(language + " transcript saved to: " + path);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Extracts audio from a video clip and saves it as an MP3 using FFmpeg.
    /// </summary>
    /// <param name="clip">The video clip to extract audio from.</param>
    public void ConvertToAudio(VideoClip clip)
    {
        Debug.Log("Generating Transcript");
        if (clip == null)
        {
            Debug.LogError("No VideoClip assigned!");
            return;
        }

        string videoPath = AssetDatabase.GetAssetPath(clip);

        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError("Could not resolve path for video clip: " + clip.name);
            return;
        }

        string outputAudioPath = Path.ChangeExtension(videoPath, ".mp3");
        outputPath = outputAudioPath;

        string arguments = $"-i \"{videoPath}\" -vn -acodec libmp3lame -q:a 2 \"{outputAudioPath}\"";

        try
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.Log(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.LogWarning(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            Debug.Log($"Audio extracted to: {outputAudioPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("FFmpeg extraction failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Transcribes an audio file using OpenAI Whisper and generates language-specific caption files.
    /// </summary>
    /// <param name="audioPath">Path to the audio file.</param>
    private async void TranscribeAudio(string audioPath)
    {
        try
        {
            string transcript = "";

            if (!File.Exists(ogTranscript))
            {
                Debug.Log(ogTranscript);
                var client = new OpenAIClient(openAIKey);

                var request = new AudioTranscriptionRequest(
                    audioPath: audioPath,
                    model: "whisper-1",
                    prompt: null,
                    responseFormat: AudioResponseFormat.Verbose_Json, // includes timestamps
                    temperature: null,
                    language: null,
                    timestampGranularity: TimestampGranularity.Segment
                );

                AudioResponse transcription = await client.AudioEndpoint.CreateTranscriptionJsonAsync(request);

                Directory.CreateDirectory(directory);

                var sb = new StringBuilder();

                if (transcription.Segments != null && transcription.Segments.Length > 0)
                {
                    foreach (var segment in transcription.Segments)
                    {
                        sb.AppendLine($"{segment.Start:0.00}::{segment.End:0.00}::{segment.Text}");
                    }
                }
                else
                {
                    sb.AppendLine(transcription.Text);
                }
                transcript = sb.ToString();
            }
            else
            {
                transcript = File.ReadAllText(ogTranscript);
            }

            GenerateTextFiles(transcript);
            videoPlayerManager.GenerateCaptionMap();

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif  
            string videoPath = AssetDatabase.GetAssetPath(videoPlayer.clip);
            File.Delete(Path.ChangeExtension(videoPath, ".mp3"));
            File.Delete(Path.ChangeExtension(videoPath, ".mp3.meta"));
        }
        catch (Exception ex)
        {
            Debug.LogError("Transcription failed: " + ex.Message);
        }
    }
}
