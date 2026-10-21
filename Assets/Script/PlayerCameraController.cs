using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using PlayerInputComponent = GardenKeeper.Inputs.PlayerInput;

[DefaultExecutionOrder(-100)]
public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform visualRoot;

    [Header("Camera")]
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraTargetHeight = 1.3f;
    [SerializeField] private float cameraHorizontalOffset;
    [Min(0f)]
    [SerializeField] private float followSmoothTime = 0.12f;
    [Min(0f)]
    [SerializeField] private float movementSwayAngle = 2.5f;
    [Min(0f)]
    [SerializeField] private float movementSwayFrequency = 1.8f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float gamepadLookSensitivity = 180f;
    [SerializeField] private float initialCameraPitch = 15f;
    [SerializeField] private float minCameraPitch = -15f;
    [SerializeField] private float maxCameraPitch = 65f;


    [SerializeField] private PauseMenuUI pauseMenuUI;

    private PlayerInputComponent playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction cancelAction;
    private InputAction cursorRelockClickAction;
    private InputAction cursorRelockSubmitAction;
    private float cameraYaw;
    private float cameraPitch;
    private float movementSwayPhase;
    private float movementSwayWeight;
    private float movementSwayWeightVelocity;
    private Vector3 cameraPositionVelocity;
    private bool cursorLocked;

    public float CameraYaw => cameraYaw;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInputComponent>();

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
        if (playerInput != null)
        {
            moveAction = playerInput.Move;
            lookAction = playerInput.Look;
            cancelAction = playerInput.Cancel;
            cursorRelockClickAction = playerInput.CursorRelockClick;
            cursorRelockSubmitAction = playerInput.CursorRelockSubmit;
        }

        SetCursorLocked(true);
    }

    private void Update()
    {
        HandleCursorLock();
        HandleCameraInput();
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
        UpdateBillboard();
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

        float movementSway = GetMovementSwayAngle();
        Quaternion cameraRotation =
            Quaternion.Euler(cameraPitch, cameraYaw, movementSway);

        Vector3 targetPosition =
            transform.position +
            Vector3.up * cameraTargetHeight +
            cameraRotation * Vector3.right * cameraHorizontalOffset;

        cameraTransform.rotation = cameraRotation;
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            cameraTransform.position,
            targetPosition -
            cameraRotation * Vector3.forward * cameraDistance,
            ref cameraPositionVelocity,
            followSmoothTime
        );

        cameraTransform.position = smoothedPosition;
    }

    private float GetMovementSwayAngle()
    {
        float movementAmount = moveAction != null
            ? Mathf.Clamp01(moveAction.ReadValue<Vector2>().magnitude)
            : 0f;

        movementSwayWeight = Mathf.SmoothDamp(
            movementSwayWeight,
            movementAmount,
            ref movementSwayWeightVelocity,
            0.15f
        );

        if (movementSwayWeight <= 0.001f || movementSwayAngle <= 0f)
        {
            return 0f;
        }

        movementSwayPhase +=
            Time.deltaTime * movementSwayFrequency * Mathf.PI * 2f;

        return
            Mathf.Sin(movementSwayPhase) *
            movementSwayAngle *
            movementSwayWeight;
    }

    private void UpdateBillboard()
    {
        if (visualRoot == null || cameraTransform == null)
        {
            return;
        }

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
        if (pauseMenuUI != null && pauseMenuUI.IsPaused)
        {
            if (cursorLocked)
                SetCursorLocked(false);

            return;
        }

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
