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
                m_CaptureRoot.SetActive(phase == GamePhase.Investigation);

            UpdateUI();
        }

        void UpdateUI()
        {
            if (m_CaptureController == null)
                m_CaptureController = FindAnyObjectByType<GhostRevealCaptureController>();

            var active = m_GameStateManager == null || m_GameStateManager.CurrentPhase == GamePhase.Investigation;
            if (m_CaptureRoot != null)
                m_CaptureRoot.SetActive(active);

            if (m_CaptureController == null || !active)
                return;

            var progress = m_CaptureController.CaptureProgress;
            if (m_CaptureProgressSlider != null)
            {
                m_CaptureProgressSlider.gameObject.SetActive(true);
                m_CaptureProgressSlider.value = progress;
            }

            if (m_CaptureProgressText != null)
            {
                m_CaptureProgressText.gameObject.SetActive(true);
                m_CaptureProgressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            if (m_CaptureStateText != null)
            {
                m_CaptureStateText.gameObject.SetActive(true);
                m_CaptureStateText.text = m_CaptureController.CurrentState switch
                {
                    GhostRevealState.Hidden => "Hold still to reveal",
                    GhostRevealState.PartialReveal => "Ghost is stirring",
                    GhostRevealState.Revealed => "Center the ghost",
                    GhostRevealState.Capturing => "Capturing...",
                    GhostRevealState.Captured => "Captured",
                    _ => ""
                };
            }
        }
    }
}
