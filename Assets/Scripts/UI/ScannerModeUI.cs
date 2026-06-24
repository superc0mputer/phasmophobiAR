using System;
using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public class ScannerModeUI : MonoBehaviour
    {
        [SerializeField]
        ScannerModeManager m_ScannerModeManager;

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        Button m_SwitchModeButton;

        [SerializeField]
        TMP_Text m_CurrentModeText;

        public void Configure(ScannerModeManager scannerModeManager, GameStateManager gameStateManager, Button switchModeButton, TMP_Text currentModeText)
        {
            UnsubscribeEvents();

            m_ScannerModeManager = scannerModeManager;
            m_GameStateManager = gameStateManager;
            m_SwitchModeButton = switchModeButton;
            m_CurrentModeText = currentModeText;

            SubscribeEvents();
            UpdateUI();
        }

        void OnEnable()
        {
            SubscribeEvents();
            UpdateUI();
        }

        void OnDisable()
        {
            UnsubscribeEvents();
        }

        void Start()
        {
            // Auto-find managers if not configured
            bool changed = false;
            if (m_ScannerModeManager == null)
            {
                m_ScannerModeManager = ScannerModeManager.Instance;
                changed = true;
            }

            if (m_GameStateManager == null)
            {
                m_GameStateManager = GameStateManager.Instance;
                changed = true;
            }

            if (changed)
                SubscribeEvents();

            UpdateUI();
        }

        void SubscribeEvents()
        {
            if (m_ScannerModeManager != null)
            {
                m_ScannerModeManager.ModeChanged -= OnModeChanged; // ensure no duplicates
                m_ScannerModeManager.ModeChanged += OnModeChanged;
            }

            if (m_GameStateManager != null)
            {
                m_GameStateManager.PhaseChanged -= OnGamePhaseChanged; // ensure no duplicates
                m_GameStateManager.PhaseChanged += OnGamePhaseChanged;
            }

            if (m_SwitchModeButton != null)
            {
                m_SwitchModeButton.onClick.RemoveListener(OnSwitchModeClicked);
                m_SwitchModeButton.onClick.AddListener(OnSwitchModeClicked);
            }
        }

        void UnsubscribeEvents()
        {
            if (m_ScannerModeManager != null)
                m_ScannerModeManager.ModeChanged -= OnModeChanged;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnGamePhaseChanged;

            if (m_SwitchModeButton != null)
                m_SwitchModeButton.onClick.RemoveListener(OnSwitchModeClicked);
        }

        void OnModeChanged(ScannerMode newMode)
        {
            UpdateUI();
        }
        
        void OnGamePhaseChanged(GamePhase phase)
        {
            UpdateUI();
        }

        public void OnSwitchModeClicked()
        {
            if (m_ScannerModeManager != null)
            {
                m_ScannerModeManager.CycleNextMode();
            }
        }

        void UpdateUI()
        {
            bool isInvestigation = m_GameStateManager != null && m_GameStateManager.CurrentPhase == GamePhase.Investigation;

            if (m_SwitchModeButton != null)
                m_SwitchModeButton.interactable = isInvestigation;

            if (m_CurrentModeText != null && m_ScannerModeManager != null)
            {
                m_CurrentModeText.text = isInvestigation
                    ? m_ScannerModeManager.CurrentMode.ToString() + " Mode"
                    : m_ScannerModeManager.CurrentMode.ToString() + " Mode (Locked)";
            }
        }
    }
}