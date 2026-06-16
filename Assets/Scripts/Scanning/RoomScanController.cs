using System;
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
            public string instruction;
        }

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        Camera m_ARCamera;

        [SerializeField]
        ARPlaneManager m_PlaneManager;

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
        float m_PoorTrackingDecayPerSecond = 0.04f;

        [SerializeField]
        UnityEvent<float> m_ProgressChanged = new UnityEvent<float>();

        [SerializeField]
        UnityEvent<TrackingConfidence> m_TrackingConfidenceChanged = new UnityEvent<TrackingConfidence>();

        [SerializeField]
        UnityEvent m_RoomScanCompleted = new UnityEvent();

        public event Action<ScanSnapshot> ScanUpdated;
        public event Action RoomScanCompleted;

        float m_ElapsedSeconds;
        float m_StableTrackingSeconds;
        float m_MovementMeters;
        float m_LookDegrees;
        float m_Progress;
        TrackingConfidence m_Confidence = TrackingConfidence.Unavailable;
        Vector3 m_LastCameraPosition;
        Quaternion m_LastCameraRotation;
        bool m_HasCameraPose;
        bool m_CompletionRaised;

        public float Progress => m_Progress;
        public TrackingConfidence Confidence => m_Confidence;
        public bool IsComplete => m_CompletionRaised;
        public UnityEvent<float> progressChanged => m_ProgressChanged;
        public UnityEvent<TrackingConfidence> trackingConfidenceChanged => m_TrackingConfidenceChanged;
        public UnityEvent roomScanCompleted => m_RoomScanCompleted;

        public void Configure(GameStateManager gameStateManager, Camera arCamera, ARPlaneManager planeManager)
        {
            m_GameStateManager = gameStateManager;
            m_ARCamera = arCamera;
            m_PlaneManager = planeManager;
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
            if (m_GameStateManager == null || m_GameStateManager.CurrentPhase != GamePhase.RoomScan || m_CompletionRaised)
                return;

            var deltaTime = Time.deltaTime;
            m_ElapsedSeconds += deltaTime;

            UpdateTracking(deltaTime);
            UpdateCameraCoverage();
            UpdateProgress(deltaTime);

            var snapshot = CreateSnapshot();
            ScanUpdated?.Invoke(snapshot);

            if (IsCompletionValid(snapshot))
                CompleteScan();
        }

        public void RestartScan()
        {
            m_ElapsedSeconds = 0f;
            m_StableTrackingSeconds = 0f;
            m_MovementMeters = 0f;
            m_LookDegrees = 0f;
            m_Progress = 0f;
            m_HasCameraPose = false;
            m_CompletionRaised = false;
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

            if (!m_HasCameraPose)
            {
                m_LastCameraPosition = cameraTransform.position;
                m_LastCameraRotation = cameraTransform.rotation;
                m_HasCameraPose = true;
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

        void UpdateProgress(float deltaTime)
        {
            var timeProgress = SafeRatio(m_ElapsedSeconds, m_MinimumScanSeconds);
            var stableProgress = SafeRatio(m_StableTrackingSeconds, m_RequiredStableTrackingSeconds);
            var movementProgress = SafeRatio(m_MovementMeters, m_RequiredMovementMeters);
            var lookProgress = SafeRatio(m_LookDegrees, m_RequiredLookDegrees);
            var planeProgress = m_RequiredTrackedPlanes <= 0 ? 1f : SafeRatio(GetTrackedPlaneCount(), m_RequiredTrackedPlanes);

            var targetProgress =
                timeProgress * 0.2f +
                stableProgress * 0.25f +
                movementProgress * 0.25f +
                lookProgress * 0.2f +
                planeProgress * 0.1f;

            if (m_Confidence == TrackingConfidence.Good)
                m_Progress = Mathf.Max(m_Progress, targetProgress);
            else if (m_Confidence == TrackingConfidence.Limited)
                m_Progress = Mathf.Max(m_Progress, Mathf.Min(targetProgress, m_Progress + deltaTime * 0.05f));
            else
                m_Progress = Mathf.Max(0f, m_Progress - m_PoorTrackingDecayPerSecond * deltaTime);

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

        void CompleteScan()
        {
            if (m_CompletionRaised)
                return;

            m_CompletionRaised = true;
            m_Progress = 1f;
            m_ProgressChanged.Invoke(m_Progress);
            m_RoomScanCompleted.Invoke();
            RoomScanCompleted?.Invoke();

            if (m_GameStateManager != null)
                m_GameStateManager.CompleteRoomScan();
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
                instruction = GetInstruction()
            };
        }

        string GetInstruction()
        {
            if (m_Confidence == TrackingConfidence.Unavailable || m_Confidence == TrackingConfidence.Poor)
                return "Move slowly and aim at textured surfaces.";

            if (GetTrackedPlaneCount() < m_RequiredTrackedPlanes)
                return "Find a floor, table, or wall surface.";

            if (m_MovementMeters < m_RequiredMovementMeters)
                return "Move through the room slowly.";

            if (m_LookDegrees < m_RequiredLookDegrees)
                return "Look around the room to cover more space.";

            return "Hold steady while the scan finishes.";
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
