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

    [Min(0f)]
    [SerializeField] private float unseenSecondsForScareSound = 20f;

    [SerializeField] private LayerMask visionLayers = ~0;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;

    public float CurrentSpeed { get; private set; }
    public float CurrentSpeedBonus { get; private set; }

    public float SpeedProgressElapsed => speedProgressElapsed;

    public bool PlayerCurrentlyVisible { get; private set; }

    public float TimeSincePlayerWasVisible => unseenTime;

    public bool HasSeenPlayerAtLeastOnce => hasSeenPlayerAtLeastOnce;

    public bool IsAgentOnNavMesh =>
        agent != null && agent.isOnNavMesh;

    private NavMeshAgent agent;

    private bool initialized;
    private bool caughtPlayer;

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

        agent.angularSpeed = agentAngularSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;

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

        BuildNavMeshTriangleCache();

        CurrentSpeed = Mathf.Max(0.1f, baseSpeed);
        agent.speed = CurrentSpeed;

        agent.SetDestination(player.position);

        destinationTimer = 0f;
        captureCheckTimer = 0f;
        visionCheckTimer = 0f;
        unseenTime = 0f;

        PlayerCurrentlyVisible = false;
        initialized = true;

        // Check visibility immediately, including the first encounter.
        ScanPlayerVision();
    }

    private void Update()
    {
        if (!initialized || caughtPlayer)
        {
            return;
        }

        if (defeatManager != null && defeatManager.IsDefeated)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            return;
        }

        float deltaTime = Time.deltaTime;

        UpdateSpeedProgression(deltaTime);
        UpdateChaseDestination(deltaTime);
        UpdateCaptureRay(deltaTime);
        UpdateVisionScan(deltaTime);
    }

    // ---------------------------------------------------------
    // Continuous speed progression
    // ---------------------------------------------------------

    private void UpdateSpeedProgression(float deltaTime)
    {
        speedProgressElapsed += deltaTime;

        float interval = Mathf.Max(0.1f, secondsPerSpeedIncrease);

        float bonus =
            Mathf.Max(0f, speedIncreasePerInterval) *
            (speedProgressElapsed / interval);

        float speed = Mathf.Max(0.1f, baseSpeed) + bonus;

        if (limitMaximumSpeed)
        {
            speed = Mathf.Min(
                speed,
                Mathf.Max(baseSpeed, maximumSpeed)
            );
        }

        CurrentSpeed = speed;
        CurrentSpeedBonus = speed - Mathf.Max(0.1f, baseSpeed);

        if (agent != null)
        {
            agent.speed = CurrentSpeed;
        }
    }

    private void ResetSpeedProgression()
    {
        speedProgressElapsed = 0f;
        CurrentSpeedBonus = 0f;
        CurrentSpeed = Mathf.Max(0.1f, baseSpeed);

        if (agent != null)
        {
            agent.speed = CurrentSpeed;
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

        if (agent.isOnNavMesh && player != null && !agent.isStopped)
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

        // The nearest collider blocks the ray.
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
        bool wasVisible = PlayerCurrentlyVisible;
        bool isVisible = EvaluatePlayerVisibility();

        if (isVisible)
        {
            bool firstDiscovery = !hasSeenPlayerAtLeastOnce;

            bool returnedAfterLongAbsence =
                hasSeenPlayerAtLeastOnce &&
                !wasVisible &&
                unseenTime >= Mathf.Max(0f, unseenSecondsForScareSound);

            if ((firstDiscovery || returnedAfterLongAbsence) &&
                audioController != null)
            {
                audioController.PlayScareSound();
            }

            hasSeenPlayerAtLeastOnce = true;
            unseenTime = 0f;
        }

        PlayerCurrentlyVisible = isVisible;
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
            // Per requested behavior, use half FOV for both axes.
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
    // Candle: teleport and reset speed
    // ---------------------------------------------------------

    public bool TryTeleportFromCandle(Vector3 candleUserPosition)
    {
        if (!initialized ||
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
        agent.isStopped = false;

        // A successful candle teleport removes all accumulated speed bonus.
        ResetSpeedProgression();

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

        // Monster's forward capture ray.
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

    private void OnDisable()
    {
        if (audioController != null)
        {
            audioController.StopAllSounds();
        }
    }
}