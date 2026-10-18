
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

    public InventoryItemType CurrentItem { get; private set; }
        = InventoryItemType.None;

    public float CandleTimeRemaining =>
    CurrentItem == InventoryItemType.Candle
        ? Mathf.Max(0f, candleTimeRemaining)
        : 0f;

    private void Awake()
    {
        RefreshHeldVisual();
    }

    private void Update()
    {
        if (CurrentItem != InventoryItemType.Candle)
        {
            return;
        }

        candleTimeRemaining -= Time.deltaTime;

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
            candleVisual.SetActive(
                CurrentItem == InventoryItemType.Candle
            );
        }

        if (bloodVisual != null)
        {
            bloodVisual.SetActive(
                CurrentItem == InventoryItemType.Blood
            );
        }
    }
}
