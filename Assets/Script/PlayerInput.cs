using UnityEngine;
using UnityEngine.InputSystem;

namespace GardenKeeper.Inputs
{
    public sealed class PlayerInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionAsset runtimeInputActions;

        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Cancel { get; private set; }
        public InputAction CursorRelockClick { get; private set; }
        public InputAction CursorRelockSubmit { get; private set; }

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "PlayerInput: Input Actions asset is missing.",
                    this
                );
                return;
            }

            runtimeInputActions = Instantiate(inputActions);

            Move = FindAction("Player/Move");
            Look = FindAction("Player/Look");
            Sprint = FindAction("Player/Sprint");
            Interact = FindAction("Player/Interact");
            Cancel = FindAction("UI/Cancel");
            CursorRelockClick = FindAction("UI/Click");
            CursorRelockSubmit = FindAction("UI/Submit");
        }

        private void OnEnable()
        {
            runtimeInputActions?.Enable();
        }

        private void OnDisable()
        {
            runtimeInputActions?.Disable();
        }

        private void OnDestroy()
        {
            if (runtimeInputActions != null)
            {
                Destroy(runtimeInputActions);
            }
        }

        private InputAction FindAction(string actionName)
        {
            InputAction action = runtimeInputActions.FindAction(
                actionName,
                throwIfNotFound: false
            );

            if (action == null)
            {
                Debug.LogError(
                    $"PlayerInput: Action '{actionName}' is missing.",
                    this
                );
            }

            return action;
        }
    }
}
