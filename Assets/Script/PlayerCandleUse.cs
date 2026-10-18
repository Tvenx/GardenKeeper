
using UnityEngine;

public class PlayerCandleUse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;

    [Header("Candle Effect")]
    [Min(0.1f)]
    [SerializeField] private float cleanseRadius = 5f;

    [Min(0f)]
    [SerializeField] private float cleanseCenterHeight = 1f;

    [Header("Gizmos")]
    [SerializeField] private bool showRadiusGizmo = true;

    public float CleanseRadius => cleanseRadius;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }
    }

    public bool TryUseCandle()
    {
        if (inventory == null ||
            inventory.CurrentItem != InventoryItemType.Candle)
        {
            return false;
        }

        Vector3 center = GetCleanseCenter();
        float radiusSquared = cleanseRadius * cleanseRadius;

        InfectableTree[] trees =
            FindObjectsByType<InfectableTree>(
                FindObjectsSortMode.None
            );

        foreach (InfectableTree tree in trees)
        {
            if (tree == null || !tree.IsCurrentlyInfected)
            {
                continue;
            }

            Vector3 offset = tree.transform.position - center;

            if (offset.sqrMagnitude <= radiusSquared)
            {
                tree.Cure();
            }
        }

        // The candle is consumed even if there are no infected trees.
        inventory.ClearItem();

        return true;
    }

    private Vector3 GetCleanseCenter()
    {
        return transform.position +
               Vector3.up * cleanseCenterHeight;
    }

    private void OnDrawGizmos()
    {
        if (!showRadiusGizmo)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            GetCleanseCenter(),
            cleanseRadius
        );
    }
}
