
using System.Collections.Generic;
using UnityEngine;

public enum InfectionDefeatMode
{
    Percentage,
    TreeCount
}

public class InfectionManager : MonoBehaviour
{
    [Header("Shared Defeat")]
    [SerializeField] private GameDefeatManager defeatManager;

    [Header("Trees")]
    [SerializeField]
    private List<InfectableTree> trees =
        new List<InfectableTree>();

    [Header("Audio")]
    [SerializeField] private AudioSource infectionAudioSource;
    [SerializeField] private AudioClip infectionSound;
    [SerializeField] private AudioClip bleedingSound;

    [Range(0f, 1f)]
    [SerializeField] private float infectionSoundVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float bleedingSoundVolume = 1f;

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
        spreadTimers =
            new Dictionary<InfectableTree, SpreadTimerState>();

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

    // ---------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------

    private void Awake()
    {
        if (infectionAudioSource == null)
        {
            infectionAudioSource = GetComponent<AudioSource>();
        }

        if (infectionAudioSource == null)
        {
            infectionAudioSource = gameObject.AddComponent<AudioSource>();
        }

        infectionAudioSource.playOnAwake = false;

        if (defeatManager == null)
        {
            defeatManager =
                FindFirstObjectByType<GameDefeatManager>();
        }
    }

    private void OnEnable()
    {
        SubscribeToTreeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromTreeEvents();
    }

    private void Start()
    {
        InitializeStartingInfections();

        randomInfectionTimer = GetRandomInfectionInterval();

        CheckDefeatCondition();
    }

    private void Update()
    {
        // Stop if this manager has already registered forest defeat.
        if (hasLost)
        {
            return;
        }

        // Also stop if another system, such as the monster,
        // has already caused the shared defeat.
        if (defeatManager != null && defeatManager.IsDefeated)
        {
            hasLost = true;
            return;
        }

        float deltaTime = Time.deltaTime;
        elapsedGameTime += deltaTime;

        List<InfectableTree> allTrees = GetValidTrees();

        // Each infected tree owns its own spread timer.
        if (enableSpread)
        {
            UpdateSpreadTimers(allTrees, deltaTime);
        }

        // Check defeat before processing another random infection.
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

    // ---------------------------------------------------------
    // Tree event subscriptions and audio
    // ---------------------------------------------------------

    private void SubscribeToTreeEvents()
    {
        HashSet<InfectableTree> subscribedTrees =
            new HashSet<InfectableTree>();

        foreach (InfectableTree tree in trees)
        {
            if (tree == null || !subscribedTrees.Add(tree))
            {
                continue;
            }

            tree.BleedingStarted += HandleTreeStartedBleeding;
        }
    }

    private void UnsubscribeFromTreeEvents()
    {
        HashSet<InfectableTree> unsubscribedTrees =
            new HashSet<InfectableTree>();

        foreach (InfectableTree tree in trees)
        {
            if (tree == null || !unsubscribedTrees.Add(tree))
            {
                continue;
            }

            tree.BleedingStarted -= HandleTreeStartedBleeding;
        }
    }

    private void HandleTreeStartedBleeding(InfectableTree tree)
    {
        PlayAudio(bleedingSound, bleedingSoundVolume);
    }

    private void PlayAudio(AudioClip clip, float volume)
    {
        if (clip == null || infectionAudioSource == null)
        {
            return;
        }

        infectionAudioSource.Stop();
        infectionAudioSource.PlayOneShot(clip, volume);
    }

    // ---------------------------------------------------------
    // Public read-only information for the debug HUD
    // ---------------------------------------------------------

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

        float totalDecrease = steps * decreasePerStep;

        currentMin = Mathf.Max(
            floor,
            startMin - totalDecrease
        );

        currentMax = Mathf.Max(
            floor,
            startMax - totalDecrease
        );
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

        secondsRemaining = Mathf.Max(
            0f,
            state.timeRemaining
        );

        return true;
    }

    // ---------------------------------------------------------
    // Initial infection
    // ---------------------------------------------------------

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
        int targetCount = Mathf.Max(
            0,
            initialInfectedTreeCount
        );

