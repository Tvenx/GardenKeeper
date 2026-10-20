
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

        bool curedAnyTree = false;

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

            if (offset.sqrMagnitude <= radiusSquared &&
                tree.Cure())
            {
                curedAnyTree = true;
            }
        }

        MonsterController[] monsters =
            FindObjectsByType<MonsterController>(
                FindObjectsSortMode.None
            );

        foreach (MonsterController monster in monsters)
        {
            if (monster == null)
            {
                continue;
            }

            // Reset temporary speed if the candle actually cured
            // an infected tree and the monster's checkbox is enabled.
            if (curedAnyTree)
            {
                monster.NotifyTreeCuredByCandle();
            }

            // If the monster is close enough, the candle also
            // teleports it and resets temporary speed regardless
            // of the tree-cure checkbox.
            monster.TryTeleportFromCandle(transform.position);
        }

        // The candle is consumed even when no trees were cured
        // and even when no monster was close enough to teleport.
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
