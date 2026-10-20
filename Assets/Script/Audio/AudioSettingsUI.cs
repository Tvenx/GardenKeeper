
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Volume Text")]
    [SerializeField] private TMP_Text masterVolumeText;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;

    [Header("Preview Sound")]
    [SerializeField] private AudioSource previewAudioSource;
    [SerializeField] private AudioClip previewClip;

    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SFXVolume";

    private void Start()
    {
        SetupSlider(masterSlider, MasterKey);
        SetupSlider(musicSlider, MusicKey);
        SetupSlider(sfxSlider, SfxKey);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSfxVolume(sfxSlider.value);

        UpdateVolumeTexts();
    }

    private void SetupSlider(Slider slider, string key)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = PlayerPrefs.GetFloat(key, 1f);
    }

    private void SetMasterVolume(float value)
    {
        SetVolume("MasterVolume", value);
        PlayerPrefs.SetFloat(MasterKey, value);
        UpdateVolumeTexts();
    }

    private void SetMusicVolume(float value)
    {
        SetVolume("MusicVolume", value);
        PlayerPrefs.SetFloat(MusicKey, value);
        UpdateVolumeTexts();
    }

    private void SetSfxVolume(float value)
    {
        SetVolume("SFXVolume", value);
        PlayerPrefs.SetFloat(SfxKey, value);
        UpdateVolumeTexts();
    }

    private void SetVolume(string parameter, float value)
    {
        float decibels = value <= 0f
            ? -80f
            : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(parameter, decibels);
    }

    private void UpdateVolumeTexts()
    {
        masterVolumeText.text = $"{Mathf.RoundToInt(masterSlider.value * 100)}%";
        musicVolumeText.text = $"{Mathf.RoundToInt(musicSlider.value * 100)}%";
        sfxVolumeText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100)}%";
    }

    public void PreviewMasterVolume()
    {
        PlayPreview(masterGroup);
    }

    public void PreviewMusicVolume()
    {
        PlayPreview(musicGroup);
    }

    public void PreviewSfxVolume()
    {
        PlayPreview(sfxGroup);
    }

    private void PlayPreview(AudioMixerGroup outputGroup)
    {
        if (previewAudioSource == null || previewClip == null)
            return;

        previewAudioSource.outputAudioMixerGroup = outputGroup;
        previewAudioSource.PlayOneShot(previewClip);
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }
}
