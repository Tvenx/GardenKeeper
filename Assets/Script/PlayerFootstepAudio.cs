using UnityEngine;
using PlayerInputComponent = GardenKeeper.Inputs.PlayerInput;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip sprintClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float minimumMovementSpeed = 0.1f;

    private CharacterController characterController;
    private AudioSource audioSource;
    private PlayerInputComponent playerInput;
    private AudioClip activeClip;
    private bool warnedMissingSprintClip;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInputComponent>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;

        if (walkClip == null)
        {
            Debug.LogError(
                "PlayerFootstepAudio: Walking clip is not assigned.",
                this
            );
        }
    }

    private void Update()
    {
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(
            characterController.velocity,
            Vector3.up
        );

        if (!characterController.isGrounded ||
            horizontalVelocity.magnitude < minimumMovementSpeed)
        {
            StopLoop();
            return;
        }

        bool sprinting = playerInput != null &&
                         playerInput.Sprint != null &&
                         playerInput.Sprint.IsPressed();
        AudioClip desiredClip = sprinting ? sprintClip : walkClip;

        if (sprinting && sprintClip == null && !warnedMissingSprintClip)
        {
            Debug.LogWarning(
                "PlayerFootstepAudio: Sprint clip is not assigned; using walking clip.",
                this
            );
            warnedMissingSprintClip = true;
            desiredClip = walkClip;
        }

        if (desiredClip == null)
        {
            StopLoop();
            return;
        }

        if (activeClip != desiredClip || !audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = desiredClip;
            audioSource.volume = volume;
            audioSource.loop = true;
            audioSource.Play();
            activeClip = desiredClip;
        }
    }

    private void StopLoop()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        activeClip = null;
    }
}
