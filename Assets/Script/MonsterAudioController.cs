using UnityEngine;

public class MonsterAudioController : MonoBehaviour
{
    [Header("Player-Facing Scare Sound")]
    [SerializeField] private AudioSource scareAudioSource;
    [SerializeField] private AudioClip scareSound;

    [Range(0f, 1f)]
    [SerializeField] private float scareVolume = 1f;

    [Header("Monster World Audio")]
    [SerializeField] private AudioSource worldAudioSource;

    private void Awake()
    {
        if (scareAudioSource == null)
        {
            GameObject sourceObject =
                new GameObject("Scare Audio Source");

            sourceObject.transform.SetParent(transform, false);

            scareAudioSource =
                sourceObject.AddComponent<AudioSource>();
        }

        if (worldAudioSource == null)
        {
            worldAudioSource = GetComponent<AudioSource>();
        }

        if (worldAudioSource == null)
        {
            worldAudioSource = gameObject.AddComponent<AudioSource>();
        }

        scareAudioSource.playOnAwake = false;
        scareAudioSource.spatialBlend = 0f;

        worldAudioSource.playOnAwake = false;
        worldAudioSource.spatialBlend = 1f;
        worldAudioSource.dopplerLevel = 0f;
    }

    public void PlayScareSound()
    {
        if (scareAudioSource == null || scareSound == null)
        {
            return;
        }

        scareAudioSource.PlayOneShot(scareSound, scareVolume);
    }

    public void PlayMonsterSound(AudioClip clip, float volume = 1f)
    {
        if (worldAudioSource == null || clip == null)
        {
            return;
        }

        worldAudioSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volume)
        );
    }

    public void StopAllSounds()
    {
        if (scareAudioSource != null)
        {
            scareAudioSource.Stop();
        }

        if (worldAudioSource != null)
        {
            worldAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        StopAllSounds();
    }
}