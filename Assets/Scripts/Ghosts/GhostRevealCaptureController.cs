using System.Collections;
using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        Slider m_CaptureProgressSlider;

        [SerializeField]
        TMP_Text m_CaptureProgressText;

        [SerializeField]
        GhostRevealCaptureSettings m_Settings = new GhostRevealCaptureSettings();

        [SerializeField]
        float m_ResultDelaySeconds = 0.4f;

        GhostRevealCaptureStateMachine m_StateMachine;
        bool m_IsSubscribed;
        bool m_HasTriggeredResult;
        bool m_IsResultDelayActive;
        float m_ResultDelayTimer;

        public GhostRevealState CurrentState => m_StateMachine != null ? m_StateMachine.CurrentState : GhostRevealState.Hidden;
        public float CaptureProgress => m_StateMachine != null ? m_StateMachine.CaptureProgress : 0f;

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
            GhostBehaviorController ghostBehavior,
            Slider captureProgressSlider = null,
            TMP_Text captureProgressText = null)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_RoomScanController = roomScanController ?? m_RoomScanController;
            m_ARCamera = arCamera ?? m_ARCamera;
            m_GhostBehavior = ghostBehavior ?? m_GhostBehavior;
            m_CaptureProgressSlider = captureProgressSlider ?? m_CaptureProgressSlider;
            m_CaptureProgressText = captureProgressText ?? m_CaptureProgressText;

            BuildStateMachine();
            ApplyVisualState();
        }

        void Update()
        {
            if (m_StateMachine == null || m_GhostBehavior == null || m_ARCamera == null)
                return;

            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
            {
                UpdateProgressUI();
                return;
            }

            var confidence = m_RoomScanController != null ? m_RoomScanController.Confidence : TrackingConfidence.Good;
            var ghostPosition = m_GhostBehavior.transform.position;
            var distance = Vector3.Distance(m_ARCamera.position, ghostPosition);
            var toGhost = ghostPosition - m_ARCamera.position;
            var angle = toGhost.sqrMagnitude > 0.0001f ? Vector3.Angle(m_ARCamera.forward, toGhost.normalized) : 0f;

            var previousState = m_StateMachine.CurrentState;
            var nextState = m_StateMachine.Tick(distance, angle, confidence, Time.deltaTime);
            ApplyStateChange(previousState, nextState);
            ApplyVisualState();
            UpdateProgressUI();

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

        void UpdateProgressUI()
        {
            var progress = CaptureProgress;

            if (m_CaptureProgressSlider != null)
                m_CaptureProgressSlider.value = progress;

            if (m_CaptureProgressText != null)
                m_CaptureProgressText.text = CurrentState == GhostRevealState.Capturing || CurrentState == GhostRevealState.Captured
                    ? $"Capture: {Mathf.RoundToInt(progress * 100f)}%"
                    : string.Empty;
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

            m_HasTriggeredResult = true;
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase == GamePhase.Investigation)
                m_GameStateManager.ShowResult();
        }

        void ResolveReferences()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_RoomScanController == null)
                m_RoomScanController = FindFirstObjectByType<RoomScanController>();

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

            UpdateProgressUI();
        }
    }
}