
using UnityEngine;

public class InfectableTree : InteractableObject
{
    public event System.Action<InfectableTree> BleedingStarted;

    protected override string InteractionPromptText =>
        isBleeding ? "СОБРАТЬ КРОВЬ" : "ИСПОЛЬЗОВАТЬ СВЕЧУ";

    protected override bool CanShowInteractionPrompt =>
        isBleeding ||
        (isCurrentlyInfected &&
         InteractionPromptInventory != null &&
         InteractionPromptInventory.CurrentItem == InventoryItemType.Candle);

    [Header("Visual Variants")]
    [SerializeField] private GameObject healthyVisual;
    [SerializeField] private GameObject infectedVisual;
    [SerializeField] private GameObject bleedingVisual;

    [Header("Tree State")]
    [SerializeField] private bool hasEverBeenInfected;
    [SerializeField] private bool isCurrentlyInfected;
    [SerializeField] private bool isBleeding;

    [Header("Bleeding Settings")]
    [Min(0.1f)]
    [SerializeField] private float bleedingCheckInterval = 10f;

    [Range(0f, 100f)]
    [SerializeField] private float bleedingChancePercent = 10f;

    [Min(0f)]
    [SerializeField] private float bleedingChanceIncreasePerFailedCheck = 20f;

    [Min(1)]
    [SerializeField] private int guaranteedBleedingAfterChecks = 5;

    private float bleedingTimeRemaining;
    private int infectionCycle;
    private int failedBleedingChecks;

    public bool HasEverBeenInfected => hasEverBeenInfected;
    public bool IsCurrentlyInfected => isCurrentlyInfected;
    public bool IsBleeding => isBleeding;

    public int InfectionCycle => infectionCycle;

    public float BleedingTimeRemaining =>
        Mathf.Max(0f, bleedingTimeRemaining);

    public bool IsBleedingTimerRunning =>
        hasEverBeenInfected &&
        !isCurrentlyInfected &&
        !isBleeding;

    protected override void Awake()
    {
        base.Awake();

        NormalizeState();

        // Initialize the countdown. If the tree cannot bleed yet,
        // the countdown remains paused at this value.
        bleedingTimeRemaining = Mathf.Max(
            0.1f,
            bleedingCheckInterval
        );

        RefreshVisuals();
    }

    private void Update()
    {
        CheckBleedingChance();
    }

    private void NormalizeState()
    {
        if (isCurrentlyInfected)
        {
            hasEverBeenInfected = true;
            isBleeding = false;
            infectionCycle = Mathf.Max(1, infectionCycle);
        }

        if (isBleeding)
        {
            hasEverBeenInfected = true;
        }
    }

    private void CheckBleedingChance()
    {
        // Pause the countdown whenever bleeding is not possible.
        // Keep the remaining time instead of resetting it.
        if (!IsBleedingTimerRunning)
        {
            return;
        }

        bleedingTimeRemaining -= Time.deltaTime;

        if (bleedingTimeRemaining > 0f)
        {
            return;
        }

        // A check has occurred. Start a fresh interval whether
        // the probability roll succeeds or fails.
        bleedingTimeRemaining = Mathf.Max(
            0.1f,
            bleedingCheckInterval
        );

        float chance = GetBleedingChanceForNextCheck();

        if (chance >= 100f || Random.Range(0f, 100f) < chance)
        {
            StartBleeding();
        }
        else
        {
            failedBleedingChecks = Mathf.Min(
                failedBleedingChecks + 1,
                Mathf.Max(0, guaranteedBleedingAfterChecks - 1)
            );
        }
    }

    private float GetBleedingChanceForNextCheck()
    {
        int checksUntilGuarantee = Mathf.Max(
            1,
            guaranteedBleedingAfterChecks
        );

        if (failedBleedingChecks >= checksUntilGuarantee - 1)
        {
            return 100f;
        }

        float chance = bleedingChancePercent +
                       failedBleedingChecks *
                       Mathf.Max(0f, bleedingChanceIncreasePerFailedCheck);

        return Mathf.Clamp(chance, 0f, 100f);
    }

    private void RefreshVisuals()
    {
        if (healthyVisual != null)
        {
            healthyVisual.SetActive(
                !isCurrentlyInfected && !isBleeding
            );
        }

        if (infectedVisual != null)
        {
            infectedVisual.SetActive(isCurrentlyInfected);
        }

        if (bleedingVisual != null)
        {
            bleedingVisual.SetActive(isBleeding);
        }

        RefreshInteractionPrompt();
    }

    public bool TryInfect()
    {
        if (isCurrentlyInfected)
        {
            return false;
        }

        hasEverBeenInfected = true;
        isCurrentlyInfected = true;
        isBleeding = false;

        infectionCycle++;

        // Preserve the bleeding countdown. Infection pauses it.
        RefreshVisuals();

        return true;
    }

    public bool Cure()
    {
        if (!isCurrentlyInfected)
        {
            return false;
        }

        isCurrentlyInfected = false;
        isBleeding = false;

        // The tree becomes healthy. Its countdown resumes
        // from the remaining time when Update runs again.
        RefreshVisuals();

        return true;
    }

    public bool StartBleeding()
    {
        if (!hasEverBeenInfected ||
            isCurrentlyInfected ||
            isBleeding)
        {
            return false;
        }

        isBleeding = true;
        failedBleedingChecks = 0;

        // The countdown pauses at its current value.
        RefreshVisuals();
        BleedingStarted?.Invoke(this);

        return true;
    }

    public bool StopBleeding()
    {
        if (!isBleeding)
        {
            return false;
        }

        isBleeding = false;

        // The paused countdown resumes when the tree is eligible.
        RefreshVisuals();

        return true;
    }

    public override bool CanInteract(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            return false;
        }

        bool canCollectBlood =
            isBleeding &&
            inventory.CurrentItem != InventoryItemType.Blood;

        bool canUseCandle =
            isCurrentlyInfected &&
            inventory.CurrentItem == InventoryItemType.Candle;

        return canCollectBlood || canUseCandle;
    }

    public override void Interact(PlayerInventory inventory)
    {
        if (!CanInteract(inventory))
        {
            return;
        }

        // Candle use is handled by PlayerInteraction. This method only
        // collects blood from a bleeding tree.
        if (!isBleeding)
        {
            return;
        }

        bool receivedBlood = false;

        if (inventory.CurrentItem == InventoryItemType.None)
        {
            receivedBlood = inventory.TryStoreItem(
                InventoryItemType.Blood
            );
        }
        else if (inventory.CurrentItem == InventoryItemType.Candle)
        {
            receivedBlood = inventory.ReplaceItem(
                InventoryItemType.Blood
            );
        }

        if (receivedBlood)
        {
            StopBleeding();
        }
    }
}
