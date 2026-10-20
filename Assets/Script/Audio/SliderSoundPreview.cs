
using UnityEngine;
using UnityEngine.EventSystems;

public class SliderSoundPreview : MonoBehaviour, IPointerUpHandler
{
    public enum VolumeType
    {
        Master,
        Music,
        SFX
    }

    [SerializeField] private AudioSettingsUI audioSettings;
    [SerializeField] private VolumeType volumeType;

    public void OnPointerUp(PointerEventData eventData)
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                audioSettings.PreviewMasterVolume();
                break;

            case VolumeType.Music:
                audioSettings.PreviewMusicVolume();
                break;

            case VolumeType.SFX:
                audioSettings.PreviewSfxVolume();
                break;
        }

        audioSettings.SaveSettings();
    }
}
