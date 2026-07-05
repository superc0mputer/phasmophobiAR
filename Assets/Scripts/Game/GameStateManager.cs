using System;
using PhasmophobiAR.Ghosts;
using PhasmophobiAR.Scanning;
using UnityEngine;
using UnityEngine.Events;

namespace PhasmophobiAR.Game
{
    public sealed class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField]
        GamePhase m_InitialPhase = GamePhase.Setup;

        [SerializeField]
        bool m_StartRoomScanOnStart = true;

        [SerializeField]
        UnityEvent<GamePhase> m_PhaseChanged = new UnityEvent<GamePhase>();

        [SerializeField]
        UnityEvent m_ScanCompleted = new UnityEvent();

        [SerializeField]
        IdentificationController m_IdentificationController;

        GamePhase m_CurrentPhase;
        bool m_HasCompletedRoomScan;
        RoomScanResult m_LastRoomScanResult;
        RoundResult m_LastRoundResult;
        CaptureOutcome m_LastCaptureOutcome = CaptureOutcome.None;
        float m_LastCaptureProgress;
        float m_LastCaptureDurationSeconds;
        string m_LastCaptureReason = string.Empty;

        public event Action<GamePhase> PhaseChanged;
        public event Action ScanCompleted;
        public event Action<RoomScanResult> ScanCompletedWithResult;
        public event Action<RoundResult> ResultPrepared;

        public GamePhase CurrentPhase => m_CurrentPhase;
        public bool HasCompletedRoomScan => m_HasCompletedRoomScan;
        public RoomScanResult LastRoomScanResult => m_LastRoomScanResult;
        public bool CanPlaceTools => m_CurrentPhase == GamePhase.Investigation;
        public bool CanCaptureGhost => m_CurrentPhase == GamePhase.Investigation;
        public RoundResult LastRoundResult => m_LastRoundResult;
        public CaptureOutcome LastCaptureOutcome => m_LastCaptureOutcome;
        public float LastCaptureProgress => m_LastCaptureProgress;
        public float LastCaptureDurationSeconds => m_LastCaptureDurationSeconds;
        public string LastCaptureReason => m_LastCaptureReason;
        public UnityEvent<GamePhase> phaseChanged => m_PhaseChanged;
        public UnityEvent scanCompleted => m_ScanCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(GameStateManager)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;
            m_CurrentPhase = m_InitialPhase;
            m_HasCompletedRoomScan = m_InitialPhase == GamePhase.Investigation || m_InitialPhase == GamePhase.Result;

            if (m_IdentificationController == null)
                m_IdentificationController = IdentificationController.Instance;
        }

        void Start()
        {
            NotifyPhaseChanged();

            if (m_StartRoomScanOnStart && m_CurrentPhase == GamePhase.Setup)
                BeginRoomScan();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginRoomScan()
        {
            if (m_HasCompletedRoomScan)
                return;

            SetPhase(GamePhase.RoomScan);
        }

        public void CompleteRoomScan()
        {
            CompleteRoomScan(null);
        }

        public void CompleteRoomScan(RoomScanResult scanResult)
        {
            if (m_HasCompletedRoomScan)
                return;

            m_HasCompletedRoomScan = true;
            m_LastRoomScanResult = scanResult;
            m_ScanCompleted.Invoke();
            ScanCompleted?.Invoke();
            ScanCompletedWithResult?.Invoke(m_LastRoomScanResult);
            SetPhase(GamePhase.Investigation);
        }

        public void ShowResult()
        {
            PrepareResult();

            SetPhase(GamePhase.Result);
        }

        public RoundResult PrepareResult()
        {
            if (m_LastRoundResult != null && m_LastCaptureOutcome != CaptureOutcome.None)
                return m_LastRoundResult;

            if (m_IdentificationController == null)
                m_IdentificationController = IdentificationController.Instance;

            if (m_IdentificationController == null)
            {
                Debug.LogWarning("Result requested without an IdentificationController in the scene.");
                return null;
            }

            m_LastRoundResult = m_IdentificationController.Evaluate();
            ApplyCaptureOutcome(m_LastRoundResult);
            ResultPrepared?.Invoke(m_LastRoundResult);
            return m_LastRoundResult;
        }

        public void ResetRound()
        {
            m_HasCompletedRoomScan = false;
            m_LastRoomScanResult = null;
            m_LastRoundResult = null;
            ResetCaptureOutcome();
            ResetRoundState();
            SetPhase(GamePhase.Setup);
        }

        public void PlayAgain()
        {
            if (!m_HasCompletedRoomScan)
            {
                ResetRound();
                BeginRoomScan();
                return;
            }

            m_LastRoundResult = null;
            ResetCaptureOutcome();
            ResetRoundState();
            SetPhase(GamePhase.Investigation);
            m_ScanCompleted.Invoke();
            ScanCompleted?.Invoke();
            ScanCompletedWithResult?.Invoke(m_LastRoomScanResult);
        }

        public void RecordCaptureOutcome(string outcomeName, float progress, float durationSeconds, string reason = null)
        {
            m_LastCaptureOutcome = ParseCaptureOutcome(outcomeName);
            m_LastCaptureProgress = Mathf.Clamp01(progress);
            m_LastCaptureDurationSeconds = Mathf.Max(0f, durationSeconds);
            m_LastCaptureReason = reason ?? string.Empty;
        }

        void ResetRoundState()
        {
            EvidenceRegistry.Instance?.Clear();
            JournalEvidenceSelection.Instance?.Clear();
            IdentificationController.Instance?.ClearSelection();
            GhostCaseController.Instance?.BeginNewCase();
            GhostSpawnController.Instance?.ResetSpawnedGhosts();
        }

        void ResetCaptureOutcome()
        {
            m_LastCaptureOutcome = CaptureOutcome.None;
            m_LastCaptureProgress = 0f;
            m_LastCaptureDurationSeconds = 0f;
            m_LastCaptureReason = string.Empty;
        }

        void ApplyCaptureOutcome(RoundResult result)
        {
            if (result == null)
                return;

            result.captureOutcome = m_LastCaptureOutcome;
            result.captureProgress = m_LastCaptureProgress;
            result.captureDurationSeconds = m_LastCaptureDurationSeconds;
            result.captureReason = m_LastCaptureReason;
        }

        static CaptureOutcome ParseCaptureOutcome(string outcomeName)
        {
            switch (outcomeName)
            {
                case nameof(CaptureOutcome.Success):
                    return CaptureOutcome.Success;
                case nameof(CaptureOutcome.Interrupted):
                    return CaptureOutcome.Interrupted;
                case nameof(CaptureOutcome.Failed):
                    return CaptureOutcome.Failed;
                default:
                    return CaptureOutcome.None;
            }
        }

        void SetPhase(GamePhase nextPhase)
        {
            if (m_CurrentPhase == nextPhase)
                return;

            if (nextPhase == GamePhase.Investigation && !m_HasCompletedRoomScan)
            {
                Debug.LogWarning("Investigation cannot start until the room scan is complete.");
                return;
            }

            m_CurrentPhase = nextPhase;
            NotifyPhaseChanged();
        }

        void NotifyPhaseChanged()
        {
            m_PhaseChanged.Invoke(m_CurrentPhase);
            PhaseChanged?.Invoke(m_CurrentPhase);
        }
    }
}
