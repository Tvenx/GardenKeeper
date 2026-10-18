
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InteractionInput
{
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    Digit0, Digit1, Digit2, Digit3, Digit4,
    Digit5, Digit6, Digit7, Digit8, Digit9,

    F1, F2, F3, F4, F5, F6,
    F7, F8, F9, F10, F11, F12,

    Space, Enter, Tab, Escape, Backspace,
    Delete, Insert, Home, End, PageUp, PageDown,

    LeftArrow, RightArrow, UpArrow, DownArrow,

    LeftShift, RightShift, LeftCtrl, RightCtrl,
    LeftAlt, RightAlt, LeftMeta, RightMeta,

    CapsLock, NumLock, PrintScreen, ScrollLock, Pause,
    ContextMenu,

    Backquote, Quote, Semicolon, Comma, Period, Slash,
    Backslash, LeftBracket, RightBracket, Minus, Equals,

    Numpad0, Numpad1, Numpad2, Numpad3, Numpad4,
    Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
    NumpadEnter, NumpadDivide, NumpadMultiply,
    NumpadPlus, NumpadMinus, NumpadPeriod, NumpadEquals,

    MouseLeft, MouseRight, MouseMiddle
}

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerCandleUse candleUse;
    [SerializeField] private Transform rayOrigin;

    [Header("Interaction")]
    [SerializeField]
    private InteractionInput interactionInput =
        InteractionInput.E;

    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 3f;

    [SerializeField] private LayerMask interactionLayers = ~0;

    private InteractableObject currentTarget;
    private Key selectedKeyboardKey = Key.E;



    private GameObject lookedAtObject;
    private InteractableObject lookedAtInteractable;

    public GameObject LookedAtObject => lookedAtObject;

    public InteractableObject LookedAtInteractable =>
        lookedAtInteractable;

    public InteractionInput SelectedInteractionInput =>
        interactionInput;

    public string InteractionButtonDisplayName
    {
        get
        {
            switch (interactionInput)
            {
                case InteractionInput.MouseLeft:
                    return "ЛКМ";

                case InteractionInput.MouseRight:
                    return "ПКМ";

                case InteractionInput.MouseMiddle:
                    return "СКМ";

                default:
                    return interactionInput.ToString();
            }
        }
    }



    private void Awake()
    {
        CacheSelectedKey();

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (candleUse == null)
        {
            candleUse = GetComponent<PlayerCandleUse>();
        }

        if (inventory == null)
        {
            Debug.LogError(
                "PlayerInteraction: PlayerInventory is missing.",
                this
            );
        }

        if (candleUse == null)
        {
            Debug.LogError(
                "PlayerInteraction: PlayerCandleUse is missing.",
                this
            );
        }

        if (rayOrigin == null)
        {
            Debug.LogWarning(
                "PlayerInteraction: Assign Ray Origin in Inspector.",
                this
            );
        }
    }

    private void OnValidate()
    {
        CacheSelectedKey();
    }

    private void CacheSelectedKey()
    {
        string inputName = interactionInput.ToString();

        if (inputName.StartsWith("Mouse", StringComparison.Ordinal))
        {
            return;
        }

        if (!Enum.TryParse<Key>(
            inputName,
            true,
            out selectedKeyboardKey))
        {
            selectedKeyboardKey = Key.E;
        }
    }

    private void Update()
    {
        if (inventory == null || rayOrigin == null)
        {
            lookedAtObject = null;
            lookedAtInteractable = null;

            SetCurrentTarget(null);
            return;
        }

        InteractableObject hitTarget = GetTargetUnderCrosshair();

        // The target can be detected even when interaction is disabled.
        // This is important when using a candle while looking at the statue.
        InteractableObject outlineTarget = null;

        if (hitTarget != null && hitTarget.CanInteract(inventory))
        {
            outlineTarget = hitTarget;
        }

        SetCurrentTarget(outlineTarget);

        if (InteractionPressedThisFrame())
        {
            HandleInteraction(hitTarget);

            // Refresh immediately after the inventory or target changes.
            InteractableObject newOutlineTarget = null;

            if (hitTarget != null && hitTarget.CanInteract(inventory))
            {
                newOutlineTarget = hitTarget;
            }

            SetCurrentTarget(newOutlineTarget);
        }
    }

    private InteractableObject GetTargetUnderCrosshair()
    {
        lookedAtObject = null;
        lookedAtInteractable = null;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
        {
            lookedAtObject = hit.collider.gameObject;

            lookedAtInteractable =
                hit.collider.GetComponentInParent<InteractableObject>();

            Debug.DrawRay(
                ray.origin,
                ray.direction * hit.distance,
                Color.green
            );

            return lookedAtInteractable;
        }

        Debug.DrawRay(
            ray.origin,
            ray.direction * interactionDistance,
            Color.red
        );

        return null;
    }

    private void HandleInteraction(InteractableObject hitTarget)
    {
        // Priority 1:
        // A bleeding tree takes priority over candle usage.
        // The candle is replaced by blood.
        if (inventory.CurrentItem == InventoryItemType.Candle)
        {
            if (hitTarget is InfectableTree tree &&
                tree.IsBleeding &&
                tree.CanInteract(inventory))
            {
                tree.Interact(inventory);
                return;
            }

            // Priority 2:
            // Otherwise, pressing the interaction button uses the candle.
            // This also works while aiming at the statue.
            if (candleUse != null)
            {
                candleUse.TryUseCandle();
            }

            return;
        }

        // Priority 3:
        // With empty hands or blood, interact with an available target.
        if (hitTarget != null && hitTarget.CanInteract(inventory))
        {
            hitTarget.Interact(inventory);
        }
    }

    private bool InteractionPressedThisFrame()
    {
        switch (interactionInput)
        {
            case InteractionInput.MouseLeft:
                return Mouse.current != null &&
                       Mouse.current.leftButton.wasPressedThisFrame;

            case InteractionInput.MouseRight:
                return Mouse.current != null &&
                       Mouse.current.rightButton.wasPressedThisFrame;

            case InteractionInput.MouseMiddle:
                return Mouse.current != null &&
                       Mouse.current.middleButton.wasPressedThisFrame;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard[selectedKeyboardKey].wasPressedThisFrame;
    }



    public string GetCurrentActionDescription()
    {
        if (inventory == null)
        {
            return "Инвентарь недоступен";
        }

        InventoryItemType item = inventory.CurrentItem;
        InteractableObject target = lookedAtInteractable;

        // A held candle is used unless a bleeding tree
        // has priority and can provide blood.
        if (item == InventoryItemType.Candle)
        {
            if (target is InfectableTree bleedingTree &&
                bleedingTree.IsBleeding &&
                bleedingTree.CanInteract(inventory))
            {
                return "Свеча исчезнет, дерево перестанет кровоточить, " +
                       "в руки попадёт кровь";
            }

            float radius = candleUse != null
                ? candleUse.CleanseRadius
                : 0f;

            return $"Использовать свечу: очистить заражённые деревья " +
                   $"в радиусе {radius:F1}; кровоточащие не изменятся";
        }

        if (item == InventoryItemType.Blood)
        {
            if (target is StatueCandleStation bloodStatue)
            {
                if (bloodStatue.HasWon)
                {
                    return "Статуя уже заполнена, победа достигнута";
                }

                if (bloodStatue.CanInteract(inventory))
                {
                    return $"Передать кровь статуе: " +
                           $"{bloodStatue.CurrentBloodDeposits + 1}/" +
                           $"{bloodStatue.RequiredBloodDeposits}";
                }

                return "Статуя сейчас недоступна";
            }

            if (target is InfectableTree)
            {
                return "Руки заняты кровью; собрать ещё кровь нельзя";
            }

            return "Чтобы сдать кровь, нужно смотреть на статую";
        }

        if (target is StatueCandleStation statue)
        {
            if (statue.HasWon)
            {
                return "Победа уже достигнута";
            }

            if (statue.CanInteract(inventory))
            {
                return "Подобрать случайную свечу у статуи";
            }

            return "У статуи закончились свечи";
        }

        if (target is InfectableTree tree)
        {
            if (tree.IsBleeding && tree.CanInteract(inventory))
            {
                return "Собрать кровь: дерево станет здоровым";
            }

            if (tree.IsCurrentlyInfected)
            {
                return "Дерево заражено; нужна очищающая свеча";
            }

            return "Дерево не кровоточит; собирать нечего";
        }

        if (lookedAtObject != null)
        {
            return "Этот объект не поддерживает взаимодействие";
        }

        return "Нет цели для взаимодействия";
    }


    private void SetCurrentTarget(InteractableObject newTarget)
    {
        if (currentTarget == newTarget)
        {
            return;
        }

        if (currentTarget != null)
        {
            currentTarget.SetOutline(false);
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTarget.SetOutline(true);
        }
    }

    private void OnDrawGizmos()
    {
        if (rayOrigin == null)
        {
            return;
        }

        Vector3 start = rayOrigin.position;
        Vector3 end =
            start + rayOrigin.forward * interactionDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.15f);
    }

    private void OnDisable()
    {
        SetCurrentTarget(null);
    }
}
