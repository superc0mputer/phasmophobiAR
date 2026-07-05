using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class RoomScanUI : MonoBehaviour
    {
        static readonly Color s_TrackingGoodColor = new Color(0.18f, 0.92f, 0.76f);
        static readonly Color s_TrackingLimitedColor = new Color(1f, 0.72f, 0.25f);
        static readonly Color s_TrackingPoorColor = new Color(1f, 0.32f, 0.28f);
        static readonly Color s_TrackingUnavailableColor = new Color(0.64f, 0.72f, 0.72f);

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        RoomScanController m_RoomScanController;

        [SerializeField]
        GameObject m_ScanRoot;

        [SerializeField]
        GameObject m_InvestigationRoot;

        [SerializeField]
        Slider m_ProgressSlider;

        [SerializeField]
        TMP_Text m_ProgressText;

        [SerializeField]
        TMP_Text m_TrackingText;

        [SerializeField]
        TMP_Text m_InstructionText;

        [SerializeField]
        TMP_Text m_RoomSignalsText;

        [SerializeField]
        Button m_StartInvestigationButton;

        bool m_IsSubscribed;

        public void Configure(
            GameStateManager gameStateManager,
            RoomScanController roomScanController,
            GameObject scanRoot,
            GameObject investigationRoot,
            Slider progressSlider,
            TMP_Text progressText,
            TMP_Text trackingText,
            TMP_Text instructionText,
            TMP_Text roomSignalsText,
            Button startInvestigationButton)
        {
            m_GameStateManager = gameStateManager;
            m_RoomScanController = roomScanController;
            m_ScanRoot = scanRoot;
            m_InvestigationRoot = investigationRoot;
            m_ProgressSlider = progressSlider;
            m_ProgressText = progressText;
            m_TrackingText = trackingText;
            m_InstructionText = instructionText;
            m_RoomSignalsText = roomSignalsText;
            m_StartInvestigationButton = startInvestigationButton;

            if (isActiveAndEnabled)
            {
                Unsubscribe();
                Subscribe();
            }
        }

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            // This legacy label repeated the progress bar and instruction.
            // Preserve its serialized reference, but keep it out of the scan HUD.
            if (m_RoomSignalsText != null)
                m_RoomSignalsText.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Start()
        {
            if (m_GameStateManager != null)
                OnPhaseChanged(m_GameStateManager.CurrentPhase);
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (m_ScanRoot != null)
                m_ScanRoot.SetActive(phase == GamePhase.RoomScan);

            if (m_InvestigationRoot != null)
                m_InvestigationRoot.SetActive(phase == GamePhase.Investigation);

            if (m_StartInvestigationButton != null && phase == GamePhase.RoomScan)
                m_StartInvestigationButton.gameObject.SetActive(false);

            if (m_InstructionText != null && phase == GamePhase.RoomScan)
                m_InstructionText.gameObject.SetActive(true);
        }

        void OnScanUpdated(RoomScanController.ScanSnapshot snapshot)
        {
            if (m_ProgressSlider != null)
                m_ProgressSlider.value = snapshot.progress;

            if (m_ProgressText != null)
                m_ProgressText.text = $"{Mathf.RoundToInt(snapshot.progress * 100f)}%";

            if (m_TrackingText != null)
            {
                m_TrackingText.text = snapshot.isReady
                    ? "●  ROOM LOCKED"
                    : GetTrackingLabel(snapshot.confidence);
                m_TrackingText.color = snapshot.isReady
                    ? s_TrackingGoodColor
                    : GetTrackingColor(snapshot.confidence);
            }

            if (m_InstructionText != null)
            {
                m_InstructionText.text = GetPlayerInstruction(snapshot);
                m_InstructionText.gameObject.SetActive(!snapshot.isReady);
            }

            if (m_StartInvestigationButton != null)
            {
                m_StartInvestigationButton.gameObject.SetActive(snapshot.isReady);
                m_StartInvestigationButton.interactable = snapshot.isReady
                    && (snapshot.confidence == TrackingConfidence.Good || snapshot.confidence == TrackingConfidence.Limited);
            }
        }

        static string GetTrackingLabel(TrackingConfidence confidence)
        {
            switch (confidence)
            {
                case TrackingConfidence.Good:
                    return "●  TRACKING";
                case TrackingConfidence.Limited:
                    return "●  LIMITED";
                case TrackingConfidence.Poor:
                    return "●  SIGNAL LOST";
                default:
                    return "●  SEARCHING";
            }
        }

        static Color GetTrackingColor(TrackingConfidence confidence)
        {
            switch (confidence)
            {
                case TrackingConfidence.Good:
                    return s_TrackingGoodColor;
                case TrackingConfidence.Limited:
                    return s_TrackingLimitedColor;
                case TrackingConfidence.Poor:
                    return s_TrackingPoorColor;
                default:
                    return s_TrackingUnavailableColor;
            }
        }

        static string GetPlayerInstruction(RoomScanController.ScanSnapshot snapshot)
        {
            if (snapshot.isReady)
                return "Room mapped";

            if (snapshot.confidence == TrackingConfidence.Unavailable || snapshot.confidence == TrackingConfidence.Poor)
                return "Move slowly — keep surfaces in view";

            if (snapshot.trackedPlaneCount == 0)
                return "Aim at a wall or floor";

            if (!snapshot.hasEstimatedBounds)
                return "Sweep across the room";

            return "Keep moving slowly";
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;

            if (m_RoomScanController != null)
                m_RoomScanController.ScanUpdated += OnScanUpdated;

            if (m_StartInvestigationButton != null && m_RoomScanController != null)
                m_StartInvestigationButton.onClick.AddListener(m_RoomScanController.ConfirmScan);

            m_IsSubscribed = true;
        }

        void Unsubscribe()
        {
            if (!m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;

            if (m_RoomScanController != null)
                m_RoomScanController.ScanUpdated -= OnScanUpdated;

            if (m_StartInvestigationButton != null && m_RoomScanController != null)
                m_StartInvestigationButton.onClick.RemoveListener(m_RoomScanController.ConfirmScan);

            m_IsSubscribed = false;
        }
    }
}
