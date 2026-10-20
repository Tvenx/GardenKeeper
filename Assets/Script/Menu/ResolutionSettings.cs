using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResolutionSettings : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private GameObject fullscreenCheckmark;

    private readonly List<Resolution> availableResolutions = new();
    private bool selectedFullscreen;

    private const string ResolutionWidthKey = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";
    private const string FullscreenKey = "Fullscreen";

    private void Start()
    {
        LoadResolutions();
        LoadSettings();
    }

    private void LoadResolutions()
    {
        availableResolutions.Clear();

        var options = new List<string>();
        var seenResolutions = new HashSet<string>();

        foreach (Resolution resolution in Screen.resolutions)
        {
            string key = $"{resolution.width}x{resolution.height}";
            if (!seenResolutions.Add(key))
                continue;

            availableResolutions.Add(resolution);
            options.Add($"{resolution.width} x {resolution.height}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadSettings()
    {
        selectedFullscreen = PlayerPrefs.GetInt(
            FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        fullscreenCheckmark.SetActive(selectedFullscreen);

        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);

        int index = availableResolutions.FindIndex(r =>
            r.width == savedWidth && r.height == savedHeight);

        if (index < 0)
        {
            index = availableResolutions.FindIndex(r =>
                r.width == Screen.width && r.height == Screen.height);
        }

        if (index < 0 && availableResolutions.Count > 0)
            index = 0;

        if (index >= 0)
        {
            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
            ApplyCurrentSettings();
        }
        else
        {
            Screen.fullScreen = selectedFullscreen;
        }
    }

    // Called by the Full Screen button. The selection is applied with the Apply button.
    public void fullScreen()
    {
        selectedFullscreen = !selectedFullscreen;
        fullscreenCheckmark.SetActive(selectedFullscreen);
    }

    // Kept for any existing Unity event bindings that still reference the previous method name.
    public void SetFullscreen(bool isFullscreen)
    {
        selectedFullscreen = isFullscreen;
        fullscreenCheckmark.SetActive(selectedFullscreen);
    }

    public void ApplySettings()
    {
        int index = resolutionDropdown.value;

        if (index >= 0 && index < availableResolutions.Count)
        {
            Resolution resolution = availableResolutions[index];
            Screen.SetResolution(
                resolution.width,
                resolution.height,
                selectedFullscreen);

            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
        }
        else
        {
            Screen.fullScreen = selectedFullscreen;
        }

        PlayerPrefs.SetInt(FullscreenKey, selectedFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyCurrentSettings()
    {
        int index = resolutionDropdown.value;
        if (index < 0 || index >= availableResolutions.Count)
            return;

        Resolution resolution = availableResolutions[index];
        Screen.SetResolution(
            resolution.width,
            resolution.height,
            selectedFullscreen);
    }
}
