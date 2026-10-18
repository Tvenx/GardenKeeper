
using System.Collections.Generic;
using UnityEngine;

public enum InfectionDefeatMode
{
    Percentage,
    TreeCount
}

public class InfectionManager : MonoBehaviour
{
    [Header("Trees")]
    [SerializeField]
    private List<InfectableTree> trees =
        new List<InfectableTree>();

    [Header("Starting Infection")]
    [Min(0)]
    [SerializeField] private int initialInfectedTreeCount = 1;

    [Header("Random Infection")]
    [SerializeField] private bool enableRandomInfection = true;

    [Min(0.1f)]
    [SerializeField] private float randomInfectionIntervalMin = 60f;

    [Min(0.1f)]
    [SerializeField] private float randomInfectionIntervalMax = 90f;

    [Min(0f)]
    [SerializeField] private float randomAccelerationEvery = 120f;

    [Min(0f)]
    [SerializeField] private float randomIntervalDecreasePerStep = 1f;

    [Min(0.1f)]
    [SerializeField] private float minimumRandomInfectionInterval = 30f;

    [Header("Infection Spread")]
    [SerializeField] private bool enableSpread = true;

    [Min(0f)]
    [SerializeField] private float spreadRadius = 10f;

    [Min(0.1f)]
    [SerializeField] private float spreadIntervalMin = 10f;

    [Min(0.1f)]
    [SerializeField] private float spreadIntervalMax = 15f;

    [Min(0f)]
    [SerializeField] private float spreadAccelerationEvery = 180f;

    [Min(0f)]
    [SerializeField] private float spreadIntervalDecreasePerStep = 1f;

    [Min(0.1f)]
    [SerializeField] private float minimumSpreadInterval = 5f;

    [Header("Defeat Condition")]
    [SerializeField] private bool canLose = true;

    [SerializeField]
    private InfectionDefeatMode defeatMode =
        InfectionDefeatMode.Percentage;

    [Range(0.1f, 100f)]
    [SerializeField] private float loseAtInfectedPercentage = 51f;

    [Min(1)]
    [SerializeField] private int loseAtInfectedTreeCount = 14;

    [Header("Gizmos")]
    [SerializeField] private bool showSpreadRadiusGizmos = true;

    private float elapsedGameTime;
    private float randomInfectionTimer;

    private bool hasLost;

    private readonly Dictionary<InfectableTree, SpreadTimerState>
        spreadTimers = new Dictionary<InfectableTree, SpreadTimerState>();



    // Read-only debug information.

    public bool RandomInfectionEnabled => enableRandomInfection;
    public bool SpreadEnabled => enableSpread;
    public bool CanLose => canLose;
    public bool HasLost => hasLost;

    public float ElapsedGameTime => elapsedGameTime;

    public float TimeUntilNextRandomInfection =>
        Mathf.Max(0f, randomInfectionTimer);

    public InfectionDefeatMode DefeatMode => defeatMode;

    public float LoseAtInfectedPercentage =>
        loseAtInfectedPercentage;

    public int LoseAtInfectedTreeCount =>
        loseAtInfectedTreeCount;

    public void GetTreeStateCounts(
        out int total,
        out int infected,
        out int healthy,
        out int bleeding)
    {
        List<InfectableTree> allTrees = GetValidTrees();

        total = allTrees.Count;
        infected = 0;
        healthy = 0;
        bleeding = 0;

        foreach (InfectableTree tree in allTrees)
        {
            if (tree.IsCurrentlyInfected)
            {
                infected++;
            }
            else if (tree.IsBleeding)
            {
                bleeding++;
            }
            else
            {
                healthy++;
            }
        }
    }

    public void GetCurrentRandomInfectionIntervalRange(
        out float min,
        out float max)
    {
        CalculateCurrentIntervalRange(
            randomInfectionIntervalMin,
            randomInfectionIntervalMax,
            randomAccelerationEvery,
            randomIntervalDecreasePerStep,
            minimumRandomInfectionInterval,
            out min,
            out max
        );
    }

    public void GetCurrentSpreadIntervalRange(
        out float min,
        out float max)
    {
        CalculateCurrentIntervalRange(
            spreadIntervalMin,
            spreadIntervalMax,
            spreadAccelerationEvery,
            spreadIntervalDecreasePerStep,
            minimumSpreadInterval,
            out min,
            out max
        );
    }

    private void CalculateCurrentIntervalRange(
        float intervalMin,
        float intervalMax,
        float accelerationEvery,
        float decreasePerStep,
        float minimumInterval,
        out float currentMin,
        out float currentMax)
    {
        float startMin = Mathf.Max(0.1f, intervalMin);
        float startMax = Mathf.Max(0.1f, intervalMax);

        if (startMin > startMax)
        {
            float temp = startMin;
            startMin = startMax;
            startMax = temp;
        }

        float floor = Mathf.Clamp(
            minimumInterval,
            0.1f,
            startMin
        );

        int steps = 0;

        if (accelerationEvery > 0f && decreasePerStep > 0f)
        {
            steps = Mathf.FloorToInt(
                elapsedGameTime / accelerationEvery
            );
        }

        float decrease = steps * decreasePerStep;

        currentMin = Mathf.Max(floor, startMin - decrease);
        currentMax = Mathf.Max(floor, startMax - decrease);
    }

