
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInputComponent = GardenKeeper.Inputs.PlayerInput;

[RequireComponent(
    typeof(CharacterController),
    typeof(PlayerInputComponent),
    typeof(PlayerCameraController)
)]
public class SimpleThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private PlayerCameraController cameraController;
    private float verticalVelocity;

    private PlayerInputComponent playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraController = GetComponent<PlayerCameraController>();
    }

    private void Start()
    {
        playerInput = GetComponent<PlayerInputComponent>();
        moveAction = playerInput.Move;
        sprintAction = playerInput.Sprint;
    }

    private void Update()
    {
        Vector2 moveInput = moveAction != null
            ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f)
            : Vector2.zero;

        bool sprinting = sprintAction != null && sprintAction.IsPressed();

        MoveCharacter(moveInput, sprinting);
    }

    private void MoveCharacter(Vector2 input, bool sprinting)
    {
        // Movement is relative to the camera's horizontal direction.
        Quaternion cameraRotation = Quaternion.Euler(
            0f,
            cameraController.CameraYaw,
            0f
        );

        Vector3 forward = cameraRotation * Vector3.forward;
        Vector3 right = cameraRotation * Vector3.right;

        Vector3 moveDirection =
            forward * input.y +
            right * input.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Keep the character on the ground and apply gravity.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        float currentSpeed = sprinting ? sprintSpeed : walkSpeed;

        Vector3 velocity =
            moveDirection * currentSpeed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // Rotate the character toward the direction of movement.
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

}
