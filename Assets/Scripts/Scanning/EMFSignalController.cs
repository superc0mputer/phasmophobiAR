using System;
using PhasmophobiAR.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.Scanning
{
    /// <summary>
    /// Computes an EMF signal strength based on camera-to-ghost distance and direction.
    /// Updates a HUD slider when in EMF scanner mode.
    /// </summary>
    public sealed class EMFSignalController : MonoBehaviour
    {
        [SerializeField]
        ScannerModeManager m_ScannerModeManager;

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        Camera m_ARCamera;

        [SerializeField]
        Slider m_EMFSlider;

        [Header("Tuning")]
        [SerializeField]
        EMFSignalSettings m_SignalSettings = new EMFSignalSettings();

        [SerializeField]
        float m_Smoothing = 8f;

        float m_CurrentValue;

        public float CurrentValue => m_CurrentValue;
        public int CurrentLevel => EMFSignalCalculator.ToEMFLevel(m_CurrentValue, m_SignalSettings);

        void Awake()
        {
            if (m_ScannerModeManager == null)
                m_ScannerModeManager = ScannerModeManager.Instance;

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main;
        }

        public void Configure(ScannerModeManager scannerModeManager, GameStateManager gameStateManager, Camera arCamera, Slider emfSlider)
        {
            m_ScannerModeManager = scannerModeManager ?? m_ScannerModeManager;
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_ARCamera = arCamera ?? m_ARCamera;
            m_EMFSlider = emfSlider ?? m_EMFSlider;
        }

        void OnEnable()
        {
            if (m_ScannerModeManager != null)
                m_ScannerModeManager.ModeChanged += OnModeChanged;
        }

        void OnDisable()
        {
            if (m_ScannerModeManager != null)
                m_ScannerModeManager.ModeChanged -= OnModeChanged;
        }

        void OnModeChanged(ScannerMode newMode)
        {
            // reset when leaving EMF mode
            if (newMode != ScannerMode.EMF)
            {
                m_CurrentValue = 0f;
                UpdateSlider(0f);
            }
        }

        void Update()
        {
            if (m_ScannerModeManager == null || m_ARCamera == null || m_EMFSlider == null)
                return;

            if (m_ScannerModeManager.CurrentMode != ScannerMode.EMF)
                return;

            // Only show during Investigation
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
            {
                UpdateSlider(0f);
                return;
            }

            var signal = EMFSignalCalculator.CalculateFromSpawnedGhosts(
                m_ARCamera.transform.position,
                m_ARCamera.transform.forward,
                m_SignalSettings,
                true);
            var ghosts = GhostSpawnController.GetSpawnedGhostInfos();
            if (ghosts == null || ghosts.Length == 0)
            {
                SmoothTo(0f);
                return;
            }

            // find nearest ghost by projected distance (horizontal)
            var camPos = m_ARCamera.transform.position;
            var camForward = m_ARCamera.transform.forward;

            float best = 0f;
            foreach (var ghost in ghosts)
            {
                if (ghost == null) continue;
                var toGhost = ghost.WorldPosition - camPos;
                var distance = toGhost.magnitude;
                var projectedToGhost = Vector3.ProjectOnPlane(toGhost, Vector3.up);
                var forwardProj = Vector3.ProjectOnPlane(camForward, Vector3.up);
                if (projectedToGhost.sqrMagnitude < 0.001f || forwardProj.sqrMagnitude < 0.001f)
                    continue;

                var dir = projectedToGhost.normalized;
                forwardProj.Normalize();
                var angle = Vector3.Angle(forwardProj, dir);

                float distanceFactor = Mathf.Clamp01(1f - (distance / m_MaxDistance));
                float directionFactor = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));
                float strength = distanceFactor * ( (1f - m_DirectionWeight) + m_DirectionWeight * directionFactor );
                if (strength > best) best = strength;
            }

            SmoothTo(signal);
        }

        void SmoothTo(float target)
        {
            m_CurrentValue = Mathf.Lerp(m_CurrentValue, target, Mathf.Clamp01(Time.deltaTime * m_Smoothing));
            UpdateSlider(m_CurrentValue);
        }

        void UpdateSlider(float value)
        {
            if (m_EMFSlider != null)
                m_EMFSlider.value = Mathf.Clamp01(value);
        }
    }
}
