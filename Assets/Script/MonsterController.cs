
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerVisionOrigin;
    [SerializeField] private Transform monsterVisionTarget;
    [SerializeField] private Transform captureRayOrigin;
    [SerializeField] private MonsterAudioController audioController;
    [SerializeField] private GameDefeatManager defeatManager;
    [SerializeField] private StatueCandleStation statue;

    [Tooltip("Assign the monster model child, not this root object.")]
    [SerializeField] private GameObject monsterVisualRoot;

    [Header("Blood Offering Activation")]
    [Min(0)]
    [SerializeField] private int offeringsRequiredToActivate = 0;

    [Min(0f)]
    [SerializeField] private float speedPerBloodOffering = 2f;

    [Header("Stalker Mode (Testing)")]
    [Tooltip("When enabled, the monster keeps its distance until the player looks at it. Candle teleport re-enables the restriction.")]
    [SerializeField] private bool stalkerModeEnabled = false;

    [Min(0.1f)]
    [SerializeField] private float stalkerMinimumDistance = 12f;

    [Header("Chase")]
    [Min(0.05f)]
    [SerializeField] private float destinationRefreshInterval = 0.2f;

    [Min(1f)]
    [SerializeField] private float agentAngularSpeed = 720f;

    [Header("Continuous Speed Progression")]
    [Min(0.1f)]
    [SerializeField] private float baseSpeed = 5f;

    [Min(0f)]
    [SerializeField] private float speedIncreasePerInterval = 1f;

    [Min(0.1f)]
    [SerializeField] private float secondsPerSpeedIncrease = 10f;

    [SerializeField] private bool limitMaximumSpeed = false;

    [Min(0.1f)]
    [SerializeField] private float maximumSpeed = 20f;

    [Header("Candle Speed Reset")]
    [SerializeField] private bool resetTemporarySpeedWhenTreeCuredByCandle = true;

    [Header("Capture Ray")]
    [Min(0.05f)]
    [SerializeField] private float captureCheckInterval = 0.1f;

    [Min(0.1f)]
    [SerializeField] private float captureRayDistance = 1.6f;

    [Min(0f)]
    [SerializeField] private float captureRayHeight = 1.1f;

    [SerializeField] private LayerMask captureLayers = ~0;

    [Header("Candle Teleport")]
    [Min(0.1f)]
    [SerializeField] private float candleReactionRadius = 8f;

    [Min(0f)]
    [SerializeField] private float minimumTeleportDistanceFromPlayer = 30f;

    [Min(0.1f)]
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Min(1)]
    [SerializeField] private int teleportAttempts = 200;

    [Header("Player Vision")]
    [Min(0.05f)]
    [SerializeField] private float visionCheckInterval = 0.1f;

    [Min(0.1f)]
    [SerializeField] private float visionDistance = 20f;

    [SerializeField] private bool deriveVisionAnglesFromCameraFov = true;

    [Range(1f, 89f)]
    [SerializeField] private float manualHorizontalHalfAngle = 30f;

    [Range(1f, 89f)]
    [SerializeField] private float manualVerticalHalfAngle = 30f;

    [Min(0f)]
    [SerializeField] private float playerVisionOriginHeight = 1.6f;

    [Min(0f)]
    [SerializeField] private float monsterVisionTargetHeight = 1.2f;

    [SerializeField] private LayerMask visionLayers = ~0;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;

    public float CurrentSpeed { get; private set; }

    // Temporary speed bonus gained through elapsed time.
    public float CurrentSpeedBonus { get; private set; }

    public float SpeedProgressElapsed => speedProgressElapsed;

    public bool PlayerCurrentlyVisible { get; private set; }

    public float TimeSincePlayerWasVisible => unseenTime;

    public bool HasSeenPlayerAtLeastOnce => hasSeenPlayerAtLeastOnce;

    public bool IsAgentOnNavMesh =>
        agent != null && agent.isOnNavMesh;

    public bool IsActivated => isActivated;

    public int CurrentBloodOfferings =>
        statue != null
            ? Mathf.Max(0, statue.CurrentBloodDeposits)
            : 0;

    public float EffectiveBaseSpeed =>
        GetBaseSpeedWithOfferingBonus();

    private NavMeshAgent agent;

    private bool initialized;
    private bool caughtPlayer;
    private bool isActivated;

    private bool stalkerRestrictionActive;
    private bool lastStalkerModeEnabled;
    private float originalAgentStoppingDistance;

    private float speedProgressElapsed;
    private float destinationTimer;
    private float captureCheckTimer;
    private float visionCheckTimer;
    private float unseenTime;

    private bool hasSeenPlayerAtLeastOnce;

    private readonly RaycastHit[] raycastHits = new RaycastHit[64];

    private Vector3 debugVisionRayStart;
    private Vector3 debugVisionRayEnd;
    private bool debugVisionRayVisible;
    private bool hasDebugVisionRay;

    private Vector3 debugCaptureRayStart;
    private Vector3 debugCaptureRayEnd;
    private bool debugCaptureRayHitPlayer;
    private bool hasDebugCaptureRay;

    private Vector3[] navVertices;
    private int[] navIndices;
    private int[] eligibleTriangleIndices;
    private float[] cumulativeTriangleAreas;
    private float totalEligibleNavMeshArea;
    private bool navMeshTriangleCacheReady;

    private Renderer[] cachedRenderers;
    private bool[] originalRendererStates;

    private Collider[] cachedColliders;
    private bool[] originalColliderStates;

    private bool interactionPaused;

    private Coroutine interactionPauseCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (audioController == null)
        {
            audioController = GetComponent<MonsterAudioController>();
        }

        if (defeatManager == null)
        {
            defeatManager = FindFirstObjectByType<GameDefeatManager>();
        }

        if (statue == null)
        {
            statue = FindFirstObjectByType<StatueCandleStation>();
        }
    }

    private void Start()
    {
        if (player == null)
        {
            SimpleThirdPersonController playerController =
                FindFirstObjectByType<SimpleThirdPersonController>();

            if (playerController != null)
            {
                player = playerController.transform;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (player == null)
        {
            Debug.LogError(
                "MonsterController: Assign Player or add SimpleThirdPersonController to the player.",
                this
            );

            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "MonsterController: Assign Player Camera or tag it MainCamera.",
                this
            );

            enabled = false;
            return;
        }

        if (defeatManager == null)
        {
            defeatManager = FindFirstObjectByType<GameDefeatManager>();
        }

        if (statue == null && offeringsRequiredToActivate > 0)
        {
            Debug.LogWarning(
                "MonsterController: Assign the statue to control monster activation.",
                this
            );
        }

        agent.angularSpeed = agentAngularSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;
        originalAgentStoppingDistance = agent.stoppingDistance;
        lastStalkerModeEnabled = stalkerModeEnabled;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit spawnHit,
                navMeshSampleDistance * 3f,
                agent.areaMask))
            {
                agent.Warp(spawnHit.position);
            }
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "MonsterController: Monster is not on a baked NavMesh.",
                this
            );

            enabled = false;
            return;
        }

        if (audioController == null)
        {
            Debug.LogWarning(
                "MonsterController: MonsterAudioController is missing.",
                this
            );
        }

        CacheMonsterComponents();
        SetMonsterVisualActive(false);

        BuildNavMeshTriangleCache();

        destinationTimer = 0f;
        captureCheckTimer = 0f;
        visionCheckTimer = 0f;
        unseenTime = 0f;

        PlayerCurrentlyVisible = false;
        initialized = true;

        // The monster waits here until the offering requirement is met.
        agent.isStopped = true;
        ResetSpeedProgression();

        if (HasRequiredOfferings())
        {
            ActivateMonster();
        }
    }

    private void OnEnable()
    {
        MonsterInteractionPause.Register(this);

    }

    private void OnDisable()
    {
        if (audioController != null)
        {
            audioController.StopAllSounds();
        }

        MonsterInteractionPause.Unregister(this);

        if (interactionPauseCoroutine != null)
        {
            StopCoroutine(interactionPauseCoroutine);
            interactionPauseCoroutine = null;
        }

        interactionPaused = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    private void Update()
    {
        if (!initialized || caughtPlayer)
        {
            return;
        }

        if (defeatManager != null && defeatManager.IsDefeated)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            return;
        }

        if (interactionPaused)
        {
            return;
        }

        HandleStalkerModeSettingChange();

        // Do not move, accelerate or scan while waiting for offerings.
        if (!isActivated)
        {
            if (!HasRequiredOfferings())
            {
                return;
            }

            ActivateMonster();
        }

        float deltaTime = Time.deltaTime;

        if (!stalkerRestrictionActive)
        {
            UpdateSpeedProgression(deltaTime);
        }

        UpdateChaseDestination(deltaTime);
        UpdateCaptureRay(deltaTime);
        UpdateVisionScan(deltaTime);
    }


    public void PauseForInteraction(float duration)
    {
        if (!isActivated || caughtPlayer || !initialized)
            return;

        if (interactionPauseCoroutine != null)
            StopCoroutine(interactionPauseCoroutine);

        interactionPauseCoroutine = StartCoroutine(
            InteractionPauseCoroutine(duration)
        );
    }

   private System.Collections.IEnumerator InteractionPauseCoroutine(
    float duration)
{
    interactionPaused = true;

    if (agent != null && agent.enabled && agent.isOnNavMesh)
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    yield return new WaitForSeconds(Mathf.Max(0f, duration));

    interactionPaused = false;
    interactionPauseCoroutine = null;

    // Не возобновляем врага, если игра уже завершена.
    if (caughtPlayer ||
        !initialized ||
        !isActivated ||
        (defeatManager != null && defeatManager.IsDefeated))
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        yield break;
    }

    if (agent != null && agent.enabled && agent.isOnNavMesh)
    {
        agent.isStopped = false;

        if (player != null)
        {
            destinationTimer = 0f;
            agent.SetDestination(player.position);
        }
    }
}

    // ---------------------------------------------------------
    // Activation through blood offerings
    // ---------------------------------------------------------

    private bool HasRequiredOfferings()
    {
        int required = Mathf.Max(0, offeringsRequiredToActivate);

        if (required == 0)
        {
            return true;
        }

        return statue != null &&
               statue.CurrentBloodDeposits >= required;
    }

    public void NotifyBloodOfferingMade(
    StatueCandleStation offeringStatue)
    {
        if (offeringStatue == null)
        {
            return;
        }

        // Use the exact statue that received the offering.
        statue = offeringStatue;

        if (!initialized || caughtPlayer)
        {
            return;
        }

        if (defeatManager != null && defeatManager.IsDefeated)
        {
            return;
        }

        if (!isActivated)
        {
            if (HasRequiredOfferings())
            {
                ActivateMonster();

                Debug.Log(
                    $"Monster activated after {CurrentBloodOfferings} blood offering(s).",
                    this
                );
            }

            return;
        }

        // Recalculate speed immediately after a new offering.
        // Existing temporary acceleration is preserved.
        UpdateSpeedProgression(0f);
    }

    private void ActivateMonster()
    {
        if (isActivated)
        {
            return;
        }

        isActivated = true;

        stalkerRestrictionActive = stalkerModeEnabled;
        ApplyStalkerStoppingDistance();

        SetMonsterVisualActive(true);
        ResetSpeedProgression();

        destinationTimer = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (player != null)
            {
                agent.SetDestination(player.position);
            }
        }

        // Check immediately on activation, so the first sighting
        // can play the scare sound without waiting for another interval.
        ScanPlayerVision();
    }

    private void OnDestroy()
    {
        MonsterInteractionPause.Unregister(this);
    }

    // ---------------------------------------------------------
    // Monster visual activation
    // ---------------------------------------------------------

    private void CacheMonsterComponents()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            originalRendererStates[i] = cachedRenderers[i].enabled;
        }

        cachedColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[cachedColliders.Length];

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            originalColliderStates[i] = cachedColliders[i].enabled;
        }
    }

    private void SetMonsterVisualActive(bool active)
    {
        // Never deactivate the GameObject that owns this controller.
        bool hasSeparateVisualRoot =
            monsterVisualRoot != null &&
            monsterVisualRoot != gameObject;

        if (hasSeparateVisualRoot)
        {
            monsterVisualRoot.SetActive(active);
        }
        else if (cachedRenderers != null)
        {
            // Fallback if no dedicated visual child was assigned.
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled =
                        active && originalRendererStates[i];
                }
            }
        }

        // An inactive monster should not block vision rays or
        // interact with the player through its physical colliders.
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled =
                        active && originalColliderStates[i];
                }
            }
        }
    }

    // ---------------------------------------------------------
    // Stalker mode
    // ---------------------------------------------------------

    private void HandleStalkerModeSettingChange()
    {
        if (stalkerModeEnabled == lastStalkerModeEnabled)
        {
            return;
        }

        lastStalkerModeEnabled = stalkerModeEnabled;

        // If toggled before activation, ActivateMonster will use the new value.
        if (!isActivated)
        {
            return;
        }

        if (stalkerModeEnabled)
        {
            SetStalkerRestriction(true);
        }
        else
        {
            SetStalkerRestriction(false);
        }
    }

    private void SetStalkerRestriction(bool active)
    {
        bool wasRestricted = stalkerRestrictionActive;
        stalkerRestrictionActive = stalkerModeEnabled && active;
        ApplyStalkerStoppingDistance();

        // Play the scare sound exactly when the stalking restriction ends.
        // No visibility timer is involved. This also covers disabling the
        // stalker mode from the Inspector while the restriction is active.
        if (wasRestricted && !stalkerRestrictionActive && audioController != null)
        {
            audioController.PlayScareSound();
        }

        if (agent != null && agent.isOnNavMesh && player != null)
        {
            destinationTimer = 0f;
            agent.SetDestination(player.position);
        }
    }

    private void ApplyStalkerStoppingDistance()
    {
        if (agent == null)
        {
            return;
        }

        agent.stoppingDistance = stalkerRestrictionActive && stalkerModeEnabled
            ? Mathf.Max(0.1f, stalkerMinimumDistance)
            : originalAgentStoppingDistance;
    }

    // ---------------------------------------------------------
    // Continuous speed progression
    // ---------------------------------------------------------

    private float GetBaseSpeedWithOfferingBonus()
    {
        if (!isActivated)
            return Mathf.Max(0.1f, baseSpeed);

        int offeringsAfterActivation = Mathf.Max(
            0,
            CurrentBloodOfferings - Mathf.Max(0, offeringsRequiredToActivate)
        );

        float permanentBonus =
            Mathf.Max(0f, speedPerBloodOffering) * offeringsAfterActivation;

        return Mathf.Max(0.1f, baseSpeed) + permanentBonus;
    }

    private void UpdateSpeedProgression(float deltaTime)
    {
        speedProgressElapsed += deltaTime;

        float interval = Mathf.Max(0.1f, secondsPerSpeedIncrease);

        // The temporary bonus grows continuously rather than in steps.
        float temporaryBonus =
            Mathf.Max(0f, speedIncreasePerInterval) *
            (speedProgressElapsed / interval);

        float effectiveBaseSpeed = GetBaseSpeedWithOfferingBonus();

        float speed = effectiveBaseSpeed + temporaryBonus;

        if (limitMaximumSpeed)
        {
            speed = Mathf.Min(
                speed,
                Mathf.Max(effectiveBaseSpeed, maximumSpeed)
            );
        }

        CurrentSpeed = speed;

        // Keep the existing property as the actual temporary bonus.
        CurrentSpeedBonus = Mathf.Max(
            0f,
            speed - effectiveBaseSpeed
        );

        if (agent != null)
        {
            agent.speed = CurrentSpeed;
        }
    }

    private void ResetSpeedProgression()
    {
        speedProgressElapsed = 0f;
        CurrentSpeedBonus = 0f;

        // Permanent offering bonuses remain after a reset.
        CurrentSpeed = GetBaseSpeedWithOfferingBonus();

        if (agent != null)
        {
            agent.speed = CurrentSpeed;
        }
    }

    // Called when a candle successfully cures at least one infected tree.
    public void NotifyTreeCuredByCandle()
    {
        if (!initialized ||
            !isActivated ||
            !resetTemporarySpeedWhenTreeCuredByCandle)
        {
            return;
        }

        ResetSpeedProgression();

        // Keep the monster chasing after the speed reset.
        destinationTimer = 0f;

        if (agent != null &&
            agent.isOnNavMesh &&
            player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    // ---------------------------------------------------------
    // Chase
    // ---------------------------------------------------------

    private void UpdateChaseDestination(float deltaTime)
    {
        destinationTimer -= deltaTime;

        if (destinationTimer > 0f)
        {
            return;
        }

        destinationTimer = Mathf.Max(
            0.05f,
            destinationRefreshInterval
        );

        if (agent.isOnNavMesh &&
            player != null &&
            !agent.isStopped)
        {
            agent.SetDestination(player.position);
        }
    }

    // ---------------------------------------------------------
    // Capture ray
    // ---------------------------------------------------------

    private void UpdateCaptureRay(float deltaTime)
    {
        captureCheckTimer -= deltaTime;

        if (captureCheckTimer > 0f)
        {
            return;
        }

        captureCheckTimer = Mathf.Max(
            0.05f,
            captureCheckInterval
        );

        CheckCaptureRay();
    }

    private void CheckCaptureRay()
    {
        if (caughtPlayer || player == null)
        {
            return;
        }

        Vector3 origin = GetCaptureRayOrigin();
        Vector3 direction = transform.forward.normalized;
        float distance = Mathf.Max(0.1f, captureRayDistance);

        int hitCount = Physics.RaycastNonAlloc(
            new Ray(origin, direction),
            raycastHits,
            distance,
            captureLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.PositiveInfinity;
        Transform closestTransform = null;

        Vector3 closestPoint = origin + direction * distance;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = raycastHits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            Transform hitTransform = hitCollider.transform;

            // Ignore the monster's own colliders.
            if (IsPartOfMonster(hitTransform))
            {
                continue;
            }

            if (raycastHits[i].distance < closestDistance)
            {
                closestDistance = raycastHits[i].distance;
                closestTransform = hitTransform;
                closestPoint = raycastHits[i].point;
            }
        }

        debugCaptureRayStart = origin;
        debugCaptureRayEnd = closestPoint;

        debugCaptureRayHitPlayer =
            closestTransform != null &&
            IsPartOfPlayer(closestTransform);

        hasDebugCaptureRay = true;

        // The closest collider blocks the ray.
        if (debugCaptureRayHitPlayer)
        {
            TriggerMonsterDefeat();
        }
    }

    private Vector3 GetCaptureRayOrigin()
    {
        return captureRayOrigin != null
            ? captureRayOrigin.position
            : transform.position + Vector3.up * captureRayHeight;
    }

    private bool IsPartOfMonster(Transform candidate)
    {
        return candidate == transform ||
               candidate.IsChildOf(transform);
    }

    private bool IsPartOfPlayer(Transform candidate)
    {
        return player != null &&
               candidate != null &&
               (candidate == player || candidate.IsChildOf(player));
    }

    private void TriggerMonsterDefeat()
    {
        if (caughtPlayer)
        {
            return;
        }

        caughtPlayer = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (defeatManager != null)
        {
            defeatManager.TriggerDefeat(
                GameDefeatReason.MonsterCaught,
                "The monster's forward ray hit the player.",
                this
            );
        }
        else
        {
            Debug.LogError(
                "DEFEAT! The monster caught the player. " +
                "Add GameDefeatManager to the scene.",
                this
            );
        }
    }

    // ---------------------------------------------------------
    // Player vision
    // ---------------------------------------------------------

    private void UpdateVisionScan(float deltaTime)
    {
        if (hasSeenPlayerAtLeastOnce && !PlayerCurrentlyVisible)
        {
            unseenTime += deltaTime;
        }

        visionCheckTimer -= deltaTime;

        if (visionCheckTimer > 0f)
        {
            return;
        }

        visionCheckTimer = Mathf.Max(
            0.05f,
            visionCheckInterval
        );

        ScanPlayerVision();
    }

    private void ScanPlayerVision()
    {
        bool isVisible = EvaluatePlayerVisibility();

        if (isVisible)
        {
            hasSeenPlayerAtLeastOnce = true;
            unseenTime = 0f;
        }

        PlayerCurrentlyVisible = isVisible;

        // The same visibility check removes the stalking restriction.
        // SetStalkerRestriction plays the scare sound on that transition.
        // No first-sighting or elapsed-time sound logic remains.
        if (isVisible && stalkerRestrictionActive)
        {
            SetStalkerRestriction(false);
        }
    }

    private bool EvaluatePlayerVisibility()
    {
        if (player == null || playerCamera == null)
        {
            hasDebugVisionRay = false;
            return false;
        }

        Vector3 origin = GetPlayerVisionOrigin();
        Vector3 target = GetMonsterVisionTarget();
        Vector3 toTarget = target - origin;

        float distance = toTarget.magnitude;

        debugVisionRayStart = origin;
        hasDebugVisionRay = true;
        debugVisionRayVisible = false;

        if (distance <= 0.01f || distance > visionDistance)
        {
            debugVisionRayEnd =
                origin + playerCamera.transform.forward * visionDistance;

            return false;
        }

        Vector3 direction = toTarget / distance;

        Vector3 localDirection =
            playerCamera.transform.InverseTransformDirection(direction);

        if (localDirection.z <= 0f)
        {
            debugVisionRayEnd =
                origin + playerCamera.transform.forward * visionDistance;

            return false;
        }

        float horizontalAngle = Mathf.Atan2(
            localDirection.x,
            localDirection.z
        ) * Mathf.Rad2Deg;

        float verticalAngle = Mathf.Atan2(
            localDirection.y,
            Mathf.Sqrt(
                localDirection.x * localDirection.x +
                localDirection.z * localDirection.z
            )
        ) * Mathf.Rad2Deg;

        GetVisionHalfAngles(
            out float horizontalLimit,
            out float verticalLimit
        );

        if (Mathf.Abs(horizontalAngle) > horizontalLimit ||
            Mathf.Abs(verticalAngle) > verticalLimit)
        {
            debugVisionRayEnd =
                origin + playerCamera.transform.forward * visionDistance;

            return false;
        }

        int hitCount = Physics.RaycastNonAlloc(
            new Ray(origin, direction),
            raycastHits,
            distance + 0.05f,
            visionLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.PositiveInfinity;
        Transform closestTransform = null;

        Vector3 closestPoint = origin + direction * distance;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = raycastHits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            Transform hitTransform = hitCollider.transform;

            // Ignore player's own colliders.
            if (IsPartOfPlayer(hitTransform))
            {
                continue;
            }

            if (raycastHits[i].distance < closestDistance)
            {
                closestDistance = raycastHits[i].distance;
                closestTransform = hitTransform;
                closestPoint = raycastHits[i].point;
            }
        }

        debugVisionRayEnd = closestPoint;

        bool targetReached =
            closestTransform != null &&
            IsPartOfMonster(closestTransform);

        debugVisionRayVisible = targetReached;

        return targetReached;
    }

    private Vector3 GetPlayerVisionOrigin()
    {
        return playerVisionOrigin != null
            ? playerVisionOrigin.position
            : player.position + Vector3.up * playerVisionOriginHeight;
    }

    private Vector3 GetMonsterVisionTarget()
    {
        return monsterVisionTarget != null
            ? monsterVisionTarget.position
            : transform.position + Vector3.up * monsterVisionTargetHeight;
    }

    private void GetVisionHalfAngles(
        out float horizontal,
        out float vertical)
    {
        if (deriveVisionAnglesFromCameraFov && playerCamera != null)
        {
            // Use half of the camera FOV for both axes as requested.
            horizontal = playerCamera.fieldOfView * 0.5f;
            vertical = playerCamera.fieldOfView * 0.5f;
        }
        else
        {
            horizontal = manualHorizontalHalfAngle;
            vertical = manualVerticalHalfAngle;
        }

        horizontal = Mathf.Clamp(horizontal, 1f, 89f);
        vertical = Mathf.Clamp(vertical, 1f, 89f);
    }

    // ---------------------------------------------------------
    // Candle: teleport and temporary speed reset
    // ---------------------------------------------------------

    public bool TryTeleportFromCandle(Vector3 candleUserPosition)
    {
        if (!initialized ||
            !isActivated ||
            caughtPlayer ||
            player == null ||
            agent == null ||
            !agent.isOnNavMesh)
        {
            return false;
        }

        if (defeatManager != null && defeatManager.IsDefeated)
        {
            return false;
        }

        float distanceToUser = HorizontalDistance(
            transform.position,
            candleUserPosition
        );

        if (distanceToUser > candleReactionRadius)
        {
            return false;
        }

        if (!TryFindValidTeleportPosition(out Vector3 teleportPosition))
        {
            Debug.LogWarning(
                "MonsterController: Could not find a valid NavMesh " +
                "teleport point beyond the minimum distance.",
                this
            );

            return false;
        }

        if (!agent.Warp(teleportPosition))
        {
            return false;
        }

        agent.ResetPath();
        agent.isStopped = interactionPaused;

        // A successful teleport from a candle ALWAYS resets temporary
        // speed. This is independent of the tree-cure checkbox.
        ResetSpeedProgression();

        // In stalker mode, every successful candle teleport starts a new
        // stalking phase, until the player sees the monster again.
        if (stalkerModeEnabled)
        {
            SetStalkerRestriction(true);
        }
        else
        {
            SetStalkerRestriction(false);
        }

        destinationTimer = 0f;
        agent.SetDestination(player.position);

        return true;
    }

    private bool TryFindValidTeleportPosition(out Vector3 result)
    {
        result = transform.position;

        if (!navMeshTriangleCacheReady ||
            totalEligibleNavMeshArea <= 0f)
        {
            BuildNavMeshTriangleCache();
        }

        if (!navMeshTriangleCacheReady ||
            totalEligibleNavMeshArea <= 0f)
        {
            return false;
        }

        float minimumDistance =
            Mathf.Max(0f, minimumTeleportDistanceFromPlayer);

        int attempts = Mathf.Max(1, teleportAttempts);

        for (int i = 0; i < attempts; i++)
        {
            if (!TryGetRandomPointOnNavMesh(out Vector3 candidate))
            {
                continue;
            }

            if (HorizontalDistance(candidate, player.position) <
                minimumDistance)
            {
                continue;
            }

            result = candidate;
            return true;
        }

        return false;
    }

    private void BuildNavMeshTriangleCache()
    {
        navMeshTriangleCacheReady = false;
        totalEligibleNavMeshArea = 0f;

        navVertices = null;
        navIndices = null;
        eligibleTriangleIndices = null;
        cumulativeTriangleAreas = null;

        if (agent == null)
        {
            return;
        }

        NavMeshTriangulation triangulation =
            NavMesh.CalculateTriangulation();

        if (triangulation.vertices == null ||
            triangulation.indices == null ||
            triangulation.vertices.Length == 0 ||
            triangulation.indices.Length < 3)
        {
            return;
        }

        navVertices = triangulation.vertices;
        navIndices = triangulation.indices;

        List<int> triangleList = new List<int>();
        List<float> cumulativeAreaList = new List<float>();

        int triangleCount = triangulation.indices.Length / 3;

        for (int triangle = 0; triangle < triangleCount; triangle++)
        {
            if (triangulation.areas != null &&
                triangulation.areas.Length > triangle)
            {
                int areaIndex = triangulation.areas[triangle];

                if (areaIndex < 0 ||
                    areaIndex > 31 ||
                    (agent.areaMask & (1 << areaIndex)) == 0)
                {
                    continue;
                }
            }

            int indexStart = triangle * 3;

            Vector3 a = navVertices[navIndices[indexStart]];
            Vector3 b = navVertices[navIndices[indexStart + 1]];
            Vector3 c = navVertices[navIndices[indexStart + 2]];

            float area =
                Vector3.Cross(b - a, c - a).magnitude * 0.5f;

            if (area <= 0.0001f)
            {
                continue;
            }

            totalEligibleNavMeshArea += area;

            triangleList.Add(triangle);
            cumulativeAreaList.Add(totalEligibleNavMeshArea);
        }

        if (triangleList.Count == 0 ||
            totalEligibleNavMeshArea <= 0f)
        {
            return;
        }

        eligibleTriangleIndices = triangleList.ToArray();
        cumulativeTriangleAreas = cumulativeAreaList.ToArray();

        navMeshTriangleCacheReady = true;
    }

    private bool TryGetRandomPointOnNavMesh(out Vector3 point)
    {
        point = Vector3.zero;

        if (!navMeshTriangleCacheReady ||
            eligibleTriangleIndices == null ||
            eligibleTriangleIndices.Length == 0)
        {
            return false;
        }

        float areaRoll =
            Random.Range(0f, totalEligibleNavMeshArea);

        int low = 0;
        int high = cumulativeTriangleAreas.Length - 1;

        while (low < high)
        {
            int middle = (low + high) / 2;

            if (areaRoll <= cumulativeTriangleAreas[middle])
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        int triangle = eligibleTriangleIndices[low];
        int indexStart = triangle * 3;

        Vector3 a = navVertices[navIndices[indexStart]];
        Vector3 b = navVertices[navIndices[indexStart + 1]];
        Vector3 c = navVertices[navIndices[indexStart + 2]];

        // Uniform random point within the selected triangle.
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;

        point =
            (1f - r1) * a +
            (r1 * (1f - r2)) * b +
            (r1 * r2) * c;

        if (NavMesh.SamplePosition(
            point,
            out NavMeshHit hit,
            navMeshSampleDistance,
            agent.areaMask))
        {
            point = hit.position;
            return true;
        }

        return false;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    // ---------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!showGizmos)
        {
            return;
        }

        Vector3 captureOrigin = captureRayOrigin != null
            ? captureRayOrigin.position
            : transform.position + Vector3.up * captureRayHeight;

        Gizmos.color =
            Application.isPlaying && debugCaptureRayHitPlayer
                ? Color.green
                : Color.red;

        Vector3 captureEnd =
            Application.isPlaying && hasDebugCaptureRay
                ? debugCaptureRayEnd
                : captureOrigin +
                  transform.forward * captureRayDistance;

        Gizmos.DrawLine(captureOrigin, captureEnd);
        Gizmos.DrawWireSphere(captureEnd, 0.08f);

        // Candle reaction radius around the monster.
        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            candleReactionRadius
        );

        if (stalkerModeEnabled && player != null)
        {
            // Orange while the stalking restriction is active; green after
            // the player has spotted the monster.
            Gizmos.color = Application.isPlaying && !stalkerRestrictionActive
                ? Color.green
                : new Color(1f, 0.5f, 0f, 1f);

            Gizmos.DrawWireSphere(
                player.position,
                Mathf.Max(0.1f, stalkerMinimumDistance)
            );
        }

        if (player != null)
        {
            Vector3 visionOrigin = playerVisionOrigin != null
                ? playerVisionOrigin.position
                : player.position +
                  Vector3.up * playerVisionOriginHeight;

            // Maximum vision distance.
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);

            Gizmos.DrawWireSphere(visionOrigin, visionDistance);

            if (playerCamera != null)
            {
                GetVisionHalfAngles(
                    out float horizontal,
                    out float vertical
                );

                Quaternion cameraRotation =
                    playerCamera.transform.rotation;

                Vector3 topLeft =
                    visionOrigin +
                    VisionDirection(cameraRotation, -horizontal, vertical) *
                    visionDistance;

                Vector3 topRight =
                    visionOrigin +
                    VisionDirection(cameraRotation, horizontal, vertical) *
                    visionDistance;

                Vector3 bottomLeft =
                    visionOrigin +
                    VisionDirection(cameraRotation, -horizontal, -vertical) *
                    visionDistance;

                Vector3 bottomRight =
                    visionOrigin +
                    VisionDirection(cameraRotation, horizontal, -vertical) *
                    visionDistance;

                Gizmos.color = Color.cyan;

                Gizmos.DrawLine(visionOrigin, topLeft);
                Gizmos.DrawLine(visionOrigin, topRight);
                Gizmos.DrawLine(visionOrigin, bottomLeft);
                Gizmos.DrawLine(visionOrigin, bottomRight);

                Gizmos.DrawLine(topLeft, topRight);
                Gizmos.DrawLine(topRight, bottomRight);
                Gizmos.DrawLine(bottomRight, bottomLeft);
                Gizmos.DrawLine(bottomLeft, topLeft);
            }

            // Last actual visibility ray.
            if (Application.isPlaying && hasDebugVisionRay)
            {
                Gizmos.color =
                    debugVisionRayVisible ? Color.green : Color.red;

                Gizmos.DrawLine(
                    debugVisionRayStart,
                    debugVisionRayEnd
                );

                Gizmos.DrawWireSphere(debugVisionRayEnd, 0.06f);
            }

            // Minimum allowed teleport distance from the player.
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                player.position,
                minimumTeleportDistanceFromPlayer
            );
        }
    }

    private static Vector3 VisionDirection(
        Quaternion cameraRotation,
        float yawDegrees,
        float pitchDegrees)
    {
        float yaw = yawDegrees * Mathf.Deg2Rad;
        float pitch = pitchDegrees * Mathf.Deg2Rad;

        Vector3 localDirection = new Vector3(
            Mathf.Sin(yaw) * Mathf.Cos(pitch),
            Mathf.Sin(pitch),
            Mathf.Cos(yaw) * Mathf.Cos(pitch)
        );

        return cameraRotation * localDirection.normalized;
    }

    
}
