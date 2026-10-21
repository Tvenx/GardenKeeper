
using System.Collections.Generic;
using UnityEngine;

public class StatueCandleStation : InteractableObject
{
    [Header("Candle Spawn")]
    [SerializeField]
    private List<Transform> candleSpawnPoints =
        new List<Transform>();

    [SerializeField] private GameObject candlePrefab;

    [Header("Blood Goal")]
    [Min(1)]
    [SerializeField] private int requiredBloodDeposits = 3;

    [SerializeField] private int currentBloodDeposits;

    [Header("Blood Level")]
    [SerializeField] private Transform bloodCylinder;

    [Min(0f)]
    [SerializeField] private float minimumBloodHeight = 0.05f;

    [Min(0f)]
    [SerializeField] private float maximumBloodHeight = 0.8f;

    private readonly List<GameObject> spawnedCandles =
        new List<GameObject>();

    private bool hasWon;
    private Vector3 bloodCylinderBaseLocalPosition;

    public int CurrentBloodDeposits => currentBloodDeposits;

    public int RequiredBloodDeposits =>
        Mathf.Max(1, requiredBloodDeposits);

    public bool HasWon => hasWon;

    public int AvailableCandles => GetAvailableCandleCount();

    protected override string InteractionPromptText =>
        InteractionPromptInventory != null &&
        InteractionPromptInventory.CurrentItem == InventoryItemType.Blood
            ? "ПЕРЕДАТЬ КРОВЬ"
            : "ПОДОБРАТЬ СВЕЧУ";

    protected override void Awake()
    {
        base.Awake();
        InitializeBloodCylinder();
        SpawnCandles();
    }

    private void InitializeBloodCylinder()
    {
        if (bloodCylinder == null)
        {
            Debug.LogError(
                "StatueCandleStation: Blood Cylinder is missing.",
                this
            );

            return;
        }

        bloodCylinderBaseLocalPosition =
            bloodCylinder.localPosition -
            Vector3.up * (bloodCylinder.localScale.y * 0.5f);

        UpdateBloodCylinderHeight();
    }

    private void UpdateBloodCylinderHeight()
    {
        if (bloodCylinder == null)
        {
            return;
        }

        int required = RequiredBloodDeposits;
        float progress = Mathf.Clamp01(
            (float)currentBloodDeposits / required
        );
        float minimumHeight = Mathf.Max(0f, minimumBloodHeight);
        float maximumHeight = Mathf.Max(
            minimumHeight,
            maximumBloodHeight
        );
        float height = Mathf.Lerp(
            minimumHeight,
            maximumHeight,
            progress
        );

        Vector3 scale = bloodCylinder.localScale;
        scale.y = height;
        bloodCylinder.localScale = scale;

        Vector3 position = bloodCylinderBaseLocalPosition;
        position.y += height * 0.5f;
        bloodCylinder.localPosition = position;
    }

    private void SpawnCandles()
    {
        spawnedCandles.Clear();

        if (candlePrefab == null)
        {
            Debug.LogError(
                "StatueCandleStation: Candle Prefab is missing.",
                this
            );

            return;
        }

        foreach (Transform spawnPoint in candleSpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            GameObject candle = Instantiate(
                candlePrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint
            );

            candle.transform.localPosition = Vector3.zero;
            candle.transform.localRotation = Quaternion.identity;

            spawnedCandles.Add(candle);
        }
    }

    private int GetAvailableCandleCount()
    {
        int count = 0;

        foreach (GameObject candle in spawnedCandles)
        {
            if (candle != null && candle.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private int GetRandomAvailableCandleIndex()
    {
        List<int> availableIndices = new List<int>();

        for (int i = 0; i < spawnedCandles.Count; i++)
        {
            GameObject candle = spawnedCandles[i];

            if (candle != null && candle.activeInHierarchy)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return -1;
        }

        return availableIndices[
            Random.Range(0, availableIndices.Count)
        ];
    }

    public override bool CanInteract(PlayerInventory inventory)
    {
        if (inventory == null || hasWon)
        {
            return false;
        }

        // Blood is always deposited before a candle can be picked up.
        if (inventory.CurrentItem == InventoryItemType.Blood)
        {
            return true;
        }

        // A candle in hand disables interaction with the statue.
        if (inventory.CurrentItem != InventoryItemType.None)
        {
            return false;
        }

        return GetAvailableCandleCount() > 0;
    }

    public override void Interact(PlayerInventory inventory)
    {
        if (!CanInteract(inventory))
        {
            return;
        }

        // Blood donation has priority over candle pickup.
        if (inventory.CurrentItem == InventoryItemType.Blood)
        {
            DepositBlood(inventory);
            return;
        }

        // The player can pick up a candle only with empty hands.
        int index = GetRandomAvailableCandleIndex();

        if (index < 0)
        {
            return;
        }

        if (inventory.TryStoreItem(InventoryItemType.Candle))
        {
            spawnedCandles[index].SetActive(false);
        }
    }

    private void DepositBlood(PlayerInventory inventory)
    {
        if (hasWon ||
            inventory.CurrentItem != InventoryItemType.Blood)
        {
            return;
        }

        inventory.ClearItem();
        currentBloodDeposits++;
        UpdateBloodCylinderHeight();

        MonsterController[] monsters =
    FindObjectsByType<MonsterController>(
        FindObjectsSortMode.None
    );

        foreach (MonsterController monster in monsters)
        {
            if (monster != null)
            {
                monster.NotifyBloodOfferingMade(this);
            }
        }


        int required = Mathf.Max(1, requiredBloodDeposits);

        Debug.Log(
            $"Blood deposited: {currentBloodDeposits}/{required}",
            this
        );

        if (currentBloodDeposits >= required)
        {
            hasWon = true;

            Debug.Log(
                "YOU WIN! The statue has been filled with blood.",
                this
            );
        }
    }
}
    