        while (currentlyInfected < targetCount &&
               candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);

            InfectableTree selectedTree = candidates[index];

            candidates.RemoveAt(index);

            if (TryInfectTree(selectedTree))
            {
                currentlyInfected++;
            }
        }
    }

    // ---------------------------------------------------------
    // Random infection
    // ---------------------------------------------------------

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

        TryInfectTree(candidates[index]);
    }

    // ---------------------------------------------------------
    // Independent spread timers
    // ---------------------------------------------------------

    private void UpdateSpreadTimers(
        List<InfectableTree> allTrees,
        float deltaTime)
    {
        List<InfectableTree> dueSources =
            new List<InfectableTree>();

        // Initialize a new timer for each newly infected tree.
        // Its timer does not count down on its initialization frame.
        foreach (InfectableTree tree in allTrees)
        {
            if (!tree.IsCurrentlyInfected)
            {
                spreadTimers.Remove(tree);
                continue;
            }

            if (!spreadTimers.TryGetValue(
                    tree,
                    out SpreadTimerState state) ||
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

        // Each target can only be selected once in this spread cycle.
        HashSet<InfectableTree> reservedTargets =
            new HashSet<InfectableTree>();

        foreach (InfectableTree source in dueSources)
        {
            if (source == null || !source.IsCurrentlyInfected)
            {
                spreadTimers.Remove(source);
                continue;
            }

            TrySpreadFromTree(
                source,
                allTrees,
                reservedTargets
            );

            // Reset the source timer after an attempt, even if no
            // eligible target was available.
            if (spreadTimers.TryGetValue(
                    source,
                    out SpreadTimerState state) &&
                state.infectionCycle == source.InfectionCycle)
            {
                state.timeRemaining = GetSpreadInterval();
            }
        }

        // Remove timers for inactive, removed or cured trees.
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

        // No suitable neighbour: the source will try again
        // after its next local timer expires.
        if (candidates.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, candidates.Count);

        InfectableTree selectedTarget = candidates[index];

        if (TryInfectTree(selectedTarget))
        {
            reservedTargets.Add(selectedTarget);
        }
    }

    // ---------------------------------------------------------
    // Tree infection and audio
    // ---------------------------------------------------------

    private bool TryInfectTree(InfectableTree tree)
    {
        if (tree == null || !tree.TryInfect())
        {
            return false;
        }

        PlayAudio(infectionSound, infectionSoundVolume);

        return true;
    }

    // ---------------------------------------------------------
    // Tree list
    // ---------------------------------------------------------

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

    // ---------------------------------------------------------
    // Interval calculation and acceleration
    // ---------------------------------------------------------

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

    // ---------------------------------------------------------
    // Defeat condition
    // ---------------------------------------------------------

    private bool CheckDefeatCondition()
    {
        // Respect defeat triggered by any other system.
        if (defeatManager != null && defeatManager.IsDefeated)
        {
            hasLost = true;
            return true;
        }

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

        float infectedPercentage =
            infectedCount * 100f / allTrees.Count;

        bool defeatReached;

        if (defeatMode == InfectionDefeatMode.Percentage)
        {
            defeatReached =
                infectedPercentage >= loseAtInfectedPercentage;
        }
        else
        {
            int threshold = Mathf.Max(
                1,
                loseAtInfectedTreeCount
            );

            defeatReached = infectedCount >= threshold;
        }

        if (!defeatReached)
        {
            return false;
        }

        hasLost = true;

        string details =
            $"Infected trees: {infectedCount}/{allTrees.Count} " +
            $"({infectedPercentage:F1}%).";

        if (defeatManager != null)
        {
            defeatManager.TriggerDefeat(
                GameDefeatReason.ForestOverrun,
                details,
                this
            );
        }
        else
        {
            Debug.LogError(
                "DEFEAT! The forest has been overrun by infection. " +
                details,
                this
            );
        }

        return true;
    }

    // ---------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!showSpreadRadiusGizmos || trees == null)
        {
            return;
        }

        Gizmos.color = new Color(
            1f,
            0.25f,
            0.15f,
            1f
        );

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