    public bool TryGetSpreadTimeRemaining(
        InfectableTree tree,
        out float secondsRemaining)
    {
        secondsRemaining = 0f;

        if (!enableSpread ||
            tree == null ||
            !tree.IsCurrentlyInfected)
        {
            return false;
        }

        if (!spreadTimers.TryGetValue(
            tree,
            out SpreadTimerState state))
        {
            return false;
        }

        if (state.infectionCycle != tree.InfectionCycle)
        {
            return false;
        }

        secondsRemaining = Mathf.Max(0f, state.timeRemaining);
        return true;
    }

    /// <summary>
    /// 
    /// </summary>

    private class SpreadTimerState
    {
        public int infectionCycle;
        public float timeRemaining;

        public SpreadTimerState(int cycle, float time)
        {
            infectionCycle = cycle;
            timeRemaining = time;
        }
    }

    private void Start()
    {
        InitializeStartingInfections();

        randomInfectionTimer = GetRandomInfectionInterval();

        CheckDefeatCondition();
    }

    private void Update()
    {
        if (hasLost)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        elapsedGameTime += deltaTime;

        List<InfectableTree> allTrees = GetValidTrees();

        if (enableSpread)
        {
            UpdateSpreadTimers(allTrees, deltaTime);
        }

        // Check immediately after spread, before random infection.
        if (CheckDefeatCondition())
        {
            return;
        }

        if (enableRandomInfection)
        {
            randomInfectionTimer -= deltaTime;

            if (randomInfectionTimer <= 0f)
            {
                TryRandomInfection();
                randomInfectionTimer = GetRandomInfectionInterval();
            }
        }

        CheckDefeatCondition();
    }

    private void InitializeStartingInfections()
    {
        List<InfectableTree> allTrees = GetValidTrees();
        List<InfectableTree> candidates =
            new List<InfectableTree>();

        int currentlyInfected = 0;

        foreach (InfectableTree tree in allTrees)
        {
            if (tree.IsCurrentlyInfected)
            {
                currentlyInfected++;
            }
            else
            {
                candidates.Add(tree);
            }
        }

        // Existing infected trees count toward the requested total.
        int targetCount = Mathf.Max(0, initialInfectedTreeCount);

        while (currentlyInfected < targetCount &&
               candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            InfectableTree selectedTree = candidates[index];

            candidates.RemoveAt(index);

            if (selectedTree.TryInfect())
            {
                currentlyInfected++;
            }
        }
    }

