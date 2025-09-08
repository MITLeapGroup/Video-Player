using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/**
 * <summary>
 * Manages a Unity VideoPlayer with support for:
 * - Scrubbing via a ProgressBar
 * - Play, pause, mute, and playback speed control
 * - Timestamp display
 * - Multi-language captions (using external transcript files)
 * </summary>
 */
public class Manager_VideoPlayer : MonoBehaviour
{
    // -------------------- INSPECTOR FIELDS --------------------

    [Header("Video Player Settings")]
    [Tooltip("Number of seconds to skip forward/backward when scrubbing.")]
    public float scrubSeconds = 5.0f;

    [Tooltip("Reference to the custom ProgressBar UI.")]
    public ProgressBar videoProgress;

    [Header("Video Player")]
    [Tooltip("Unity VideoPlayer component for playback.")]
    public VideoPlayer videoPlayer;

    [Tooltip("RawImage where the video is displayed.")]
    public RawImage videoImage;

    [Tooltip("Optional list of videos to manage.")]
    public ArrayList videoList;

    [Tooltip("UI text showing the current time vs. total video length.")]
    public TextMeshProUGUI timestamp;

    [Header("Playback Speed")]
    [Tooltip("Dropdown UI for selecting playback speed.")]
    public TMP_Dropdown speedDropdown;

    [Tooltip("Playback speeds available in the dropdown.")]
    public float[] speedValues = { 0.5f, 0.75f, 1f, 1.5f, 2f };

    [Header("Captions")]
    [Tooltip("Dropdown UI for selecting caption language.")]
    public TMP_Dropdown captionDropdown;

    [Tooltip("Languages supported for captions. Index 0 = original language.")]
    public string[] languages = { "English" };

    [Tooltip("All caption displays that should be updated (supports multiple).")]
    public Captions[] captions;

    [Tooltip("Reference to the caption generator that handles transcript files.")]
    public GenerateCaption captionGenerator;

    // -------------------- PRIVATE STATE --------------------

    /// <summary>
    /// Expanded list of language options including "off" for disabling captions.
    /// </summary>
    private string[] languageOptions;

    /// <summary>
    /// Currently selected caption language.
    /// </summary>
    private string language;

    private bool pausedBeforeDrag = true;
    private bool dragging = false;

    // -------------------- UNITY LIFECYCLE --------------------

    private void Start()
    {
        language = languages[0];

        // Listen for when the video is fully prepared (metadata loaded, duration available).
        videoPlayer.prepareCompleted += OnVideoPrepared;

        SetupSpeedDropdown();
        SetupCaptionDropdown();
    }

