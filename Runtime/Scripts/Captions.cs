using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utilities.Extensions; // (Assumed for possible extension methods on TMP)

/**
 * <summary>
 * Handles displaying captions on a TextMeshProUGUI component.  
 * Uses a timestamp-to-text dictionary (caption map) to set the text
 * according to the current playback time in seconds.
 * </summary>
 */
public class Captions : MonoBehaviour
{
    /// <summary>
    /// Reference to the TextMeshProUGUI component that displays captions.
    /// This must be assigned in the Unity Inspector.
    /// </summary>
    public TextMeshProUGUI text;

    /// <summary>
    /// A dictionary mapping timestamps (in whole seconds) to caption text.
    /// Example: { 0 → "Intro", 5 → "Hello World", 10 → "Next line" }
    /// </summary>
    private Dictionary<int, string> captionMap;

    /// <summary>
    /// Updates the displayed caption based on the provided timestamp.
    /// </summary>
    /// <param name="seconds">The current playback time in seconds.</param>
    /// <remarks>
    /// - If a caption exists for the given time, it is shown.  
    /// - If no caption exists, the text is cleared.  
    /// - If no caption map is set, "N/A" is displayed.  
    /// </remarks>
    public void UpdateText(int seconds)
    {
        if (captionMap != null && captionMap.Count > 0)
        {
            if (captionMap.TryGetValue(seconds, out string value))
            {
                text.SetText(value);
            }
            else
            {
                text.SetText("");
            }
        }
        else
        {
            text.SetText("N/A");
        }
    }

    /// <summary>
    /// Assigns a new caption map to this component.
    /// </summary>
    /// <param name="map">A dictionary mapping seconds to caption text.</param>
    /// <remarks>
    /// This should typically be called once after captions are generated or loaded.
    /// </remarks>
    public void SetCaptionMap(Dictionary<int, string> map)
    {
        captionMap = map;
    }
}