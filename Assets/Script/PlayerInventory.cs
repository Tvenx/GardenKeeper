using UnityEngine;

public enum InventoryItemType
{
    None,
    Candle,
    Blood
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Placeholder")]
    [SerializeField] private GameObject placeholder;
    [SerializeField] private GameObject candleVisual;
    [SerializeField] private GameObject bloodVisual;

    [Header("Candle Lifetime")]
    [Min(0.1f)]
    [SerializeField] private float candleBurnDuration = 5f;

    private float candleTimeRemaining;
    private CandleFlameController candleFlame;
    [SerializeField] private Sprite[] candleFlameFrames = new Sprite[0];

    public InventoryItemType CurrentItem { get; private set; }
        = InventoryItemType.None;

    public float CandleTimeRemaining =>
    CurrentItem == InventoryItemType.Candle
        ? Mathf.Max(0f, candleTimeRemaining)
        : 0f;

    public float CandleBurnDuration =>
        Mathf.Max(0.1f, candleBurnDuration);

    private void Awake()
    {
        if (candleVisual != null)
        {
            candleFlame = candleVisual.GetComponent<CandleFlameController>();

            if (candleFlame == null)
            {
                candleFlame =
                    candleVisual.AddComponent<CandleFlameController>();
            }

            candleFlame.SetFlameFrames(candleFlameFrames);
        }

        RefreshHeldVisual();
    }

    private void Update()
    {
        if (CurrentItem != InventoryItemType.Candle)
        {
            return;
        }

        candleTimeRemaining -= Time.deltaTime;

        if (candleFlame != null)
        {
            candleFlame.SetBurnProgress(
                CandleTimeRemaining / CandleBurnDuration
            );
        }

        if (candleTimeRemaining <= 0f)
        {
            ClearItem();
        }
    }

    public bool TryStoreItem(InventoryItemType item)
    {
        if (item == InventoryItemType.None ||
            CurrentItem != InventoryItemType.None)
        {
            return false;
        }

        CurrentItem = item;

        if (item == InventoryItemType.Candle)
        {
            StartCandleTimer();
        }

        RefreshHeldVisual();

        return true;
    }

    public bool ReplaceItem(InventoryItemType item)
    {
        if (item == InventoryItemType.None ||
            CurrentItem == InventoryItemType.None ||
            CurrentItem == item)
        {
            return false;
        }

        CurrentItem = item;

        if (item == InventoryItemType.Candle)
        {
            StartCandleTimer();
        }
        else
        {
            candleTimeRemaining = 0f;
        }

        RefreshHeldVisual();

        return true;
    }

    public void ClearItem()
    {
        CurrentItem = InventoryItemType.None;
        candleTimeRemaining = 0f;

        RefreshHeldVisual();
    }

    private void StartCandleTimer()
    {
        candleTimeRemaining = Mathf.Max(
            0.1f,
            candleBurnDuration
        );
    }

    private void RefreshHeldVisual()
    {
        if (placeholder != null && !placeholder.activeSelf)
        {
            placeholder.SetActive(true);
        }

        if (candleVisual != null)
        {
            bool candleIsActive =
                CurrentItem == InventoryItemType.Candle;

            candleVisual.SetActive(candleIsActive);

            if (candleIsActive && candleFlame != null)
            {
                candleFlame.SetBurnProgress(
                    CandleTimeRemaining / CandleBurnDuration
                );
            }
        }

        if (bloodVisual != null)
        {
            bloodVisual.SetActive(
                CurrentItem == InventoryItemType.Blood
            );
        }
    }
}