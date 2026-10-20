
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionSettings : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private readonly List<Resolution> availableResolutions = new();

    private const string ResolutionWidthKey = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";
    private const string FullscreenKey = "Fullscreen";

    private void Start()
    {
        LoadResolutions();
        LoadSettings();

        resolutionDropdown.onValueChanged.AddListener(ApplyResolution);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    private void LoadResolutions()
    {
        availableResolutions.Clear();

        var options = new List<string>();
        Resolution[] resolutions = Screen.resolutions;

        foreach (Resolution resolution in resolutions)
        {
            string option = $"{resolution.width} x {resolution.height}";

            // Не добавляем одинаковые разрешения повторно.
            if (options.Contains(option))
                continue;

            options.Add(option);
            availableResolutions.Add(resolution);
        }

        // Заполняем существующий Dropdown.
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadSettings()
    {
        bool fullscreen = PlayerPrefs.GetInt(
            FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        int savedWidth = PlayerPrefs.GetInt(
            ResolutionWidthKey, Screen.width);

        int savedHeight = PlayerPrefs.GetInt(
            ResolutionHeightKey, Screen.height);

        int index = availableResolutions.FindIndex(r =>
            r.width == savedWidth && r.height == savedHeight);

        if (index < 0)
        {
            index = availableResolutions.FindIndex(r =>
                r.width == Screen.width && r.height == Screen.height);
        }

        if (index < 0 && availableResolutions.Count > 0)
            index = availableResolutions.Count - 1;

        if (index >= 0)
        {
            resolutionDropdown.SetValueWithoutNotify(index);
            ApplyResolution(index);
        }
    }

    public void ApplyResolution(int index)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return;

        Resolution resolution = availableResolutions[index];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            fullscreenToggle.isOn);

        PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(
            FullscreenKey, isFullscreen ? 1 : 0);

        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(ApplyResolution);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
    }
}
