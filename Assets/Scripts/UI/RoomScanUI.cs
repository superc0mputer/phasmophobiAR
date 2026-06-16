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

        bool m_IsSubscribed;

        public void Configure(
            GameStateManager gameStateManager,
            RoomScanController roomScanController,
            GameObject scanRoot,
            GameObject investigationRoot,
            Slider progressSlider,
            TMP_Text progressText,
            TMP_Text trackingText,
            TMP_Text instructionText)
        {
            m_GameStateManager = gameStateManager;
            m_RoomScanController = roomScanController;
            m_ScanRoot = scanRoot;
            m_InvestigationRoot = investigationRoot;
            m_ProgressSlider = progressSlider;
            m_ProgressText = progressText;
            m_TrackingText = trackingText;
            m_InstructionText = instructionText;

            if (isActiveAndEnabled)
                Subscribe();
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
        }

        void OnScanUpdated(RoomScanController.ScanSnapshot snapshot)
        {
            if (m_ProgressSlider != null)
                m_ProgressSlider.value = snapshot.progress;

            if (m_ProgressText != null)
                m_ProgressText.text = $"{Mathf.RoundToInt(snapshot.progress * 100f)}%";

            if (m_TrackingText != null)
                m_TrackingText.text = $"Tracking: {snapshot.confidence}";

            if (m_InstructionText != null)
                m_InstructionText.text = snapshot.instruction;
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;

            if (m_RoomScanController != null)
                m_RoomScanController.ScanUpdated += OnScanUpdated;

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

            m_IsSubscribed = false;
        }
    }
}
