using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CandleTimerHUD : MonoBehaviour
{
    private const float BarWidth = 300f;
    private const float BarHeight = 12f;

    private static readonly Color BarColor =
        new Color(1f, 0.68f, 0.24f, 1f);

    private static readonly Color BarWarningColor =
        new Color(0.95f, 0.23f, 0.16f, 1f);

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform leftFill;
    [SerializeField] private RectTransform rightFill;

    private Image leftFillImage;
    private Image rightFillImage;
    private int lastDisplayedSecond = int.MinValue;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (inventory == null ||
            panelRoot == null ||
            timerText == null ||
            leftFill == null ||
            rightFill == null)
        {
            Debug.LogError(
                "CandleTimerHUD: Assign the inventory and timer UI references.",
                this
            );
            enabled = false;
            return;
        }

        leftFillImage = leftFill.GetComponent<Image>();
        rightFillImage = rightFill.GetComponent<Image>();

        if (leftFillImage == null || rightFillImage == null)
        {
            Debug.LogError(
                "CandleTimerHUD: Both timer fills need an Image component.",
                this
            );
            enabled = false;
        }
    }

    private void Update()
    {
        bool candleInHand =
            inventory.CurrentItem == InventoryItemType.Candle;

        if (panelRoot.activeSelf != candleInHand)
        {
            panelRoot.SetActive(candleInHand);
        }

        if (!candleInHand)
        {
            lastDisplayedSecond = int.MinValue;
            return;
        }

        float remaining = inventory.CandleTimeRemaining;
        float progress = Mathf.Clamp01(
            remaining / inventory.CandleBurnDuration
        );

        UpdateTimerText(remaining);
        UpdateBar(progress);
    }

    private void UpdateTimerText(float remaining)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));

        if (totalSeconds == lastDisplayedSecond)
        {
            return;
        }

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
        lastDisplayedSecond = totalSeconds;
    }

    private void UpdateBar(float progress)
    {
        float halfWidth = BarWidth * 0.5f * progress;
        leftFill.sizeDelta = new Vector2(halfWidth, BarHeight);
        rightFill.sizeDelta = new Vector2(halfWidth, BarHeight);

        float warningAmount = Mathf.InverseLerp(0.3f, 0f, progress);
        Color fillColor = Color.Lerp(
            BarColor,
            BarWarningColor,
            warningAmount
        );

        leftFillImage.color = fillColor;
        rightFillImage.color = fillColor;
    }

    private void OnDisable()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}
