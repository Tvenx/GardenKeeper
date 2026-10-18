using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInputComponent = GardenKeeper.Inputs.PlayerInput;

public class PlayerMovementAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation States")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkForwardState = "WalkForward";
    [SerializeField] private string walkForwardRightState = "WalkForwardRight";
    [SerializeField] private string walkRightState = "WalkRight";
    [SerializeField] private string walkBackwardRightState = "WalkBackwardRight";
    [SerializeField] private string walkBackwardState = "WalkBackward";
    [SerializeField] private string walkBackwardLeftState = "WalkBackwardLeft";
    [SerializeField] private string walkLeftState = "WalkLeft";
    [SerializeField] private string walkForwardLeftState = "WalkForwardLeft";

    [Min(0f)]
    [SerializeField] private float transitionDuration = 0.1f;

    private PlayerInputComponent playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;
    private string[] directionStates;
    private string currentState;
    private float defaultAnimatorSpeed = 1f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInputComponent>();

        if (playerInput == null)
        {
            Debug.LogError(
                "PlayerMovementAnimation: PlayerInput component is missing.",
                this
            );
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError(
                "PlayerMovementAnimation: Animator component is missing.",
                this
            );
        }
        else
        {
            defaultAnimatorSpeed = animator.speed;
        }

        directionStates = new[]
        {
            walkForwardState,
            walkForwardRightState,
            walkRightState,
            walkBackwardRightState,
            walkBackwardState,
            walkBackwardLeftState,
            walkLeftState,
            walkForwardLeftState
        };
    }

    private void Start()
    {
        if (playerInput != null)
        {
            moveAction = playerInput.Move;
            sprintAction = playerInput.Sprint;
        }
    }

    private void Update()
    {
        if (animator == null || moveAction == null)
        {
            return;
        }

        Vector2 input = Vector2.ClampMagnitude(
            moveAction.ReadValue<Vector2>(),
            1f
        );

        bool sprinting = input.sqrMagnitude >= 0.01f &&
                         sprintAction != null &&
                         sprintAction.IsPressed();
        animator.speed = defaultAnimatorSpeed * (sprinting ? 2f : 1f);

        if (input.sqrMagnitude < 0.01f)
        {
            PlayState(idleState);
            return;
        }

        int directionIndex = Mathf.RoundToInt(
            Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg / 45f
        );

        directionIndex = (directionIndex % 8 + 8) % 8;
        PlayState(directionStates[directionIndex]);
    }

    private void PlayState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || currentState == stateName)
        {
            return;
        }

        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration,
            0
        );

        currentState = stateName;
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.speed = defaultAnimatorSpeed;
        }
    }
}
