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

        // The visual UI is created lazily when this object first becomes
        // the player's valid interaction target.
        interactionPrompt = GetComponent<WorldInteractionPrompt>();
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
        if (!isTargeted)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetVisible(false);
            }

            return;
        }

        if (interactionPrompt == null)
        {
            interactionPrompt = GetComponent<WorldInteractionPrompt>();

            if (interactionPrompt == null)
            {
                interactionPrompt =
                    gameObject.AddComponent<WorldInteractionPrompt>();
            }
        }

        if (!interactionPromptInitialized)
        {
            interactionPrompt.Initialize(transform);
            interactionPromptInitialized = true;
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
