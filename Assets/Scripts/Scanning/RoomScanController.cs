using System;
using System.Collections.Generic;
using PhasmophobiAR.Game;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PhasmophobiAR.Scanning
{
    public sealed class RoomScanController : MonoBehaviour
    {
        [Serializable]
        public struct ScanSnapshot
        {
            public float progress;
            public TrackingConfidence confidence;
            public float elapsedSeconds;
            public float stableTrackingSeconds;
            public float movementMeters;
            public float lookDegrees;
            public int trackedPlaneCount;
            public int floorPlaneCount;
            public int wallPlaneCount;
            public int tablePlaneCount;
            public int featurePointCount;
            public float featurePointDensity;
            public bool hasEstimatedBounds;
            public Bounds estimatedBounds;
            public bool hasLiDARMeshData;
            public int meshCount;
            public int meshVertexCount;
            public bool hasDepthOcclusion;
            public int safeSpawnCount;
            public bool isReady;
            public string instruction;
        }

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        Camera m_ARCamera;

        [SerializeField]
        ARPlaneManager m_PlaneManager;

        [SerializeField]
        ARPointCloudManager m_PointCloudManager;

        [SerializeField]
        ARMeshManager m_MeshManager;

        [SerializeField]
        AROcclusionManager m_OcclusionManager;

        [Header("Completion")]
        [SerializeField]
        float m_MinimumScanSeconds = 8f;

        [SerializeField]
        float m_RequiredStableTrackingSeconds = 4f;

        [SerializeField]
        float m_RequiredMovementMeters = 1.25f;

        [SerializeField]
        float m_RequiredLookDegrees = 120f;

        [SerializeField]
        int m_RequiredTrackedPlanes = 1;

        [Header("Tuning")]
        [SerializeField]
        float m_MovementNoiseThresholdMeters = 0.03f;

        [SerializeField]
        float m_RotationNoiseThresholdDegrees = 2f;

        [SerializeField]
        float m_CoverageWarmupSeconds = 0.75f;

        [SerializeField]
        float m_PoorTrackingDecayPerSecond = 0.04f;

        [SerializeField]
        float m_FeatureDensityRadiusMeters = 2.5f;

        [SerializeField]
        float m_SpawnSurfaceHeightOffsetMeters = 0.25f;

        [SerializeField]
        float m_MinimumSpawnDistanceFromCameraMeters = 1.25f;

        [SerializeField]
        float m_SpawnCandidateMergeDistanceMeters = 0.35f;

        [SerializeField]
        int m_MaxStableSpawnCandidates = 8;

        [SerializeField]
        UnityEvent<float> m_ProgressChanged = new UnityEvent<float>();

        [SerializeField]
        UnityEvent<TrackingConfidence> m_TrackingConfidenceChanged = new UnityEvent<TrackingConfidence>();

        [SerializeField]
        UnityEvent m_RoomScanCompleted = new UnityEvent();

        [SerializeField]
        UnityEvent m_RoomScanReady = new UnityEvent();

        public event Action<ScanSnapshot> ScanUpdated;
        public event Action RoomScanCompleted;
        public event Action RoomScanReady;

        float m_ElapsedSeconds;
        float m_StableTrackingSeconds;
        float m_GoodTrackingSeconds;
        float m_MovementMeters;
        float m_LookDegrees;
        float m_Progress;
        TrackingConfidence m_Confidence = TrackingConfidence.Unavailable;
        Vector3 m_LastCameraPosition;
        Quaternion m_LastCameraRotation;
        readonly List<SafeGhostSpawnCandidate> m_StableSpawnCandidates = new List<SafeGhostSpawnCandidate>();
        bool m_HasCameraPose;
        bool m_IsScanReady;
        bool m_InvestigationStarted;

        public float Progress => m_Progress;
        public TrackingConfidence Confidence => m_Confidence;
        public bool IsReady => m_IsScanReady;
        public bool IsComplete => m_InvestigationStarted;
        public UnityEvent<float> progressChanged => m_ProgressChanged;
        public UnityEvent<TrackingConfidence> trackingConfidenceChanged => m_TrackingConfidenceChanged;
        public UnityEvent roomScanCompleted => m_RoomScanCompleted;
        public UnityEvent roomScanReady => m_RoomScanReady;

        public void Configure(
            GameStateManager gameStateManager,
            Camera arCamera,
            ARPlaneManager planeManager,
            ARPointCloudManager pointCloudManager,
            ARMeshManager meshManager,
            AROcclusionManager occlusionManager)
        {
            m_GameStateManager = gameStateManager;
            m_ARCamera = arCamera;
            m_PlaneManager = planeManager;
            m_PointCloudManager = pointCloudManager;
            m_MeshManager = meshManager;
            m_OcclusionManager = occlusionManager;
        }

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_ARCamera == null)
                m_ARCamera = Camera.main;
        }

        void OnEnable()
        {
            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
        }

        void Update()
        {
            if (m_GameStateManager == null || m_GameStateManager.CurrentPhase != GamePhase.RoomScan || m_InvestigationStarted)
                return;

            var deltaTime = Time.deltaTime;
            m_ElapsedSeconds += deltaTime;

            UpdateTracking(deltaTime);
            UpdateCameraCoverage();
            UpdateProgress(deltaTime);

            var snapshot = CreateSnapshot();
            ScanUpdated?.Invoke(snapshot);

            if (!m_IsScanReady && IsCompletionValid(snapshot))
                MarkScanReady();
        }

        public void RestartScan()
        {
            m_ElapsedSeconds = 0f;
            m_StableTrackingSeconds = 0f;
            m_GoodTrackingSeconds = 0f;
            m_MovementMeters = 0f;
            m_LookDegrees = 0f;
            m_Progress = 0f;
            m_HasCameraPose = false;
            m_IsScanReady = false;
            m_InvestigationStarted = false;
            m_StableSpawnCandidates.Clear();
            SetConfidence(EvaluateTrackingConfidence());
            m_ProgressChanged.Invoke(m_Progress);
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.RoomScan)
                RestartScan();
        }

        void UpdateTracking(float deltaTime)
        {
            SetConfidence(EvaluateTrackingConfidence());

            if (m_Confidence == TrackingConfidence.Good || m_Confidence == TrackingConfidence.Limited)
                m_StableTrackingSeconds += deltaTime;
            else
                m_StableTrackingSeconds = Mathf.Max(0f, m_StableTrackingSeconds - deltaTime);

            if (m_Confidence == TrackingConfidence.Good)
                m_GoodTrackingSeconds += deltaTime;
            else
                m_GoodTrackingSeconds = 0f;
        }

        TrackingConfidence EvaluateTrackingConfidence()
        {
            switch (ARSession.state)
            {
                case ARSessionState.SessionTracking:
                    return TrackingConfidence.Good;
                case ARSessionState.Ready:
                case ARSessionState.SessionInitializing:
                    return TrackingConfidence.Limited;
                case ARSessionState.CheckingAvailability:
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                    return TrackingConfidence.Unavailable;
                default:
                    return TrackingConfidence.Poor;
            }
        }

        void UpdateCameraCoverage()
        {
            if (m_ARCamera == null)
                return;

            var cameraTransform = m_ARCamera.transform;

            if (m_GoodTrackingSeconds < m_CoverageWarmupSeconds)
            {
                CaptureCameraCoverageBaseline(cameraTransform);
                return;
            }

            if (!m_HasCameraPose)
            {
                CaptureCameraCoverageBaseline(cameraTransform);
                return;
            }

            var distance = Vector3.Distance(m_LastCameraPosition, cameraTransform.position);
            if (distance >= m_MovementNoiseThresholdMeters)
            {
                m_MovementMeters += distance;
                m_LastCameraPosition = cameraTransform.position;
            }

            var angle = Quaternion.Angle(m_LastCameraRotation, cameraTransform.rotation);
            if (angle >= m_RotationNoiseThresholdDegrees)
            {
                m_LookDegrees += angle;
                m_LastCameraRotation = cameraTransform.rotation;
            }
        }

        void CaptureCameraCoverageBaseline(Transform cameraTransform)
        {
            m_LastCameraPosition = cameraTransform.position;
            m_LastCameraRotation = cameraTransform.rotation;
            m_HasCameraPose = true;
        }

        void UpdateProgress(float deltaTime)
        {
            var timeProgress = SafeRatio(m_ElapsedSeconds, m_MinimumScanSeconds);
            var stableProgress = SafeRatio(m_StableTrackingSeconds, m_RequiredStableTrackingSeconds);
            var movementProgress = SafeRatio(m_MovementMeters, m_RequiredMovementMeters);
            var lookProgress = SafeRatio(m_LookDegrees, m_RequiredLookDegrees);
            var planeProgress = m_RequiredTrackedPlanes <= 0 ? 1f : SafeRatio(GetTrackedPlaneCount(), m_RequiredTrackedPlanes);
            var playerCoverageProgress = Mathf.Max(movementProgress, lookProgress);
            var roomSignalProgress = Mathf.Clamp01(
                SafeRatio(GetFeaturePointCount(), 60f) * 0.45f +
                SafeRatio(GetClassifiedPlaneCount(), 1f) * 0.3f +
                (TryGetEstimatedBounds(out _) ? 0.25f : 0f));

            if (playerCoverageProgress <= 0f)
            {
                m_ProgressChanged.Invoke(m_Progress);
                return;
            }

            var baseProgress =
                movementProgress * 0.4f +
                lookProgress * 0.3f +
                planeProgress * 0.15f +
                timeProgress * stableProgress * 0.15f;

            var targetProgress = Mathf.Clamp01(baseProgress + roomSignalProgress * 0.1f);

            if (m_Confidence == TrackingConfidence.Good)
                m_Progress = Mathf.Max(m_Progress, targetProgress);
            else if (m_Confidence == TrackingConfidence.Limited)
                m_Progress = Mathf.Max(m_Progress, Mathf.Min(targetProgress, m_Progress + deltaTime * 0.05f));
            else
                m_Progress = Mathf.Max(0f, m_Progress - m_PoorTrackingDecayPerSecond * deltaTime);

            if (m_IsScanReady)
                m_Progress = 1f;

            m_Progress = Mathf.Clamp01(m_Progress);
            m_ProgressChanged.Invoke(m_Progress);
        }

        bool IsCompletionValid(ScanSnapshot snapshot)
        {
            return snapshot.progress >= 1f
                && snapshot.elapsedSeconds >= m_MinimumScanSeconds
                && snapshot.stableTrackingSeconds >= m_RequiredStableTrackingSeconds
                && snapshot.movementMeters >= m_RequiredMovementMeters
                && snapshot.lookDegrees >= m_RequiredLookDegrees
                && snapshot.trackedPlaneCount >= m_RequiredTrackedPlanes
                && (snapshot.confidence == TrackingConfidence.Good || snapshot.confidence == TrackingConfidence.Limited);
        }

        void MarkScanReady()
        {
            m_IsScanReady = true;
            m_Progress = 1f;
            m_ProgressChanged.Invoke(m_Progress);
            m_RoomScanReady.Invoke();
            RoomScanReady?.Invoke();
        }

        public void ConfirmScan()
        {
            if (m_InvestigationStarted)
                return;

            var snapshot = CreateSnapshot();
            if (!m_IsScanReady && !IsCompletionValid(snapshot))
            {
                Debug.Log("Room scan is not ready yet. Keep scanning before starting investigation.");
                return;
            }

            if (snapshot.confidence != TrackingConfidence.Good && snapshot.confidence != TrackingConfidence.Limited)
            {
                Debug.Log("Tracking is not stable enough to start investigation.");
                return;
            }

            m_IsScanReady = true;
            m_InvestigationStarted = true;
            m_Progress = 1f;
            m_ProgressChanged.Invoke(m_Progress);
            m_RoomScanCompleted.Invoke();
            RoomScanCompleted?.Invoke();

            if (m_GameStateManager != null)
                m_GameStateManager.CompleteRoomScan(CreateResult());
        }

        ScanSnapshot CreateSnapshot()
        {
            return new ScanSnapshot
            {
                progress = m_Progress,
                confidence = m_Confidence,
                elapsedSeconds = m_ElapsedSeconds,
                stableTrackingSeconds = m_StableTrackingSeconds,
                movementMeters = m_MovementMeters,
                lookDegrees = m_LookDegrees,
                trackedPlaneCount = GetTrackedPlaneCount(),
                floorPlaneCount = GetPlaneClassificationCount(PlaneClassifications.Floor),
                wallPlaneCount = GetPlaneClassificationCount(PlaneClassifications.WallFace | PlaneClassifications.InnerWallFace | PlaneClassifications.InvisibleWallFace),
                tablePlaneCount = GetPlaneClassificationCount(PlaneClassifications.Table),
                featurePointCount = GetFeaturePointCount(),
                featurePointDensity = GetFeaturePointDensity(),
                hasEstimatedBounds = TryGetEstimatedBounds(out var estimatedBounds),
                estimatedBounds = estimatedBounds,
                hasLiDARMeshData = HasLiDARMeshData(),
                meshCount = GetMeshCount(),
                meshVertexCount = GetMeshVertexCount(),
                hasDepthOcclusion = HasDepthOcclusion(),
                safeSpawnCount = GetSafeSpawnCandidates().Length,
                isReady = m_IsScanReady,
                instruction = GetInstruction()
            };
        }

        string GetInstruction()
        {
            if (m_Confidence == TrackingConfidence.Unavailable || m_Confidence == TrackingConfidence.Poor)
                return "Move slowly and aim at textured surfaces.";

            if (m_IsScanReady)
                return "Ready. Keep scanning for better spawn points or start investigation.";

            if (GetTrackedPlaneCount() < m_RequiredTrackedPlanes)
                return "Find a floor, table, or wall surface.";

            if (GetFeaturePointCount() < 30 && !HasLiDARMeshData())
                return "Aim at detailed objects to gather feature points.";

            if (!TryGetEstimatedBounds(out _))
                return "Sweep across the room to estimate its bounds.";

            if (m_MovementMeters < m_RequiredMovementMeters)
                return "Move through the room slowly.";

            if (m_LookDegrees < m_RequiredLookDegrees)
                return "Look around the room to cover more space.";

            return "Hold steady while the scan finishes.";
        }

        RoomScanResult CreateResult()
        {
            var snapshot = CreateSnapshot();
            return new RoomScanResult
            {
                progress = snapshot.progress,
                confidence = snapshot.confidence,
                elapsedSeconds = snapshot.elapsedSeconds,
                stableTrackingSeconds = snapshot.stableTrackingSeconds,
                movementMeters = snapshot.movementMeters,
                lookDegrees = snapshot.lookDegrees,
                trackedPlaneCount = snapshot.trackedPlaneCount,
                floorPlaneCount = snapshot.floorPlaneCount,
                wallPlaneCount = snapshot.wallPlaneCount,
                tablePlaneCount = snapshot.tablePlaneCount,
                featurePointCount = snapshot.featurePointCount,
                featurePointDensity = snapshot.featurePointDensity,
                hasEstimatedBounds = snapshot.hasEstimatedBounds,
                estimatedBounds = snapshot.estimatedBounds,
                hasLiDARMeshData = snapshot.hasLiDARMeshData,
                meshCount = snapshot.meshCount,
                meshVertexCount = snapshot.meshVertexCount,
                hasDepthOcclusion = snapshot.hasDepthOcclusion,
                safeSpawnCandidates = GetSafeSpawnCandidates()
            };
        }

        int GetTrackedPlaneCount()
        {
            if (m_PlaneManager == null)
                return 0;

            var count = 0;
            foreach (var plane in m_PlaneManager.trackables)
            {
                if (plane.trackingState == TrackingState.Tracking)
                    count++;
            }

            return count;
        }

        int GetClassifiedPlaneCount()
        {
            if (m_PlaneManager == null)
                return 0;

            var count = 0;
            foreach (var plane in m_PlaneManager.trackables)
            {
                if (plane.trackingState == TrackingState.Tracking && plane.classifications != PlaneClassifications.None)
                    count++;
            }

            return count;
        }

        int GetPlaneClassificationCount(PlaneClassifications classifications)
        {
            if (m_PlaneManager == null)
                return 0;

            var count = 0;
            foreach (var plane in m_PlaneManager.trackables)
            {
                if (plane.trackingState == TrackingState.Tracking && (plane.classifications & classifications) != 0)
                    count++;
            }

            return count;
        }

        int GetFeaturePointCount()
        {
            if (m_PointCloudManager == null)
                return 0;

            var count = 0;
            foreach (var pointCloud in m_PointCloudManager.trackables)
            {
                var positions = pointCloud.positions;
                if (positions.HasValue)
                    count += positions.Value.Length;
            }

            return count;
        }

        float GetFeaturePointDensity()
        {
            if (m_PointCloudManager == null || m_ARCamera == null || m_FeatureDensityRadiusMeters <= 0f)
                return 0f;

            var count = 0;
            var radiusSqr = m_FeatureDensityRadiusMeters * m_FeatureDensityRadiusMeters;
            var cameraPosition = m_ARCamera.transform.position;

            foreach (var pointCloud in m_PointCloudManager.trackables)
            {
                var positions = pointCloud.positions;
                if (!positions.HasValue)
                    continue;

                var cloudPositions = positions.Value;
                for (var i = 0; i < cloudPositions.Length; i++)
                {
                    var worldPosition = pointCloud.transform.TransformPoint(cloudPositions[i]);
                    if ((worldPosition - cameraPosition).sqrMagnitude <= radiusSqr)
                        count++;
                }
            }

            var volume = 4f / 3f * Mathf.PI * Mathf.Pow(m_FeatureDensityRadiusMeters, 3f);
            return count / volume;
        }

        bool TryGetEstimatedBounds(out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            if (m_PlaneManager != null)
            {
                foreach (var plane in m_PlaneManager.trackables)
                {
                    if (plane.trackingState != TrackingState.Tracking)
                        continue;

                    Encapsulate(ref bounds, ref hasBounds, plane.center);
                    var extents = plane.extents;
                    Encapsulate(ref bounds, ref hasBounds, plane.transform.TransformPoint(new Vector3(extents.x, 0f, extents.y)));
                    Encapsulate(ref bounds, ref hasBounds, plane.transform.TransformPoint(new Vector3(-extents.x, 0f, extents.y)));
                    Encapsulate(ref bounds, ref hasBounds, plane.transform.TransformPoint(new Vector3(extents.x, 0f, -extents.y)));
                    Encapsulate(ref bounds, ref hasBounds, plane.transform.TransformPoint(new Vector3(-extents.x, 0f, -extents.y)));
                }
            }

            if (m_MeshManager != null)
            {
                foreach (var meshFilter in m_MeshManager.meshes)
                {
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;

                    var meshBounds = meshFilter.sharedMesh.bounds;
                    Encapsulate(ref bounds, ref hasBounds, meshFilter.transform.TransformPoint(meshBounds.min));
                    Encapsulate(ref bounds, ref hasBounds, meshFilter.transform.TransformPoint(meshBounds.max));
                }
            }

            return hasBounds;
        }

        SafeGhostSpawnCandidate[] GetSafeSpawnCandidates()
        {
            MergeSpawnCandidates(BuildCurrentSafeSpawnCandidates());
            return m_StableSpawnCandidates.ToArray();
        }

        SafeGhostSpawnCandidate[] BuildCurrentSafeSpawnCandidates()
        {
            var candidates = new List<SafeGhostSpawnCandidate>();
            var cameraPosition = m_ARCamera != null ? m_ARCamera.transform.position : Vector3.zero;

            if (m_PlaneManager != null)
            {
                foreach (var plane in m_PlaneManager.trackables)
                {
                    if (plane.trackingState != TrackingState.Tracking)
                        continue;

                    var classifications = plane.classifications;
                    var isUsableSurface = (classifications & (PlaneClassifications.Floor | PlaneClassifications.Table | PlaneClassifications.SeatOfAnyType)) != 0
                        || (classifications == PlaneClassifications.None && Vector3.Dot(plane.normal, Vector3.up) > 0.75f);

                    if (!isUsableSurface)
                        continue;

                    var position = plane.center + Vector3.up * m_SpawnSurfaceHeightOffsetMeters;
                    var distanceFromCamera = Vector3.Distance(position, cameraPosition);
                    if (distanceFromCamera < m_MinimumSpawnDistanceFromCameraMeters)
                        continue;

                    var classificationScore = classifications == PlaneClassifications.None ? 0.45f : 0.75f;
                    if ((classifications & PlaneClassifications.Floor) != 0)
                        classificationScore = 1f;
                    else if ((classifications & PlaneClassifications.Table) != 0)
                        classificationScore = 0.8f;

                    var sizeScore = Mathf.Clamp01(plane.size.magnitude / 2f);
                    var distanceScore = Mathf.Clamp01(distanceFromCamera / 3f);
                    var score = classificationScore * 0.55f + sizeScore * 0.25f + distanceScore * 0.2f;
                    candidates.Add(new SafeGhostSpawnCandidate(position, score, classifications.ToString()));
                }
            }

            candidates.Sort((left, right) => right.score.CompareTo(left.score));
            return candidates.ToArray();
        }

        void MergeSpawnCandidates(SafeGhostSpawnCandidate[] currentCandidates)
        {
            foreach (var candidate in currentCandidates)
            {
                var matchingIndex = -1;
                for (var i = 0; i < m_StableSpawnCandidates.Count; i++)
                {
                    if (Vector3.Distance(m_StableSpawnCandidates[i].position, candidate.position) <= m_SpawnCandidateMergeDistanceMeters)
                    {
                        matchingIndex = i;
                        break;
                    }
                }

                if (matchingIndex >= 0)
                {
                    if (candidate.score > m_StableSpawnCandidates[matchingIndex].score)
                        m_StableSpawnCandidates[matchingIndex] = candidate;
                }
                else
                {
                    m_StableSpawnCandidates.Add(candidate);
                }
            }

            m_StableSpawnCandidates.Sort((left, right) => right.score.CompareTo(left.score));

            while (m_StableSpawnCandidates.Count > m_MaxStableSpawnCandidates)
                m_StableSpawnCandidates.RemoveAt(m_StableSpawnCandidates.Count - 1);
        }

        bool HasLiDARMeshData()
        {
            return m_MeshManager != null && GetMeshVertexCount() > 0;
        }

        int GetMeshCount()
        {
            return m_MeshManager?.meshes?.Count ?? 0;
        }

        int GetMeshVertexCount()
        {
            if (m_MeshManager == null)
                return 0;

            var count = 0;
            foreach (var meshFilter in m_MeshManager.meshes)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    count += meshFilter.sharedMesh.vertexCount;
            }

            return count;
        }

        bool HasDepthOcclusion()
        {
            return m_OcclusionManager != null
                && m_OcclusionManager.enabled
                && m_OcclusionManager.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
        }

        static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Vector3 point)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(point);
        }

        void SetConfidence(TrackingConfidence confidence)
        {
            if (m_Confidence == confidence)
                return;

            m_Confidence = confidence;
            m_TrackingConfidenceChanged.Invoke(m_Confidence);
        }

        static float SafeRatio(float value, float required)
        {
            if (required <= 0f)
                return 1f;

            return Mathf.Clamp01(value / required);
        }
    }
}
