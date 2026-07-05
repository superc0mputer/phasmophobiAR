using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class GhostCaptureUI : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GhostRevealCaptureController m_CaptureController;

        [SerializeField]
        GameObject m_CaptureRoot;

        [SerializeField]
        Slider m_CaptureProgressSlider;

        [SerializeField]
        TMP_Text m_CaptureProgressText;

        [SerializeField]
        TMP_Text m_CaptureStateText;

        bool m_IsSubscribed;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

        }

        void OnEnable()
        {
            Subscribe();
            if (m_GameStateManager != null)
                OnPhaseChanged(m_GameStateManager.CurrentPhase);

            UpdateUI();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GameStateManager gameStateManager,
            GhostRevealCaptureController captureController,
            GameObject captureRoot,
            Slider captureProgressSlider,
            TMP_Text captureProgressText,
            TMP_Text captureStateText)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_CaptureController = captureController ?? m_CaptureController;
            m_CaptureRoot = captureRoot ?? m_CaptureRoot;
            m_CaptureProgressSlider = captureProgressSlider ?? m_CaptureProgressSlider;
            m_CaptureProgressText = captureProgressText ?? m_CaptureProgressText;
            m_CaptureStateText = captureStateText ?? m_CaptureStateText;

            UpdateUI();
        }

        void Update()
        {
            UpdateUI();
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_GameStateManager != null)
            {
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
                m_GameStateManager.PhaseChanged += OnPhaseChanged;
            }

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
            if (m_CaptureRoot != null)
                m_CaptureRoot.SetActive(IsStatusUiActive());

            UpdateUI();
        }

        void UpdateUI()
        {
            if (m_CaptureController == null)
                m_CaptureController = FindCaptureController();

            var active = IsStatusUiActive();
            if (m_CaptureRoot != null)
                m_CaptureRoot.SetActive(active);

            if (!active)
                return;

            var showCaptureProgress = m_CaptureController != null
                && m_CaptureController.CaptureRequested
                && !m_CaptureController.HasCompletedCapture;
            var progress = m_CaptureController != null ? m_CaptureController.CaptureProgress : 0f;

            if (m_CaptureProgressSlider != null)
            {
                m_CaptureProgressSlider.gameObject.SetActive(showCaptureProgress);
                m_CaptureProgressSlider.value = progress;
            }

            if (m_CaptureProgressText != null)
            {
                m_CaptureProgressText.gameObject.SetActive(showCaptureProgress);
                m_CaptureProgressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            if (m_CaptureStateText != null)
            {
                m_CaptureStateText.gameObject.SetActive(true);
                m_CaptureStateText.text = GetStatusText();
            }
        }

        bool IsStatusUiActive()
        {
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
                return false;

            if (m_CaptureController == null)
                m_CaptureController = FindCaptureController();

            return m_CaptureController == null || !m_CaptureController.HasCompletedCapture;
        }

        static GhostRevealCaptureController FindCaptureController()
        {
            var spawnedGhosts = GhostSpawnController.GetSpawnedGhostInfos();
            foreach (var info in spawnedGhosts)
            {
                if (info?.ghostTransform == null)
                    continue;

                var controller = info.ghostTransform.GetComponent<GhostRevealCaptureController>();
                if (controller != null)
                    return controller;
            }

            return FindAnyObjectByType<GhostRevealCaptureController>();
        }

        string GetStatusText()
        {
            if (m_CaptureController == null)
                return "Hold still to reveal";

            if (m_CaptureController.IsFakeCaptureFailureActive)
                return "SIGNAL LOST";

            if (!m_CaptureController.CaptureRequested)
            {
                switch (m_CaptureController.CurrentState)
                {
                    case GhostRevealState.PartialReveal:
                        return "Ghost is stirring";
                    case GhostRevealState.Revealed:
                    case GhostRevealState.Capturing:
                    case GhostRevealState.Captured:
                        return "Ghost revealed";
                    default:
                        return "Hold still to reveal";
                }
            }

            switch (m_CaptureController.CurrentState)
            {
                case GhostRevealState.PartialReveal:
                    return "Ghost is stirring";
                case GhostRevealState.Revealed:
                    return "Center the ghost";
                case GhostRevealState.Capturing:
                    return "Capturing...";
                case GhostRevealState.Captured:
                    return "Captured";
                default:
                    return "Hold still to reveal";
            }
        }
    }
}