    private void Update()
    {
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
            {
                UpdateProgressBar();
            }

            UpdateTimestamp();

            if (captions.Length > 0)
            {
                // Update each caption with the current playback time.
                foreach (Captions caption in captions)
                {
                    caption.UpdateText((int)videoPlayer.time);
                }
            }
        }
    }

    // -------------------- VIDEO PLAYER EVENTS --------------------

    /// <summary>
    /// Called when the video finishes preparing.
    /// Used to generate captions for the current clip.
    /// </summary>
    private void OnVideoPrepared(VideoPlayer source)
    {
        GenerateCaptionMap();
    }

    // -------------------- UI SETUP --------------------

    /// <summary>
    /// Populates and wires up the playback speed dropdown.
    /// </summary>
    private void SetupSpeedDropdown()
    {
        if (speedDropdown == null) return;

        int? oneIndex = null;
        var optionData = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < speedValues.Length; i++)
        {
            float speed = speedValues[i];
            optionData.Add(new TMP_Dropdown.OptionData(speed + "x"));
            if (Math.Abs(speed - 1f) < 0.001f)
            {
                oneIndex = i;
            }
        }

        speedDropdown.AddOptions(optionData);
        if (oneIndex != null) speedDropdown.value = oneIndex.Value;

        speedDropdown.onValueChanged.AddListener(OnSpeedDropdownValueChanged);
    }

    /// <summary>
    /// Populates and wires up the caption language dropdown.
    /// </summary>
    private void SetupCaptionDropdown()
    {
        if (captionDropdown == null) return;

        // Add 'off' option
        languageOptions = new string[languages.Length + 1];
        Array.Copy(languages, languageOptions, languages.Length);
        languageOptions[languageOptions.Length - 1] = "off";

        var optionData = new List<TMP_Dropdown.OptionData>();
        foreach (string lang in languageOptions)
        {
            optionData.Add(new TMP_Dropdown.OptionData(lang));
        }

        captionDropdown.AddOptions(optionData);
        captionDropdown.onValueChanged.AddListener(OnLanguageDropdownValueChanged);
    }

    // -------------------- VIDEO CONTROL --------------------

    /// <summary>Plays the video.</summary>
    public void PlayVideo() => videoPlayer.Play();

    /// <summary>Pauses the video.</summary>
    public void PauseVideo() => videoPlayer.Pause();

    /// <summary>Skips forward by <see cref="scrubSeconds"/>.</summary>
    public void VideoForward()
    {
        videoPlayer.time += scrubSeconds;
        UpdateProgressBar();
    }

    /// <summary>Skips backward by <see cref="scrubSeconds"/>.</summary>
    public void VideoBackward()
    {
        videoPlayer.time -= scrubSeconds;
        UpdateProgressBar();
    }

    /// <summary>Sets the video volume.</summary>
    public void SetVolume(float volume) => videoPlayer.SetDirectAudioVolume(0, volume);

    /// <summary>Mutes or unmutes the video audio.</summary>
    public void Mute(bool state) => videoPlayer.SetDirectAudioMute(0, state);

    // -------------------- PROGRESS BAR --------------------

    /// <summary>
    /// Updates the progress bar UI to reflect the current playback position.
    /// </summary>
    public void UpdateProgressBar()
    {
        videoProgress.SetBarProgress((float)videoPlayer.time / (float)videoPlayer.length);
    }

    /// <summary>
    /// Called when the user clicks down on the progress bar.
    /// Pauses playback (if it wasn’t already paused) and seeks to position.
    /// </summary>
    public void onProgressBarDown(float normalizedPos)
    {
        pausedBeforeDrag = !videoPlayer.isPlaying;
        dragging = true;
        videoPlayer.Pause();

        UpdateVideoPosition(normalizedPos);
    }

    /// <summary>
    /// Called while the user drags the progress bar.
    /// Updates video position without resuming playback.
    /// </summary>
    public void onProgressBarDrag(float normalizedPos) => UpdateVideoPosition(normalizedPos);

    /// <summary>
    /// Called when the user releases the progress bar.
    /// Resumes playback if it was playing before scrubbing.
    /// </summary>
    public void onProgressBarUp(float normalizedPos)
    {
        if (!pausedBeforeDrag) videoPlayer.Play();
        dragging = false;
        UpdateVideoPosition(normalizedPos);
    }

    /// <summary>
    /// Sets the current playback time from a normalized value (0–1).
    /// </summary>
    public void UpdateVideoPosition(float normalizedTime)
    {
        videoPlayer.time = normalizedTime * videoPlayer.length;
    }

    // -------------------- TIMESTAMP --------------------

    /// <summary>
    /// Updates the timestamp UI with the current time vs. total video length.
    /// </summary>
    public void UpdateTimestamp()
    {
        TimeSpan totalTime = TimeSpan.FromSeconds(videoPlayer.length);
        TimeSpan currentTime = TimeSpan.FromSeconds(videoPlayer.time);

        timestamp.text =
            $"{currentTime.Minutes:D1}:{currentTime.Seconds:D2} / " +
            $"{totalTime.Minutes:D1}:{totalTime.Seconds:D2}";
    }

    // -------------------- DROPDOWN CALLBACKS --------------------

    /// <summary>
    /// Called when playback speed is changed in the dropdown.
    /// </summary>
    private void OnSpeedDropdownValueChanged(int index)
    {
        videoPlayer.playbackSpeed = speedValues[index];
    }

    /// <summary>
    /// Called when caption language is changed in the dropdown.
    /// Toggles caption visibility and reloads transcript.
    /// </summary>
    private void OnLanguageDropdownValueChanged(int index)
    {
        language = languageOptions[index];

        foreach (Captions caption in captions)
        {
            caption.gameObject.SetActive(language != "off");
        }

        GenerateCaptionMap();
    }

    // -------------------- CAPTIONS --------------------

    /**
     * <summary>
     * Generates a caption map for the current video.
     * Loads transcript files (with timestamps) and builds a dictionary mapping
     * every second of video playback to the correct caption text.
     * </summary>
     */
    public void GenerateCaptionMap()
    {
        if (captionGenerator == null) return;

        string langDirectory = captionGenerator.directory + language + "/";
        string transcriptPath = langDirectory + videoPlayer.clip.name + ".txt";

        if (!File.Exists(transcriptPath)) return;

        Debug.Log(videoPlayer.url);

        // Load transcript lines and split into [start, end, text]
        List<string[]> splitList = new List<string[]>();
        foreach (string line in File.ReadAllLines(transcriptPath))
        {
            splitList.Add(line.Split(new string[] { "::" }, StringSplitOptions.None));
        }

        // Build dictionary of captions (per second)
        Dictionary<int, string> captionMap = new Dictionary<int, string>();
        foreach (string[] arrSeg in splitList)
        {
            int start = (int)float.Parse(arrSeg[0]);
            int end = (int)float.Parse(arrSeg[1]);
            string text = arrSeg[2];

            for (int i = start; i < end; i++)
            {
                if (!captionMap.ContainsKey(i))
                {
                    captionMap.Add(i, text);
                }
            }
        }

        // Assign to all caption components
        foreach (Captions caption in captions)
        {
            caption.SetCaptionMap(captionMap);
        }
    }
}