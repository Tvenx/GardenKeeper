
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using PlayerInputComponent = GardenKeeper.Inputs.PlayerInput;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputComponent))]
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
    [SerializeField] private float cameraHorizontalOffset;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float gamepadLookSensitivity = 180f;
    [SerializeField] private float initialCameraPitch = 15f;
    [SerializeField] private float minCameraPitch = -15f;
    [SerializeField] private float maxCameraPitch = 65f;

    private CharacterController controller;

    private float cameraYaw;
    private float cameraPitch;
    private float verticalVelocity;

    private bool cursorLocked;

    private PlayerInputComponent playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction cancelAction;
    private InputAction cursorRelockClickAction;
    private InputAction cursorRelockSubmitAction;

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
        playerInput = GetComponent<PlayerInputComponent>();
        moveAction = playerInput.Move;
        lookAction = playerInput.Look;
        sprintAction = playerInput.Sprint;
        cancelAction = playerInput.Cancel;
        cursorRelockClickAction = playerInput.CursorRelockClick;
        cursorRelockSubmitAction = playerInput.CursorRelockSubmit;

        SetCursorLocked(true);
    }

    private void Update()
    {
        HandleCursorLock();
        HandleCameraInput();

        Vector2 moveInput = moveAction != null
            ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f)
            : Vector2.zero;

        bool sprinting = sprintAction != null && sprintAction.IsPressed();

        MoveCharacter(moveInput, sprinting);
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
        UpdateBillboard();
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

    private void HandleCameraInput()
    {
        if (!cursorLocked || lookAction == null)
        {
            return;
        }

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float sensitivity = lookAction.activeControl is DeltaControl
            ? mouseSensitivity
            : gamepadLookSensitivity * Time.deltaTime;

        cameraYaw += lookInput.x * sensitivity;
        cameraPitch -= lookInput.y * sensitivity;

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
            Vector3.up * cameraTargetHeight +
            cameraRotation * Vector3.right * cameraHorizontalOffset;

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

    private void HandleCursorLock()
    {
        if (cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            SetCursorLocked(false);
        }

        if (!cursorLocked &&
            ((cursorRelockClickAction != null &&
              cursorRelockClickAction.WasPressedThisFrame()) ||
             (cursorRelockSubmitAction != null &&
              cursorRelockSubmitAction.WasPressedThisFrame())))
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
