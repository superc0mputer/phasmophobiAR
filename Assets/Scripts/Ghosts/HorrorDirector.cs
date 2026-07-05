using System.Collections;
using System.Collections.Generic;
using PhasmophobiAR.Game;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Hidden pacing system shared by all modular horror events on a ghost.</summary>
    [DisallowMultipleComponent]
    public sealed class HorrorDirector : MonoBehaviour
    {
        [Header("Hidden tension")]
        [SerializeField, Range(0f, 1f)] float m_StartingTension = 0.08f;
        [SerializeField, Min(0f)] float m_PassiveTensionPerSecond = 0.0035f;
        [SerializeField, Min(0f)] float m_NearGhostTensionPerSecond = 0.007f;
        [SerializeField, Min(0f)] float m_CaptureTensionPerSecond = 0.018f;
        [SerializeField] float m_NearGhostDistanceMeters = 2.5f;

        [Header("Pacing")]
        [SerializeField] float m_InitialQuietPeriodSeconds = 14f;
        [SerializeField] Vector2 m_EventIntervalAtLowTension = new Vector2(28f, 45f);
        [SerializeField] Vector2 m_EventIntervalAtHighTension = new Vector2(10f, 19f);
        [SerializeField, Range(0f, 1f)] float m_TensionReliefAfterEvent = 0.08f;

        readonly List<HorrorEvent> m_Events = new List<HorrorEvent>();
        GameStateManager m_GameState;
        GhostBehaviorController m_GhostBehavior;
        GhostRevealCaptureController m_CaptureController;
        Transform m_ARCamera;
        float m_NextEventTime;
        bool m_IsPlayingEvent;

        public float Tension { get; private set; }
        public float Aggression => Tension;
        public Transform ARCamera => m_ARCamera;
        public GhostBehaviorController GhostBehavior => m_GhostBehavior;
        public GhostRevealCaptureController CaptureController => m_CaptureController;
        public bool IsPlayingEvent => m_IsPlayingEvent;

        public void Configure(GameStateManager gameState, Transform arCamera, GhostBehaviorController behavior, GhostRevealCaptureController capture)
        {
            m_GameState = gameState != null ? gameState : GameStateManager.Instance;
            m_ARCamera = arCamera != null ? arCamera : Camera.main != null ? Camera.main.transform : null;
            m_GhostBehavior = behavior;
            m_CaptureController = capture;
            CacheEvents();
            ResetDirector();
        }

        void OnEnable()
        {
            if (m_GameState == null) m_GameState = GameStateManager.Instance;
            if (m_GameState != null) m_GameState.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            if (m_GameState != null) m_GameState.PhaseChanged -= OnPhaseChanged;
            StopAllCoroutines();
            CancelEvents();
            m_IsPlayingEvent = false;
        }

        void Update()
        {
            if (m_GameState != null && m_GameState.CurrentPhase != GamePhase.Investigation) return;
            if (m_ARCamera == null || m_IsPlayingEvent) return;

            IncreaseTension(Time.unscaledDeltaTime);
            var urgentEvent = SelectUrgentEvent();
            if (urgentEvent != null)
            {
                StartCoroutine(PlayEvent(urgentEvent));
                return;
            }

            if (Time.unscaledTime < m_NextEventTime) return;

            var selectedEvent = SelectEvent();
            if (selectedEvent == null)
            {
                m_NextEventTime = Time.unscaledTime + 2f;
                return;
            }
            StartCoroutine(PlayEvent(selectedEvent));
        }

        public void AddTension(float amount)
        {
            Tension = Mathf.Clamp01(Tension + amount);
        }

        void IncreaseTension(float deltaTime)
        {
            var rate = m_PassiveTensionPerSecond;
            if (m_GhostBehavior != null && Vector3.Distance(m_ARCamera.position, m_GhostBehavior.transform.position) <= m_NearGhostDistanceMeters)
                rate += m_NearGhostTensionPerSecond;
            if (m_CaptureController != null && m_CaptureController.CurrentState == GhostRevealState.Capturing)
                rate += m_CaptureTensionPerSecond;
            AddTension(rate * deltaTime);
        }

        HorrorEvent SelectEvent()
        {
            CacheEvents();
            var totalWeight = 0f;
            foreach (var horrorEvent in m_Events)
                if (horrorEvent != null && horrorEvent.CanTrigger(this)) totalWeight += horrorEvent.SelectionWeight;
            if (totalWeight <= 0f) return null;

            var roll = Random.value * totalWeight;
            foreach (var horrorEvent in m_Events)
            {
                if (horrorEvent == null || !horrorEvent.CanTrigger(this)) continue;
                roll -= horrorEvent.SelectionWeight;
                if (roll <= 0f) return horrorEvent;
            }
            return null;
        }

        HorrorEvent SelectUrgentEvent()
        {
            CacheEvents();
            foreach (var horrorEvent in m_Events)
                if (horrorEvent != null && horrorEvent.CanTrigger(this) && horrorEvent.WantsImmediateTrigger(this))
                    return horrorEvent;
            return null;
        }

        IEnumerator PlayEvent(HorrorEvent horrorEvent)
        {
            m_IsPlayingEvent = true;
            yield return horrorEvent.Trigger(this);
            Tension = Mathf.Clamp01(Tension - m_TensionReliefAfterEvent);
            ScheduleNextEvent();
            m_IsPlayingEvent = false;
        }

        void CacheEvents()
        {
            m_Events.Clear();
            GetComponents(m_Events);
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Investigation) ResetDirector();
            else
            {
                StopAllCoroutines();
                CancelEvents();
                m_IsPlayingEvent = false;
            }
        }

        void ResetDirector()
        {
            Tension = Mathf.Clamp01(m_StartingTension);
            m_IsPlayingEvent = false;
            CacheEvents();
            foreach (var horrorEvent in m_Events) horrorEvent.ResetForInvestigation();
            m_NextEventTime = Time.unscaledTime + Mathf.Max(0f, m_InitialQuietPeriodSeconds);
        }

        void ScheduleNextEvent()
        {
            var low = Random.Range(m_EventIntervalAtLowTension.x, m_EventIntervalAtLowTension.y);
            var high = Random.Range(m_EventIntervalAtHighTension.x, m_EventIntervalAtHighTension.y);
            m_NextEventTime = Time.unscaledTime + Mathf.Max(2f, Mathf.Lerp(low, high, Tension));
        }

        void CancelEvents()
        {
            CacheEvents();
            foreach (var horrorEvent in m_Events) horrorEvent.Cancel();
        }
    }
}