    private void TryRandomInfection()
    {
        List<InfectableTree> candidates =
            new List<InfectableTree>();

        foreach (InfectableTree tree in GetValidTrees())
        {
            if (!tree.IsCurrentlyInfected)
            {
                candidates.Add(tree);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, candidates.Count);
        candidates[index].TryInfect();
    }

    private void UpdateSpreadTimers(
        List<InfectableTree> allTrees,
        float deltaTime)
    {
        List<InfectableTree> dueSources =
            new List<InfectableTree>();

        // Timers are independent. A newly infected tree receives
        // a fresh timer and does not count down during this frame.
        foreach (InfectableTree tree in allTrees)
        {
            if (!tree.IsCurrentlyInfected)
            {
                spreadTimers.Remove(tree);
                continue;
            }

            if (!spreadTimers.TryGetValue(tree, out SpreadTimerState state) ||
                state.infectionCycle != tree.InfectionCycle)
            {
                spreadTimers[tree] = new SpreadTimerState(
                    tree.InfectionCycle,
                    GetSpreadInterval()
                );

                continue;
            }

            state.timeRemaining -= deltaTime;

            if (state.timeRemaining <= 0f)
            {
                dueSources.Add(tree);
            }
        }

        // All sources that are ready this frame share a reservation set.
        // A target cannot be selected by more than one source per cycle.
        HashSet<InfectableTree> reservedTargets =
            new HashSet<InfectableTree>();

        foreach (InfectableTree source in dueSources)
        {
            if (source == null || !source.IsCurrentlyInfected)
            {
                spreadTimers.Remove(source);
                continue;
            }

            TrySpreadFromTree(source, allTrees, reservedTargets);

            // Reset this source's local timer after its attempt,
            // whether or not a suitable target was available.
            if (spreadTimers.TryGetValue(
                source,
                out SpreadTimerState state) &&
                state.infectionCycle == source.InfectionCycle)
            {
                state.timeRemaining = GetSpreadInterval();
            }
        }

        // Remove entries for trees that are no longer in the manager's list.
        HashSet<InfectableTree> validSet =
            new HashSet<InfectableTree>(allTrees);

        List<InfectableTree> toRemove =
            new List<InfectableTree>();

        foreach (KeyValuePair<InfectableTree, SpreadTimerState> entry
                 in spreadTimers)
        {
            if (entry.Key == null ||
                !validSet.Contains(entry.Key) ||
                !entry.Key.IsCurrentlyInfected)
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (InfectableTree tree in toRemove)
        {
            spreadTimers.Remove(tree);
        }
    }

    private void TrySpreadFromTree(
        InfectableTree source,
        List<InfectableTree> allTrees,
        HashSet<InfectableTree> reservedTargets)
    {
        List<InfectableTree> candidates =
            new List<InfectableTree>();

        float radius = Mathf.Max(0f, spreadRadius);
        float radiusSquared = radius * radius;

        Vector3 sourcePosition = source.transform.position;

        foreach (InfectableTree target in allTrees)
        {
            if (target == source ||
                target.IsCurrentlyInfected ||
                reservedTargets.Contains(target))
            {
                continue;
            }

            Vector3 offset =
                target.transform.position - sourcePosition;

            if (offset.sqrMagnitude <= radiusSquared)
            {
                candidates.Add(target);
            }
        }

        // No target in range: this source will retry after its next interval.
        if (candidates.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, candidates.Count);
        InfectableTree selectedTarget = candidates[index];

        if (selectedTarget.TryInfect())
        {
            reservedTargets.Add(selectedTarget);
        }
    }

    private List<InfectableTree> GetValidTrees()
    {
        List<InfectableTree> result =
            new List<InfectableTree>();

        HashSet<InfectableTree> seen =
            new HashSet<InfectableTree>();

        foreach (InfectableTree tree in trees)
        {
            if (tree == null ||
                !tree.isActiveAndEnabled ||
                !tree.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (seen.Add(tree))
            {
                result.Add(tree);
            }
        }

        return result;
    }

    private float GetRandomInfectionInterval()
    {
        return CalculateRandomInterval(
            randomInfectionIntervalMin,
            randomInfectionIntervalMax,
            randomAccelerationEvery,
            randomIntervalDecreasePerStep,
            minimumRandomInfectionInterval
        );
    }

    private float GetSpreadInterval()
    {
        return CalculateRandomInterval(
            spreadIntervalMin,
            spreadIntervalMax,
            spreadAccelerationEvery,
            spreadIntervalDecreasePerStep,
            minimumSpreadInterval
        );
    }

    private float CalculateRandomInterval(
        float intervalMin,
        float intervalMax,
        float accelerationEvery,
        float decreasePerStep,
        float minimumInterval)
    {
        float startMin = Mathf.Max(0.1f, intervalMin);
        float startMax = Mathf.Max(0.1f, intervalMax);

        if (startMin > startMax)
        {
            float temp = startMin;
            startMin = startMax;
            startMax = temp;
        }

        float floor = Mathf.Clamp(
            minimumInterval,
            0.1f,
            startMin
        );

        int accelerationSteps = 0;

        if (accelerationEvery > 0f &&
            decreasePerStep > 0f)
        {
            accelerationSteps = Mathf.FloorToInt(
                elapsedGameTime / accelerationEvery
            );
        }

        float totalDecrease =
            accelerationSteps * decreasePerStep;

        float currentMin = Mathf.Max(
            floor,
            startMin - totalDecrease
        );

        float currentMax = Mathf.Max(
            floor,
            startMax - totalDecrease
        );

        return Random.Range(currentMin, currentMax);
    }

    private bool CheckDefeatCondition()
    {
        if (!canLose)
        {
            return false;
        }

        if (hasLost)
        {
            return true;
        }

        List<InfectableTree> allTrees = GetValidTrees();

        if (allTrees.Count == 0)
        {
            return false;
        }

        int infectedCount = 0;

        foreach (InfectableTree tree in allTrees)
        {
            if (tree.IsCurrentlyInfected)
            {
                infectedCount++;
            }
        }

        bool defeatReached = false;
        float infectedPercentage =
            infectedCount * 100f / allTrees.Count;

        if (defeatMode == InfectionDefeatMode.Percentage)
        {
            defeatReached =
                infectedPercentage >= loseAtInfectedPercentage;
        }
        else
        {
            int threshold = Mathf.Max(1, loseAtInfectedTreeCount);

            defeatReached = infectedCount >= threshold;
        }

        if (!defeatReached)
        {
            return false;
        }

        hasLost = true;

        Debug.LogError(
            $"DEFEAT! Infected trees: {infectedCount}/{allTrees.Count} " +
            $"({infectedPercentage:F1}%).",
            this
        );

        return true;
    }

    private void OnDrawGizmos()
    {
        if (!showSpreadRadiusGizmos || trees == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.25f, 0.15f, 1f);

        float radius = Mathf.Max(0f, spreadRadius);

        HashSet<InfectableTree> drawnTrees =
            new HashSet<InfectableTree>();

        foreach (InfectableTree tree in trees)
        {
            if (tree == null || !drawnTrees.Add(tree))
            {
                continue;
            }

            Gizmos.DrawWireSphere(
                tree.transform.position,
                radius
            );
        }
    }
}
