
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleThirdPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform visualRoot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = -20f;

    [Header("Camera")]
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraTargetHeight = 1.3f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float initialCameraPitch = 15f;
    [SerializeField] private float minCameraPitch = -15f;
    [SerializeField] private float maxCameraPitch = 65f;

    private CharacterController controller;

    private float cameraYaw;
    private float cameraPitch;
    private float verticalVelocity;

    private bool cursorLocked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        cameraYaw = transform.eulerAngles.y;
        cameraPitch = Mathf.Clamp(
            initialCameraPitch,
            minCameraPitch,
            maxCameraPitch
        );
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        HandleCursorLock(keyboard, mouse);
        HandleCameraInput(mouse);

        Vector2 moveInput = GetMovementInput(keyboard);

        bool sprinting = keyboard != null &&
            (keyboard.leftShiftKey.isPressed ||
             keyboard.rightShiftKey.isPressed);

        MoveCharacter(moveInput, sprinting);
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
        UpdateBillboard();
    }

    private Vector2 GetMovementInput(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            input.y += 1f;

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            input.y -= 1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            input.x += 1f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            input.x -= 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void MoveCharacter(Vector2 input, bool sprinting)
    {
        // Movement is relative to the camera's horizontal direction.
        Quaternion cameraRotation = Quaternion.Euler(0f, cameraYaw, 0f);

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

    private void HandleCameraInput(Mouse mouse)
    {
        if (!cursorLocked || mouse == null)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        cameraYaw += mouseDelta.x * mouseSensitivity;
        cameraPitch -= mouseDelta.y * mouseSensitivity;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            minCameraPitch,
            maxCameraPitch
        );
    }

    private void UpdateCameraPosition()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Quaternion cameraRotation =
            Quaternion.Euler(cameraPitch, cameraYaw, 0f);

        Vector3 targetPosition =
            transform.position +
            Vector3.up * cameraTargetHeight;

        cameraTransform.rotation = cameraRotation;

        cameraTransform.position =
            targetPosition -
            cameraRotation * Vector3.forward * cameraDistance;
    }

    private void UpdateBillboard()
    {
        if (visualRoot == null || cameraTransform == null)
        {
            return;
        }

        // Rotate only around the vertical axis,
        // so the 2D sprite faces the camera without tilting.
        Vector3 direction =
            cameraTransform.position - visualRoot.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            visualRoot.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void HandleCursorLock(Keyboard keyboard, Mouse mouse)
    {
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }

        if (!cursorLocked &&
            mouse != null &&
            mouse.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }
}
