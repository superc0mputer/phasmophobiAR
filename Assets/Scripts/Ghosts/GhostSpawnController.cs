using System;
using System.Collections.Generic;
using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace PhasmophobiAR.Ghosts
{
    public enum GhostSpawnSource
    {
        ScanCandidate,
        EstimatedBoundsFallback,
        CameraFallback
    }

    public sealed class GhostSpawnInfo
    {
        public Transform ghostTransform;
        public Transform anchorTransform;
        public Pose worldPose;
        public ARAnchor anchor;
        public GhostSpawnSource source;
        public string reason;
        public float score;
        public bool hasARAnchor;

        public Vector3 WorldPosition => anchor != null
            ? anchor.transform.position
            : anchorTransform != null ? anchorTransform.position : worldPose.position;

        public Quaternion WorldRotation => anchor != null
            ? anchor.transform.rotation
            : anchorTransform != null ? anchorTransform.rotation : worldPose.rotation;
    }

    public sealed class GhostSpawnController : MonoBehaviour
    {
        public static GhostSpawnController Instance { get; private set; }

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GhostCaseController m_GhostCaseController;

        [SerializeField]
        GameObject m_GhostPrefab;

        [SerializeField]
        Transform m_ARCamera;

        [SerializeField]
        ARAnchorManager m_AnchorManager;

        [Header("Spawn")]
        [SerializeField]
        int m_SpawnCount = 1;

        [SerializeField]
        float m_SpawnRadiusMeters = 2.5f;

        [SerializeField]
        float m_SpawnHeightOffsetMeters = 0.25f;

        [SerializeField]
        float m_MinimumDistanceFromPlayerMeters = 1.25f;

        [SerializeField]
        float m_FallbackDistanceMeters = 2.5f;

        [SerializeField]
        float m_MinimumCandidateSeparationMeters = 0.5f;

        [SerializeField]
        bool m_RandomizeSpawnCandidateOrder = true;

        [SerializeField]
        int m_RandomizedSpawnCandidatePoolSize = 4;

        [SerializeField]
        float m_MaxHeightAboveCameraMeters = 0.75f;

        [SerializeField]
        float m_MaxHeightBelowCameraMeters = 2.5f;

        [Header("Debug")]
        [SerializeField]
        bool m_ShowDebugAnchors;

        [SerializeField]
        GameObject m_DebugAnchorPrefab;

        bool m_HasSpawned;
        bool m_IsSubscribed;

        static readonly List<GhostSpawnInfo> s_SpawnedGhosts = new List<GhostSpawnInfo>();
        static string s_LastSpawnDiagnostics = "Ghost spawn: no attempt yet.";

        public bool HasSpawned => m_HasSpawned;

        public static string LastSpawnDiagnostics => s_LastSpawnDiagnostics;

        public static GhostSpawnInfo[] GetSpawnedGhostInfos()
        {
            return s_SpawnedGhosts.ToArray();
        }

        public static Vector3[] GetSpawnedGhostWorldPositions()
        {
            var positions = new Vector3[s_SpawnedGhosts.Count];
            for (var i = 0; i < s_SpawnedGhosts.Count; i++)
                positions[i] = s_SpawnedGhosts[i].WorldPosition;

            return positions;
        }

        public static Transform[] GetSpawnedGhosts()
        {
            var transforms = new Transform[s_SpawnedGhosts.Count];
            for (var i = 0; i < s_SpawnedGhosts.Count; i++)
                transforms[i] = s_SpawnedGhosts[i].ghostTransform;

            return transforms;
        }

        public void Configure(GameStateManager gameStateManager, Camera arCamera, ARAnchorManager anchorManager)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_ARCamera = arCamera != null ? arCamera.transform : m_ARCamera;
            m_AnchorManager = anchorManager ?? m_AnchorManager;

            if (isActiveAndEnabled)
            {
                Unsubscribe();
                Subscribe();
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(GhostSpawnController)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_GhostCaseController == null)
                m_GhostCaseController = GhostCaseController.Instance;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main.transform;

            if (m_AnchorManager == null)
                m_AnchorManager = GetOrCreateAnchorManager();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(GameStateManager gameStateManager, GhostCaseController ghostCaseController, GameObject ghostPrefab, Transform arCamera)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_GhostCaseController = ghostCaseController ?? m_GhostCaseController;
            m_GhostPrefab = ghostPrefab ?? m_GhostPrefab;
            m_ARCamera = arCamera ?? m_ARCamera;

            if (isActiveAndEnabled)
            {
                Unsubscribe();
                Subscribe();
            }
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
            {
                m_GameStateManager.ScanCompletedWithResult += SpawnGhostsOnce;
                m_GameStateManager.ScanCompleted += SpawnGhostsOnceFromLastResult;
                m_GameStateManager.PhaseChanged += OnPhaseChanged;
            }

            m_IsSubscribed = true;
        }

        void Unsubscribe()
        {
            if (!m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
            {
                m_GameStateManager.ScanCompletedWithResult -= SpawnGhostsOnce;
                m_GameStateManager.ScanCompleted -= SpawnGhostsOnceFromLastResult;
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
            }

            m_IsSubscribed = false;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.RoomScan && phase != GamePhase.Setup)
                return;

            m_HasSpawned = false;
            ClearSpawnedGhosts();
        }

        void SpawnGhostsOnceFromLastResult()
        {
            SpawnGhostsOnce(m_GameStateManager != null ? m_GameStateManager.LastRoomScanResult : null);
        }

        public async void SpawnGhostsOnce(RoomScanResult scanResult)
        {
            if (m_HasSpawned)
                return;

            if (m_GameStateManager != null && !m_GameStateManager.HasCompletedRoomScan)
                return;

            m_HasSpawned = true;
            if (m_GhostCaseController == null)
                m_GhostCaseController = GhostCaseController.Instance;
            m_GhostCaseController?.EnsureCase();

            if (!ResolveGhostPrefab() || m_ARCamera == null)
            {
                s_LastSpawnDiagnostics = "Ghost spawn failed: missing ghost prefab or AR camera.";
                Debug.Log("Room scan completed. Ghost spawn hook fired; assign a ghost prefab to spawn a visible ghost. Place a prefab at Resources/Ghost.prefab or assign one to GhostSpawnController.");
                return;
            }

            if (m_AnchorManager == null)
                m_AnchorManager = GetOrCreateAnchorManager();

            var diagnostics = new SpawnDiagnostics();
            var spawnCandidates = BuildSpawnCandidates(scanResult, diagnostics);
            RandomizeSpawnCandidateOrder(spawnCandidates);
            var spawnCount = Mathf.Max(1, m_SpawnCount);
            var spawnedCount = 0;

            foreach (var candidate in spawnCandidates)
            {
                if (spawnedCount >= spawnCount)
                    break;

                if (IsTooCloseToExistingSpawn(candidate.pose.position))
                {
                    diagnostics.Reject(candidate, "Too close to existing ghost spawn.");
                    continue;
                }

                var parent = await CreateAnchorParentAsync(candidate.pose);
                if (!CanCompleteSpawn())
                {
                    DestroyAnchorParent(parent);
                    diagnostics.Reject(candidate, "Spawn cancelled because phase changed before anchor creation completed.");
                    s_LastSpawnDiagnostics = diagnostics.BuildSummary(s_SpawnedGhosts);
                    return;
                }

                var ghost = Instantiate(m_GhostPrefab, parent);
                ghost.transform.localPosition = Vector3.zero;
                ghost.transform.localRotation = Quaternion.identity;

                var info = new GhostSpawnInfo
                {
                    ghostTransform = ghost.transform,
                    anchorTransform = parent,
                    worldPose = candidate.pose,
                    anchor = parent != null ? parent.GetComponent<ARAnchor>() : null,
                    source = candidate.source,
                    reason = candidate.reason,
                    score = candidate.score,
                    hasARAnchor = parent != null && parent.GetComponent<ARAnchor>() != null
                };

                s_SpawnedGhosts.Add(info);
                diagnostics.Spawn(info);
                CreateDebugAnchorView(info);
                spawnedCount++;
            }

            s_LastSpawnDiagnostics = diagnostics.BuildSummary(s_SpawnedGhosts);
            Debug.Log(s_LastSpawnDiagnostics);
        }

        public void ResetSpawnedGhosts()
        {
            m_HasSpawned = false;
            ClearSpawnedGhosts();
        }

        bool ResolveGhostPrefab()
        {
            if (m_GhostPrefab != null)
                return true;

            m_GhostPrefab = Resources.Load<GameObject>("Ghost");
            return m_GhostPrefab != null;
        }

        List<SpawnCandidate> BuildSpawnCandidates(RoomScanResult scanResult, SpawnDiagnostics diagnostics)
        {
            var candidates = new List<SpawnCandidate>();
            AddScanCandidates(scanResult, candidates, diagnostics);

            if (TryGetBoundsFallback(scanResult, candidates, diagnostics, out var boundsCandidate))
                candidates.Add(boundsCandidate);

            if (TryCreateCameraFallback(scanResult, diagnostics, out var cameraFallback))
                candidates.Add(cameraFallback);

            return candidates;
        }

        void RandomizeSpawnCandidateOrder(List<SpawnCandidate> candidates)
        {
            if (!m_RandomizeSpawnCandidateOrder || candidates == null || candidates.Count <= 1)
                return;

            candidates.Sort((left, right) => right.score.CompareTo(left.score));
            var poolSize = Mathf.Clamp(m_RandomizedSpawnCandidatePoolSize, 1, candidates.Count);
            for (var i = 0; i < poolSize - 1; i++)
            {
                var swapIndex = UnityEngine.Random.Range(i, poolSize);
                if (swapIndex == i)
                    continue;

                var candidate = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = candidate;
            }
        }

        void AddScanCandidates(RoomScanResult scanResult, List<SpawnCandidate> candidates, SpawnDiagnostics diagnostics)
        {
            if (scanResult?.safeSpawnCandidates == null)
                return;

            foreach (var candidate in scanResult.safeSpawnCandidates)
            {
                var position = candidate.position;
                var spawnCandidate = new SpawnCandidate(
                    new Pose(position, GetFacingCameraRotation(position)),
                    GhostSpawnSource.ScanCandidate,
                    candidate.reason,
                    candidate.score);

                if (!IsValidSpawnPosition(position, scanResult, out var rejectionReason))
                {
                    diagnostics.Reject(spawnCandidate, rejectionReason);
                    continue;
                }

                candidates.Add(spawnCandidate);
            }

            candidates.Sort((left, right) => right.score.CompareTo(left.score));
        }

        bool TryGetBoundsFallback(RoomScanResult scanResult, List<SpawnCandidate> existingCandidates, SpawnDiagnostics diagnostics, out SpawnCandidate candidate)
        {
            candidate = default;

            if (scanResult == null || !scanResult.hasEstimatedBounds || m_ARCamera == null)
                return false;

            var bounds = scanResult.estimatedBounds;
            var cameraPosition = m_ARCamera.position;
            var position = bounds.center;
            position.y = bounds.min.y + m_SpawnHeightOffsetMeters;

            var horizontal = Vector3.ProjectOnPlane(position - cameraPosition, Vector3.up);
            if (horizontal.sqrMagnitude < m_MinimumDistanceFromPlayerMeters * m_MinimumDistanceFromPlayerMeters)
            {
                var fallbackDirection = GetCameraForwardOnPlane();
                position = cameraPosition + fallbackDirection * Mathf.Max(m_MinimumDistanceFromPlayerMeters, m_FallbackDistanceMeters);
                position.y = bounds.min.y + m_SpawnHeightOffsetMeters;
                horizontal = Vector3.ProjectOnPlane(position - cameraPosition, Vector3.up);
            }

            if (m_SpawnRadiusMeters > 0f && horizontal.magnitude > m_SpawnRadiusMeters)
            {
                position = cameraPosition + horizontal.normalized * m_SpawnRadiusMeters;
                position.y = bounds.min.y + m_SpawnHeightOffsetMeters;
            }

            candidate = new SpawnCandidate(
                new Pose(position, GetFacingCameraRotation(position)),
                GhostSpawnSource.EstimatedBoundsFallback,
                "Estimated room bounds fallback",
                0.25f);

            if (!IsValidSpawnPosition(position, scanResult, out var rejectionReason))
            {
                diagnostics.Reject(candidate, rejectionReason);
                return false;
            }

            if (IsTooCloseToCandidates(position, existingCandidates))
            {
                diagnostics.Reject(candidate, "Too close to a stronger spawn candidate.");
                return false;
            }

            return true;
        }

        bool TryCreateCameraFallback(RoomScanResult scanResult, SpawnDiagnostics diagnostics, out SpawnCandidate candidate)
        {
            candidate = default;

            if (scanResult != null && scanResult.hasEstimatedBounds)
            {
                diagnostics.Skip("Camera fallback skipped because estimated room bounds are available.");
                return false;
            }

            var forward = GetCameraForwardOnPlane();
            var angles = new[] { 0f, -35f, 35f, -70f, 70f, 180f };

            for (var i = 0; i < angles.Length; i++)
            {
                var direction = Quaternion.AngleAxis(angles[i], Vector3.up) * forward;
                var position = m_ARCamera.position + direction * Mathf.Max(m_MinimumDistanceFromPlayerMeters, m_FallbackDistanceMeters);
                position.y += m_SpawnHeightOffsetMeters;
                var testCandidate = new SpawnCandidate(
                    new Pose(position, Quaternion.LookRotation(-direction, Vector3.up)),
                    GhostSpawnSource.CameraFallback,
                    angles[i] == 0f ? "Camera forward fallback" : $"Camera fallback rotated {angles[i]:0} degrees",
                    0f);

                if (IsValidSpawnPosition(position, scanResult, out var rejectionReason, allowOutsideEstimatedBounds: scanResult == null || !scanResult.hasEstimatedBounds))
                {
                    candidate = testCandidate;
                    return true;
                }

                diagnostics.Reject(testCandidate, rejectionReason);
            }

            diagnostics.Skip("Camera fallback skipped because all fallback samples failed validation.");
            return false;
        }

        bool IsValidSpawnPosition(Vector3 position, RoomScanResult scanResult, out string rejectionReason, bool allowOutsideEstimatedBounds = false)
        {
            rejectionReason = null;

            if (!IsFinite(position))
            {
                rejectionReason = "Position contains non-finite coordinates.";
                return false;
            }

            if (m_ARCamera == null)
                return true;

            var cameraPosition = m_ARCamera.position;
            var horizontalDistance = Vector3.ProjectOnPlane(position - cameraPosition, Vector3.up).magnitude;
            if (horizontalDistance < m_MinimumDistanceFromPlayerMeters)
            {
                rejectionReason = $"Too close to player ({horizontalDistance:0.00}m < {m_MinimumDistanceFromPlayerMeters:0.00}m).";
                return false;
            }

            if (m_SpawnRadiusMeters > 0f && horizontalDistance > m_SpawnRadiusMeters)
            {
                rejectionReason = $"Outside spawn radius ({horizontalDistance:0.00}m > {m_SpawnRadiusMeters:0.00}m).";
                return false;
            }

            var heightDelta = position.y - cameraPosition.y;
            if (heightDelta > m_MaxHeightAboveCameraMeters || heightDelta < -m_MaxHeightBelowCameraMeters)
            {
                rejectionReason = $"Height delta out of range ({heightDelta:0.00}m).";
                return false;
            }

            if (!allowOutsideEstimatedBounds && scanResult != null && scanResult.hasEstimatedBounds && !IsInsideEstimatedBounds(position, scanResult.estimatedBounds))
            {
                rejectionReason = "Outside estimated room bounds.";
                return false;
            }

            return true;
        }

        bool IsInsideEstimatedBounds(Vector3 position, Bounds bounds)
        {
            const float margin = 0.35f;
            return position.x >= bounds.min.x - margin
                && position.x <= bounds.max.x + margin
                && position.z >= bounds.min.z - margin
                && position.z <= bounds.max.z + margin;
        }

        static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        bool IsTooCloseToExistingSpawn(Vector3 position)
        {
            foreach (var info in s_SpawnedGhosts)
            {
                if (Vector3.Distance(info.WorldPosition, position) < m_MinimumCandidateSeparationMeters)
                    return true;
            }

            return false;
        }

        bool IsTooCloseToCandidates(Vector3 position, List<SpawnCandidate> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (Vector3.Distance(candidate.pose.position, position) < m_MinimumCandidateSeparationMeters)
                    return true;
            }

            return false;
        }

        Quaternion GetFacingCameraRotation(Vector3 position)
        {
            var directionToCamera = m_ARCamera != null
                ? Vector3.ProjectOnPlane(m_ARCamera.position - position, Vector3.up)
                : Vector3.forward;

            if (directionToCamera.sqrMagnitude < 0.001f)
                directionToCamera = Vector3.forward;

            return Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
        }

        Vector3 GetCameraForwardOnPlane()
        {
            if (m_ARCamera == null)
                return Vector3.forward;

            var forward = Vector3.ProjectOnPlane(m_ARCamera.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(m_ARCamera.up, Vector3.up);

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            return forward.normalized;
        }

        async Awaitable<Transform> CreateAnchorParentAsync(Pose pose)
        {
            if (m_AnchorManager != null)
            {
                try
                {
                    if (!m_AnchorManager.enabled)
                        m_AnchorManager.enabled = true;

                    var result = await m_AnchorManager.TryAddAnchorAsync(pose);
                    if (result.status.IsSuccess() && result.value != null)
                        return result.value.transform;

                    Debug.LogWarning($"AR anchor creation failed for ghost spawn: {result.status}. Falling back to world-space parent.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AR anchor creation threw for ghost spawn: {ex.Message}. Falling back to world-space parent.");
                }
            }

            return CreateWorldSpaceAnchorParent(pose);
        }

        Transform CreateWorldSpaceAnchorParent(Pose pose)
        {
            var anchorObject = new GameObject("Ghost World Anchor");
            anchorObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return anchorObject.transform;
        }

        void CreateDebugAnchorView(GhostSpawnInfo info)
        {
            if (!m_ShowDebugAnchors || info == null)
                return;

            GameObject debugObject;
            if (m_DebugAnchorPrefab != null)
            {
                debugObject = Instantiate(m_DebugAnchorPrefab, info.WorldPosition, info.WorldRotation);
            }
            else
            {
                debugObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugObject.name = "Ghost Anchor Debug";
                debugObject.transform.localScale = Vector3.one * 0.08f;
                debugObject.transform.SetPositionAndRotation(info.WorldPosition, info.WorldRotation);

                var collider = debugObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;

                var renderer = debugObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.material.SetColor("_BaseColor", new Color(0.3f, 0.95f, 1f, 0.9f));
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            var anchorTransform = info.anchor != null ? info.anchor.transform : info.ghostTransform.parent;
            if (anchorTransform != null)
                debugObject.transform.SetParent(anchorTransform, true);
        }

        static ARAnchorManager GetOrCreateAnchorManager()
        {
            var anchorManager = UnityEngine.Object.FindFirstObjectByType<ARAnchorManager>();
            if (anchorManager != null)
                return anchorManager;

            var xrOrigin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null)
                return null;

            anchorManager = xrOrigin.gameObject.AddComponent<ARAnchorManager>();
            Debug.Log($"Created ARAnchorManager on {xrOrigin.gameObject.name} for ghost anchoring.");
            return anchorManager;
        }

        public static void ClearSpawnedGhosts()
        {
            foreach (var info in s_SpawnedGhosts)
            {
                if (info == null)
                    continue;

                if (info.anchor != null)
                {
                    Destroy(info.anchor.gameObject);
                    continue;
                }

                if (info.anchorTransform != null)
                {
                    Destroy(info.anchorTransform.gameObject);
                    continue;
                }

                if (info.ghostTransform != null)
                    Destroy(info.ghostTransform.gameObject);
            }

            s_SpawnedGhosts.Clear();
            s_LastSpawnDiagnostics = "Ghost spawn: cleared.";
        }

        bool CanCompleteSpawn()
        {
            if (!isActiveAndEnabled)
                return false;

            if (m_GameStateManager == null)
                return true;

            return m_GameStateManager.HasCompletedRoomScan
                && (m_GameStateManager.CurrentPhase == GamePhase.Investigation
                    || m_GameStateManager.CurrentPhase == GamePhase.RoomScan);
        }

        static void DestroyAnchorParent(Transform parent)
        {
            if (parent != null)
                Destroy(parent.gameObject);
        }

        readonly struct SpawnCandidate
        {
            public readonly Pose pose;
            public readonly GhostSpawnSource source;
            public readonly string reason;
            public readonly float score;

            public SpawnCandidate(Pose pose, GhostSpawnSource source, string reason, float score)
            {
                this.pose = pose;
                this.source = source;
                this.reason = reason;
                this.score = score;
            }
        }

        sealed class SpawnDiagnostics
        {
            readonly List<string> m_Spawned = new List<string>();
            readonly List<string> m_Rejected = new List<string>();

            public void Spawn(GhostSpawnInfo info)
            {
                if (info == null)
                    return;

                m_Spawned.Add($"{info.source} at {FormatVector(info.WorldPosition)}; anchor={(info.hasARAnchor ? "AR" : "world")}; score={info.score:0.00}; reason={info.reason}");
            }

            public void Reject(SpawnCandidate candidate, string reason)
            {
                m_Rejected.Add($"{candidate.source} at {FormatVector(candidate.pose.position)} rejected: {reason}");
            }

            public void Skip(string reason)
            {
                m_Rejected.Add(reason);
            }

            public string BuildSummary(List<GhostSpawnInfo> currentSpawns)
            {
                var summary = $"Ghost spawns: {currentSpawns.Count}";
                if (m_Spawned.Count == 0)
                    summary += "\nNo valid ghost spawn candidate found.";

                if (m_Spawned.Count > 0)
                    summary += "\nSpawned:\n- " + string.Join("\n- ", m_Spawned);

                if (m_Rejected.Count > 0)
                    summary += "\nRejected/Skipped:\n- " + string.Join("\n- ", m_Rejected);

                return summary;
            }

            static string FormatVector(Vector3 value)
            {
                return $"({value.x:0.00}, {value.y:0.00}, {value.z:0.00})";
            }
        }
    }
}
