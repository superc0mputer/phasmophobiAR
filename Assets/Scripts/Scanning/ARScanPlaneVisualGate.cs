using PhasmophobiAR.Game;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Templates.AR;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

namespace PhasmophobiAR.Scanning
{
    [RequireComponent(typeof(Renderer))]
    public sealed class ARScanPlaneVisualGate : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        Renderer[] m_Renderers;
        ARPlaneMeshVisualizer m_PlaneMeshVisualizer;
        ARPlaneMeshVisualizerFader m_PlaneVisualizerFader;
        ARFeatheredPlaneMeshVisualizer m_FeatheredPlaneVisualizer;
        bool m_LastVisibleState;
        bool m_HasAppliedVisibleState;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_PlaneMeshVisualizer = GetComponent<ARPlaneMeshVisualizer>();
            m_PlaneVisualizerFader = GetComponent<ARPlaneMeshVisualizerFader>();
            m_FeatheredPlaneVisualizer = GetComponent<ARFeatheredPlaneMeshVisualizer>();
        }

        void OnEnable()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;

            ApplyCurrentPhase();
        }

        void OnDisable()
        {
            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            Apply(phase);
        }

        void LateUpdate()
        {
            ApplyCurrentPhase();
        }

        void ApplyCurrentPhase()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            var phase = m_GameStateManager != null ? m_GameStateManager.CurrentPhase : GamePhase.RoomScan;
            Apply(phase);
        }

        void Apply(GamePhase phase)
        {
            var visible = phase == GamePhase.RoomScan;
            if (m_HasAppliedVisibleState && visible == m_LastVisibleState && m_Renderers != null && m_Renderers.Length > 0)
                return;

            m_LastVisibleState = visible;
            m_HasAppliedVisibleState = true;

            if (m_Renderers == null || m_Renderers.Length == 0)
                m_Renderers = GetComponentsInChildren<Renderer>(true);

            if (m_PlaneMeshVisualizer == null)
                m_PlaneMeshVisualizer = GetComponent<ARPlaneMeshVisualizer>();

            if (m_PlaneVisualizerFader == null)
                m_PlaneVisualizerFader = GetComponent<ARPlaneMeshVisualizerFader>();

            if (m_FeatheredPlaneVisualizer == null)
                m_FeatheredPlaneVisualizer = GetComponent<ARFeatheredPlaneMeshVisualizer>();

            if (m_PlaneMeshVisualizer != null)
                m_PlaneMeshVisualizer.enabled = visible;

            if (m_PlaneVisualizerFader != null)
                m_PlaneVisualizerFader.enabled = visible;

            if (m_FeatheredPlaneVisualizer != null)
                m_FeatheredPlaneVisualizer.enabled = visible;

            foreach (var currentRenderer in m_Renderers)
            {
                if (currentRenderer != null)
                    currentRenderer.enabled = visible;
            }
        }
    }
}
