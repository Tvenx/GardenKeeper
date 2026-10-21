
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInputComponent = GardenKeeper.Inputs.PlayerInput;

[RequireComponent(typeof(PlayerInputComponent))]
public class PlayerInteraction : MonoBehaviour
{
    private static readonly int UseCandleStateHash =
        Animator.StringToHash("UseCandle");

    public static bool IsCandleAnimationPauseActive { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerCandleUse candleUse;
    [SerializeField] private Transform rayOrigin;

    [Header("Interaction")]
    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 3f;

    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private InteractableObject currentTarget;
    private PlayerInputComponent playerInput;
    private InputAction interactAction;
    private bool candleAnimationHasStarted;
    private float previousTimeScale;
    private AnimatorUpdateMode previousAnimatorUpdateMode;

    private GameObject lookedAtObject;
    private InteractableObject lookedAtInteractable;

    public GameObject LookedAtObject => lookedAtObject;

    public InteractableObject LookedAtInteractable =>
        lookedAtInteractable;

    public string InteractionButtonDisplayName => "LMB";

    private void Awake()
    {
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

        playerInput = GetComponent<PlayerInputComponent>();

        if (rayOrigin == null)
        {
            Debug.LogWarning(
                "PlayerInteraction: Assign Ray Origin in Inspector.",
                this
            );
        }
    }

    private void Start()
    {
        if (playerInput != null)
        {
            interactAction = playerInput.Interact;
        }
    }

    private void OnDisable()
    {
        EndCandleAnimationPause();
        SetCurrentTarget(null);
    }

    private void Update()
    {
        UpdateCandleAnimationPause();

        if (IsCandleAnimationPauseActive || Time.timeScale == 0f)
        {
            return;
        }

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

        if (hitTarget != null && hitTarget.CanShowOutline(inventory))
        {
            outlineTarget = hitTarget;
        }

        SetCurrentTarget(outlineTarget);

        if (interactAction != null &&
            interactAction.WasPressedThisFrame())
        {
            HandleInteraction(hitTarget);

            // Refresh immediately after the inventory or target changes.
            InteractableObject newOutlineTarget = null;

            if (hitTarget != null && hitTarget.CanShowOutline(inventory))
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
                if (candleUse.TryUseCandle())
                {
                    if (animator == null)
                    {
                        Debug.LogError(
                            "PlayerInteraction: Animator is missing.",
                            this
                        );
                        return;
                    }

                    BeginCandleAnimationPause();
                    animator.SetTrigger(UseCandleStateHash);
                }
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

    private void BeginCandleAnimationPause()
    {
        previousTimeScale = Time.timeScale;
        previousAnimatorUpdateMode = animator.updateMode;
        candleAnimationHasStarted = false;
        IsCandleAnimationPauseActive = true;

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 0f;
    }

    private void UpdateCandleAnimationPause()
    {
        if (!IsCandleAnimationPauseActive)
        {
            return;
        }

        if (animator == null)
        {
            EndCandleAnimationPause();
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.shortNameHash == UseCandleStateHash)
        {
            candleAnimationHasStarted = true;

            if (state.normalizedTime >= 1f &&
                !animator.IsInTransition(0))
            {
                EndCandleAnimationPause();
            }
        }
        else if (candleAnimationHasStarted)
        {
            EndCandleAnimationPause();
        }
    }

    private void EndCandleAnimationPause()
    {
        if (!IsCandleAnimationPauseActive)
        {
            return;
        }

        if (animator != null)
        {
            animator.updateMode = previousAnimatorUpdateMode;
        }

        Time.timeScale = previousTimeScale;
        IsCandleAnimationPauseActive = false;
        candleAnimationHasStarted = false;
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
            if (currentTarget != null)
            {
                currentTarget.SetOutline(
                    true,
                    inventory,
                    InteractionButtonDisplayName
                );
            }

            return;
        }

        if (currentTarget != null)
        {
            currentTarget.SetOutline(false);
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTarget.SetOutline(
                true,
                inventory,
                InteractionButtonDisplayName
            );
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

}
