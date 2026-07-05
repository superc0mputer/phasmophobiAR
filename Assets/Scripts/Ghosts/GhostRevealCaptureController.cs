using System;
using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    public sealed class GhostRevealCaptureController : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        RoomScanController m_RoomScanController;

        [SerializeField]
        Transform m_ARCamera;

        [SerializeField]
        GhostBehaviorController m_GhostBehavior;

        [SerializeField]
        GhostRevealCaptureSettings m_Settings = new GhostRevealCaptureSettings();

        [SerializeField]
        float m_ResultDelaySeconds = 0.4f;

        GhostRevealCaptureStateMachine m_StateMachine;
        bool m_IsSubscribed;
        bool m_HasTriggeredResult;
        bool m_IsResultDelayActive;
        bool m_HasCaptureActivity;
        bool m_HasReportedInterruption;
        float m_ResultDelayTimer;
        float m_CaptureElapsedSeconds;
        float m_FakeCaptureFailureRemaining;

        public GhostRevealState CurrentState => m_StateMachine != null ? m_StateMachine.CurrentState : GhostRevealState.Hidden;
        public float CaptureProgress => m_StateMachine != null ? m_StateMachine.CaptureProgress : 0f;
        public bool IsFakeCaptureFailureActive => m_FakeCaptureFailureRemaining > 0f;

        public event Action CaptureSucceeded;
        public event Action<string> CaptureInterrupted;

        void Awake()
        {
            ResolveReferences();
            BuildStateMachine();
            ApplyVisualState();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GameStateManager gameStateManager,
            RoomScanController roomScanController,
            Transform arCamera,
            GhostBehaviorController ghostBehavior)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_RoomScanController = roomScanController ?? m_RoomScanController;
            m_ARCamera = arCamera ?? m_ARCamera;
            m_GhostBehavior = ghostBehavior ?? m_GhostBehavior;

            BuildStateMachine();
            ApplyVisualState();
        }

        void Update()
        {
            if (m_StateMachine == null || m_GhostBehavior == null || m_ARCamera == null)
                return;

            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
                return;

            if (m_FakeCaptureFailureRemaining > 0f)
            {
                m_FakeCaptureFailureRemaining = Mathf.Max(0f, m_FakeCaptureFailureRemaining - Time.unscaledDeltaTime);
                m_GhostBehavior.SetRevealState(GhostRevealState.Hidden, CaptureProgress);
                if (m_FakeCaptureFailureRemaining <= 0f)
                    ApplyVisualState();
                return;
            }

            var confidence = m_RoomScanController != null ? m_RoomScanController.Confidence : TrackingConfidence.Good;
            var ghostPosition = m_GhostBehavior.transform.position;
            var distance = Vector3.Distance(m_ARCamera.position, ghostPosition);
            var toGhost = ghostPosition - m_ARCamera.position;
            var angle = toGhost.sqrMagnitude > 0.0001f ? Vector3.Angle(m_ARCamera.forward, toGhost.normalized) : 0f;

            var previousState = m_StateMachine.CurrentState;
            var previousProgress = m_StateMachine.CaptureProgress;
            var nextState = m_StateMachine.Tick(distance, angle, confidence, Time.deltaTime);
            ApplyStateChange(previousState, nextState);
            ApplyVisualState();

            if (nextState == GhostRevealState.Capturing || nextState == GhostRevealState.Captured)
            {
                m_HasCaptureActivity = true;
                m_CaptureElapsedSeconds += Time.deltaTime;
                m_HasReportedInterruption = false;
            }

            if (m_HasCaptureActivity && !m_HasTriggeredResult && !m_HasReportedInterruption && previousProgress > 0f && CaptureProgress <= 0f)
                HandleCaptureInterrupted();

            if (nextState == GhostRevealState.Captured)
                HandleCaptureCompleted(Time.deltaTime);
            else
                m_IsResultDelayActive = false;
        }

        void BuildStateMachine()
        {
            var settings = m_Settings != null ? m_Settings.Copy() : new GhostRevealCaptureSettings();

            if (m_GhostBehavior != null)
            {
                var revealMultiplier = Mathf.Lerp(1.15f, 0.8f, Mathf.Clamp01(m_GhostBehavior.RevealDifficulty));
                var captureMultiplier = Mathf.Lerp(0.9f, 1.35f, Mathf.Clamp01(m_GhostBehavior.CaptureDifficulty));
                settings.partialRevealDistanceMeters = Mathf.Max(0.1f, settings.partialRevealDistanceMeters * revealMultiplier);
                settings.revealDistanceMeters = Mathf.Max(0.1f, settings.revealDistanceMeters * revealMultiplier);
                settings.captureSeconds = Mathf.Max(0.1f, settings.captureSeconds * captureMultiplier);
            }

            m_StateMachine = new GhostRevealCaptureStateMachine(settings);
            m_StateMachine.Reset();
            m_HasTriggeredResult = false;
            m_IsResultDelayActive = false;
            m_ResultDelayTimer = 0f;
            m_HasCaptureActivity = false;
            m_HasReportedInterruption = false;
            m_CaptureElapsedSeconds = 0f;
            m_FakeCaptureFailureRemaining = 0f;
        }

        public bool BeginFakeCaptureFailure(float durationSeconds)
        {
            if (m_StateMachine == null || m_StateMachine.CurrentState != GhostRevealState.Capturing || m_FakeCaptureFailureRemaining > 0f)
                return false;

            m_FakeCaptureFailureRemaining = Mathf.Max(0.1f, durationSeconds);
            m_GhostBehavior?.SetRevealState(GhostRevealState.Hidden, CaptureProgress);
            return true;
        }

        void ApplyStateChange(GhostRevealState previousState, GhostRevealState nextState)
        {
            if (m_GhostBehavior == null)
                return;

            if (previousState != nextState || previousState == GhostRevealState.Hidden)
                ApplyVisualState();
        }

        void ApplyVisualState()
        {
            if (m_GhostBehavior == null)
                return;

            m_GhostBehavior.SetRevealState(m_StateMachine != null ? m_StateMachine.CurrentState : GhostRevealState.Hidden, CaptureProgress);
        }

        void HandleCaptureCompleted(float deltaTime)
        {
            if (m_HasTriggeredResult)
                return;

            if (!m_IsResultDelayActive)
            {
                m_IsResultDelayActive = true;
                m_ResultDelayTimer = 0f;
            }

            m_ResultDelayTimer += deltaTime;

            if (m_ResultDelayTimer < m_ResultDelaySeconds)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.RecordCaptureOutcome("Success", CaptureProgress, m_CaptureElapsedSeconds, string.Empty);

            m_HasTriggeredResult = true;
            CaptureSucceeded?.Invoke();
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase == GamePhase.Investigation)
                m_GameStateManager.ShowResult();
        }

        void HandleCaptureInterrupted()
        {
            m_HasReportedInterruption = true;
            m_HasCaptureActivity = false;
            m_CaptureElapsedSeconds = 0f;

            if (m_GameStateManager != null)
                m_GameStateManager.RecordCaptureOutcome("Interrupted", CaptureProgress, 0f, "Capture interrupted");

            CaptureInterrupted?.Invoke("Capture interrupted");
        }

        void ResolveReferences()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_RoomScanController == null)
                m_RoomScanController = FindAnyObjectByType<RoomScanController>();

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main.transform;

            if (m_GhostBehavior == null)
                m_GhostBehavior = GetComponent<GhostBehaviorController>();
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;

            m_IsSubscribed = true;
        }

        void Unsubscribe()
        {
            if (!m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;

            m_IsSubscribed = false;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Setup || phase == GamePhase.RoomScan)
                BuildStateMachine();

            if (phase != GamePhase.Investigation)
                m_FakeCaptureFailureRemaining = 0f;

            if (phase == GamePhase.Result)
            {
                m_HasCaptureActivity = false;
                m_HasReportedInterruption = false;
            }
        }
    }
}
