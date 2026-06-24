using System;
using PhasmophobiAR.Game;
using UnityEngine;
using UnityEngine.Events;

namespace PhasmophobiAR.Scanning
{
    public sealed class ScannerModeManager : MonoBehaviour
    {
        public static ScannerModeManager Instance { get; private set; }

        [SerializeField]
        ScannerMode m_InitialMode = ScannerMode.EMF;

        [SerializeField]
        UnityEvent<ScannerMode> m_ModeChanged = new UnityEvent<ScannerMode>();

        ScannerMode m_CurrentMode;

        public ScannerMode CurrentMode => m_CurrentMode;
        public UnityEvent<ScannerMode> modeChanged => m_ModeChanged;
        public event Action<ScannerMode> ModeChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(ScannerModeManager)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;
            m_CurrentMode = m_InitialMode;
        }

        void Start()
        {
            SetMode(m_CurrentMode);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool TrySwitchMode(ScannerMode newMode)
        {
            if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentPhase != GamePhase.Investigation)
                return false;

            if (m_CurrentMode == newMode)
                return false;

            SetMode(newMode);
            return true;
        }

        public void CycleNextMode()
        {
            if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentPhase != GamePhase.Investigation)
                return;

            // Enums are 0, 1, 2...
            int nextMode = (int)m_CurrentMode + 1;
            if (!Enum.IsDefined(typeof(ScannerMode), nextMode))
            {
                nextMode = 0; // Wrap around to EMF
            }
            
            SetMode((ScannerMode)nextMode);
        }

        void SetMode(ScannerMode mode)
        {
            m_CurrentMode = mode;
            m_ModeChanged.Invoke(m_CurrentMode);
            ModeChanged?.Invoke(m_CurrentMode);
        }
    }
}