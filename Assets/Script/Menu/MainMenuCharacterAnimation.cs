using System.Collections;
using UnityEngine;


public class MainMenuCharacterAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation States")]
    [SerializeField] private string walkRightState = "WalkRight";
    [SerializeField] private string walkLeftState = "WalkLeft";
    [SerializeField] private string walkForwardState = "WalkForward";
    [SerializeField] private string walkBackwardState = "WalkBackward";

    [Header("Movement Durations")]
    [SerializeField, Min(0f)] private float walkRightDuration = 8f;
    [SerializeField, Min(0f)] private float walkLeftDuration = 3f;
    [SerializeField, Min(0f)] private float walkForwardDuration = 6f;
    [SerializeField, Min(0f)] private float finalWalkLeftDuration = 2f;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.2f;

    public bool IsWalking { get; private set; }

    private Coroutine playRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("CharacterAnimation: Animator not found.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        playRoutine = StartCoroutine(PlayLoop());
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        IsWalking = false;
    }

    private IEnumerator PlayLoop()
    {
        while (true)
        {
            // 0–8 секунд — вправо
            yield return PlayMovement(
                walkRightState,
                walkRightDuration
            );

            // 8–11 секунд — влево
            yield return PlayMovement(
                walkLeftState,
                walkLeftDuration
            );

            // 11–17 секунд — вперёд
            yield return PlayMovement(
                walkForwardState,
                walkForwardDuration
            );

            // 17–19 секунд — влево
            yield return PlayMovement(
                walkLeftState,
                finalWalkLeftDuration
            );
        }
    }

    private IEnumerator PlayMovement(string stateName, float duration)
    {
        IsWalking = true;

        bool forceTurnLeft =
            stateName == walkLeftState &&
            Mathf.Approximately(duration, finalWalkLeftDuration);

        Transform characterModel = animator.transform;

        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (forceTurnLeft)
            {
                Vector3 rotation = characterModel.localEulerAngles;
                rotation.y = 180f;
                characterModel.localEulerAngles = rotation;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}