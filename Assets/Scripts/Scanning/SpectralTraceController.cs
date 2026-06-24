using System;
using System.Collections;
using System.Collections.Generic;
using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using UnityEngine;
using TMPro;

namespace PhasmophobiAR.Scanning
{
    /// <summary>
    /// Spawns transient spectral traces near hidden ghosts while in Spectral scanner mode.
    /// Traces are more likely and more visible when tracking confidence is higher.
    /// </summary>
    public sealed class SpectralTraceController : MonoBehaviour
    {
        [SerializeField]
        ScannerModeManager m_ScannerModeManager;

        [SerializeField]
        RoomScanController m_RoomScanController;

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [SerializeField]
        Camera m_ARCamera;

        [SerializeField]
        TMP_Text m_TracesText;

        [Header("Tuning")]
        [SerializeField]
        float m_MaxDistance = 6f;

        [SerializeField]
        float m_MaxAngleDegrees = 60f;

        [SerializeField]
        float m_TraceIntervalSeconds = 1.0f;

        [SerializeField]
        float m_TraceLifetimeSeconds = 6.0f;

        [SerializeField]
        TrackingConfidence m_MinTrackingRequired = TrackingConfidence.Limited;

        readonly Dictionary<Transform, float> m_LastTraceTime = new Dictionary<Transform, float>();
        readonly List<GameObject> m_ActiveTraces = new List<GameObject>();
        bool m_HasRecordedSpectralTrace;

        void Awake()
        {
            if (m_ScannerModeManager == null)
                m_ScannerModeManager = ScannerModeManager.Instance;

            if (m_RoomScanController == null)
                m_RoomScanController = UnityEngine.Object.FindAnyObjectByType<RoomScanController>();

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main;
        }

        public void Configure(
            ScannerModeManager scanner,
            RoomScanController scanController,
            GameStateManager gameStateManager,
            Camera arCamera,
            TMP_Text tracesText,
            EvidenceRegistry evidenceRegistry = null)
        {
            m_ScannerModeManager = scanner ?? m_ScannerModeManager;
            m_RoomScanController = scanController ?? m_RoomScanController;
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_ARCamera = arCamera ?? m_ARCamera;
            m_TracesText = tracesText ?? m_TracesText;
            m_EvidenceRegistry = evidenceRegistry ?? m_EvidenceRegistry;
        }

        void OnEnable()
        {
            if (m_ScannerModeManager != null)
                m_ScannerModeManager.ModeChanged += OnModeChanged;

            if (m_EvidenceRegistry != null)
                m_EvidenceRegistry.EvidenceCleared += OnEvidenceCleared;

            UpdateTracesText();
        }

        void OnDisable()
        {
            if (m_ScannerModeManager != null)
                m_ScannerModeManager.ModeChanged -= OnModeChanged;

            if (m_EvidenceRegistry != null)
                m_EvidenceRegistry.EvidenceCleared -= OnEvidenceCleared;
        }

        void OnModeChanged(ScannerMode newMode)
        {
            if (newMode != ScannerMode.Spectral)
            {
                // clear existing traces when leaving spectral mode
                ClearAllTraces();
            }

            UpdateTracesText();
        }

        void Update()
        {
            if (m_ScannerModeManager == null || m_ARCamera == null)
                return;

            if (m_ScannerModeManager.CurrentMode != ScannerMode.Spectral)
                return;

            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
                return;

            if (m_RoomScanController == null)
                return;

            // Require some tracking confidence
            if (m_RoomScanController.Confidence < m_MinTrackingRequired)
                return;

            var now = Time.time;
            var ghosts = GhostSpawnController.GetSpawnedGhostInfos();
            if (ghosts == null || ghosts.Length == 0)
                return;

            var camPos = m_ARCamera.transform.position;
            var forwardProj = Vector3.ProjectOnPlane(m_ARCamera.transform.forward, Vector3.up);
            if (forwardProj.sqrMagnitude < 0.001f)
                return;

            forwardProj.Normalize();

            foreach (var ghost in ghosts)
            {
                if (ghost == null || ghost.ghostTransform == null) continue;

                // Throttle per-ghost traces
                m_LastTraceTime.TryGetValue(ghost.ghostTransform, out var lastTime);
                if (now - lastTime < m_TraceIntervalSeconds)
                    continue;

                var ghostPosition = ghost.WorldPosition;
                var toGhost = ghostPosition - camPos;
                var horiz = Vector3.ProjectOnPlane(toGhost, Vector3.up);
                var distance = horiz.magnitude;
                if (distance > m_MaxDistance)
                    continue;
                if (horiz.sqrMagnitude < 0.001f)
                    continue;

                var dir = horiz.normalized;
                var angle = Vector3.Angle(forwardProj, dir);
                if (angle > m_MaxAngleDegrees)
                    continue;

                // spawn a trace near the ghost, slightly offset so it appears on floor
                var tracePos = ghostPosition + Vector3.up * -0.1f;
                SpawnTrace(tracePos);
                m_LastTraceTime[ghost.ghostTransform] = now;
            }

            UpdateTracesText();
        }

        void SpawnTrace(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.08f;
            go.transform.SetParent(transform, true);
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                rend.material.SetColor("_BaseColor", new Color(0.6f, 0.9f, 1f, 0.9f));
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            m_ActiveTraces.Add(go);
            TryRecordSpectralTraceEvidence();
            StartCoroutine(FadeAndDestroy(go, m_TraceLifetimeSeconds));
        }

        void TryRecordSpectralTraceEvidence()
        {
            if (m_HasRecordedSpectralTrace)
                return;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            if (m_EvidenceRegistry == null)
            {
                Debug.LogWarning("Spectral trace evidence could not be recorded because no EvidenceRegistry exists in the scene.");
                return;
            }

            m_HasRecordedSpectralTrace = true;
            m_EvidenceRegistry.RecordEvidence(EvidenceType.SpectralTrace);
        }

        void OnEvidenceCleared()
        {
            m_HasRecordedSpectralTrace = false;
        }

        IEnumerator FadeAndDestroy(GameObject go, float life)
        {
            var rend = go.GetComponent<Renderer>();
            float start = Time.time;
            Color startColor = rend != null ? rend.material.GetColor("_BaseColor") : Color.white;
            while (Time.time - start < life)
            {
                var t = (Time.time - start) / life;
                if (rend != null)
                {
                    var c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, t);
                    rend.material.SetColor("_BaseColor", c);
                }
                yield return null;
            }

            m_ActiveTraces.Remove(go);
            Destroy(go);
            UpdateTracesText();
        }

        void ClearAllTraces()
        {
            foreach (var go in m_ActiveTraces)
                if (go != null) Destroy(go);
            m_ActiveTraces.Clear();
            UpdateTracesText();
        }

        void UpdateTracesText()
        {
            if (m_TracesText != null)
            {
                var shouldShowText = ShouldShowTracesText();
                m_TracesText.gameObject.SetActive(shouldShowText);

                if (shouldShowText)
                    m_TracesText.text = $"Traces: {m_ActiveTraces.Count}";
            }
        }

        bool ShouldShowTracesText()
        {
            if (m_ScannerModeManager == null || m_ScannerModeManager.CurrentMode != ScannerMode.Spectral)
                return false;

            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
                return false;

            if (m_RoomScanController == null)
                return false;

            return m_RoomScanController.Confidence >= m_MinTrackingRequired;
        }
    }
}
