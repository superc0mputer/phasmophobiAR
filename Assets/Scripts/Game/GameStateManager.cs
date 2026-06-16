using System;
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

        GamePhase m_CurrentPhase;
        bool m_HasCompletedRoomScan;
        RoomScanResult m_LastRoomScanResult;

        public event Action<GamePhase> PhaseChanged;
        public event Action ScanCompleted;
        public event Action<RoomScanResult> ScanCompletedWithResult;

        public GamePhase CurrentPhase => m_CurrentPhase;
        public bool HasCompletedRoomScan => m_HasCompletedRoomScan;
        public RoomScanResult LastRoomScanResult => m_LastRoomScanResult;
        public bool CanPlaceTools => m_CurrentPhase == GamePhase.Investigation;
        public bool CanCaptureGhost => m_CurrentPhase == GamePhase.Investigation;
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
            SetPhase(GamePhase.Result);
        }

        public void ResetRound()
        {
            m_HasCompletedRoomScan = false;
            m_LastRoomScanResult = null;
            SetPhase(GamePhase.Setup);
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
