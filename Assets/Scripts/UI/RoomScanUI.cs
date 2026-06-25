using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class RoomScanUI : MonoBehaviour
    {
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
        }

        void OnScanUpdated(RoomScanController.ScanSnapshot snapshot)
        {
            if (m_ProgressSlider != null)
                m_ProgressSlider.value = snapshot.progress;

            if (m_ProgressText != null)
                m_ProgressText.text = $"SCAN {Mathf.RoundToInt(snapshot.progress * 100f)}%";

            if (m_TrackingText != null)
                m_TrackingText.text = $"Signal: {GetSignalLabel(snapshot.confidence)}";

            if (m_InstructionText != null)
                m_InstructionText.text = GetPlayerInstruction(snapshot);

            if (m_RoomSignalsText != null)
                m_RoomSignalsText.text = GetScanStatus(snapshot);

            if (m_StartInvestigationButton != null)
            {
                m_StartInvestigationButton.gameObject.SetActive(snapshot.isReady);
                m_StartInvestigationButton.interactable = snapshot.isReady
                    && (snapshot.confidence == TrackingConfidence.Good || snapshot.confidence == TrackingConfidence.Limited);
            }
        }

        static string GetRoomDetailLabel(RoomScanController.ScanSnapshot snapshot)
        {
            if (snapshot.hasLiDARMeshData || snapshot.featurePointCount >= 120)
                return "high";

            if (snapshot.featurePointCount >= 45)
                return "medium";

            if (snapshot.featurePointCount > 0)
                return "low";

            return "none";
        }

        static string GetSignalLabel(TrackingConfidence confidence)
        {
            switch (confidence)
            {
                case TrackingConfidence.Good:
                    return "stable";
                case TrackingConfidence.Limited:
                    return "flickering";
                case TrackingConfidence.Poor:
                    return "unstable";
                default:
                    return "searching";
            }
        }

        static string GetPlayerInstruction(RoomScanController.ScanSnapshot snapshot)
        {
            if (snapshot.isReady)
                return "The room has enough signal. Begin the hunt when you are ready.";

            if (snapshot.confidence == TrackingConfidence.Unavailable || snapshot.confidence == TrackingConfidence.Poor)
                return "Move slowly. Let the camera find edges, shelves, and corners.";

            return snapshot.instruction;
        }

        static string GetScanStatus(RoomScanController.ScanSnapshot snapshot)
        {
            if (snapshot.isReady)
                return "Room imprint locked. Ghost placement is ready.";

            var detail = GetRoomDetailLabel(snapshot);
            if (detail == "none" || detail == "low")
                return "Searching for haunted anchors...";

            if (!snapshot.hasEstimatedBounds)
                return "Mapping walls and corners...";

            return "Building the investigation space...";
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
