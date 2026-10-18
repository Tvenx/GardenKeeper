using UnityEngine;

public abstract class InteractableObject : MonoBehaviour
{
    [Header("Interaction Outline")]
    [SerializeField] private Behaviour quickOutline;

    private WorldInteractionPrompt interactionPrompt;
    private bool interactionPromptInitialized;
    private bool isTargeted;
    private PlayerInventory currentInventory;
    private bool hasInventoryContext;
    private string interactionPromptKeyOverride;

    protected PlayerInventory InteractionPromptInventory =>
        hasInventoryContext ? currentInventory : null;

    protected virtual string InteractionPromptText =>
        "ВЗАИМОДЕЙСТВОВАТЬ";

    protected virtual string InteractionPromptKey => "E";

    protected virtual bool CanShowInteractionPrompt => true;

    protected virtual void Awake()
    {
        if (quickOutline != null)
        {
            quickOutline.enabled = false;
        }

        interactionPrompt = GetComponent<WorldInteractionPrompt>();
        if (interactionPrompt == null)
        {
            Debug.LogError(
                $"{GetType().Name} on {name} needs a " +
                $"{nameof(WorldInteractionPrompt)} component with its UI " +
                "assigned on the prefab.",
                this
            );
        }
    }

    public void SetOutline(bool visible)
    {
        UpdatePromptTarget(visible, null, false, null);
    }

    public void SetOutline(bool visible, PlayerInventory inventory)
    {
        UpdatePromptTarget(visible, inventory, true, null);
    }

    public void SetOutline(
        bool visible,
        PlayerInventory inventory,
        string keyDisplayName)
    {
        UpdatePromptTarget(visible, inventory, true, keyDisplayName);
    }

    private void UpdatePromptTarget(
        bool visible,
        PlayerInventory inventory,
        bool includeInventory,
        string keyDisplayName)
    {
        isTargeted = visible;
        hasInventoryContext = includeInventory;
        currentInventory = inventory;
        interactionPromptKeyOverride = keyDisplayName;

        if (quickOutline != null)
        {
            quickOutline.enabled = visible;
        }

        RefreshInteractionPrompt();
    }

    protected void RefreshInteractionPrompt()
    {
        if (interactionPrompt == null)
        {
            return;
        }

        if (!isTargeted)
        {
            interactionPrompt.SetVisible(false);
            return;
        }

        if (!interactionPromptInitialized)
        {
            interactionPromptInitialized = interactionPrompt.Initialize(transform);
            if (!interactionPromptInitialized)
            {
                return;
            }
        }

        interactionPrompt.SetPrompt(
            InteractionPromptText,
            string.IsNullOrWhiteSpace(interactionPromptKeyOverride)
                ? InteractionPromptKey
                : interactionPromptKeyOverride
        );

        bool canInteract = !hasInventoryContext ||
                           CanInteract(currentInventory);

        interactionPrompt.SetVisible(
            CanShowInteractionPrompt && canInteract
        );
    }

    public abstract bool CanInteract(PlayerInventory inventory);

    public abstract void Interact(PlayerInventory inventory);
}
