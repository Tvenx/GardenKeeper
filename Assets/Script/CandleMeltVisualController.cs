using UnityEngine;

[DisallowMultipleComponent]
public sealed class CandleMeltVisualController : MonoBehaviour
{
    private const int RequiredStageCount = 7;

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject[] meltStages =
        new GameObject[RequiredStageCount];

    [Header("Flame Light")]
    [SerializeField] private Light flameLight;

    [Min(0f)]
    [SerializeField] private float fullCandleIntensity = 1.5f;

    [Min(0f)]
    [SerializeField] private float nearlyBurnedOutIntensity = 0.2f;

    private int activeStageIndex = int.MinValue;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }

        if (inventory == null)
        {
            Debug.LogError(
                "CandleMeltVisualController: PlayerInventory is missing.",
                this
            );
            enabled = false;
            return;
        }

        if (meltStages == null || meltStages.Length != RequiredStageCount)
        {
            Debug.LogError(
                "CandleMeltVisualController: Assign exactly 7 melt stages.",
                this
            );
            enabled = false;
            return;
        }

        if (flameLight == null)
        {
            flameLight = GetComponentInChildren<Light>(true);
        }

        if (flameLight == null)
        {
            Debug.LogWarning(
                "CandleMeltVisualController: Assign the candle flame Light.",
                this
            );
        }

        SetActiveStage(-1);
    }

    private void LateUpdate()
    {
        if (inventory.CurrentItem != InventoryItemType.Candle)
        {
            SetActiveStage(-1);
            return;
        }

        float progress = Mathf.Clamp01(
            inventory.CandleTimeRemaining / inventory.CandleBurnDuration
        );

        int stageIndex = Mathf.Clamp(
            Mathf.CeilToInt(progress * RequiredStageCount) - 1,
            0,
            RequiredStageCount - 1
        );

        SetActiveStage(stageIndex);
        UpdateLightIntensity(progress);
    }

    private void UpdateLightIntensity(float progress)
    {
        if (flameLight == null)
        {
            return;
        }

        flameLight.intensity = Mathf.Lerp(
            nearlyBurnedOutIntensity,
            fullCandleIntensity,
            progress
        );
    }

    private void SetActiveStage(int stageIndex)
    {
        if (activeStageIndex == stageIndex)
        {
            return;
        }

        for (int i = 0; i < meltStages.Length; i++)
        {
            if (meltStages[i] != null)
            {
                meltStages[i].SetActive(i == stageIndex);
            }
        }

        activeStageIndex = stageIndex;
    }
}
