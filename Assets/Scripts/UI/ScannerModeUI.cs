using System;
using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using PhasmophobiAR.Tools;
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

        [SerializeField]
        TMP_Text m_ModeReadoutText;

        [SerializeField]
        EMFSignalController m_EMFSignalController;

        public void Configure(ScannerModeManager scannerModeManager, GameStateManager gameStateManager, Button switchModeButton, TMP_Text currentModeText)
        {
            Configure(scannerModeManager, gameStateManager, switchModeButton, currentModeText, null, null);
        }

        public void Configure(
            ScannerModeManager scannerModeManager,
            GameStateManager gameStateManager,
            Button switchModeButton,
            TMP_Text currentModeText,
            TMP_Text modeReadoutText,
            EMFSignalController emfSignalController)
        {
            UnsubscribeEvents();

            m_ScannerModeManager = scannerModeManager;
            m_GameStateManager = gameStateManager;
            m_SwitchModeButton = switchModeButton;
            m_CurrentModeText = currentModeText;
            m_ModeReadoutText = modeReadoutText ?? m_ModeReadoutText;
            m_EMFSignalController = emfSignalController ?? m_EMFSignalController;

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

            if (m_EMFSignalController == null)
                m_EMFSignalController = FindAnyObjectByType<EMFSignalController>();

            if (changed)
                SubscribeEvents();

            UpdateUI();
        }

        void Update()
        {
            UpdateReadout();
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
                    ? GetModeLabel(m_ScannerModeManager.CurrentMode)
                    : "Scanner offline";
            }

            UpdateReadout();
        }

        void UpdateReadout()
        {
            if (m_ModeReadoutText == null)
                return;

            var isInvestigation = m_GameStateManager != null && m_GameStateManager.CurrentPhase == GamePhase.Investigation;
            if (!isInvestigation || m_ScannerModeManager == null)
            {
                m_ModeReadoutText.gameObject.SetActive(false);
                return;
            }

            m_ModeReadoutText.gameObject.SetActive(true);
            switch (m_ScannerModeManager.CurrentMode)
            {
                case ScannerMode.EMF:
                    if (m_EMFSignalController == null)
                        m_EMFSignalController = FindAnyObjectByType<EMFSignalController>();

                    var emfValue = m_EMFSignalController != null ? m_EMFSignalController.CurrentValue * 5f : 0f;
                    m_ModeReadoutText.text = $"EMF {emfValue:0.0}";
                    m_ModeReadoutText.color = SignalColor(emfValue / 5f);
                    break;
                case ScannerMode.Thermal:
                    var thermometer = FindAnyObjectByType<ThermometerTool>();
                    if (thermometer != null)
                    {
                        m_ModeReadoutText.text = $"{thermometer.CurrentCelsius:0.0} C";
                        m_ModeReadoutText.color = TemperatureColor(thermometer.CurrentCelsius);
                    }
                    else
                    {
                        m_ModeReadoutText.text = "--.- C";
                        m_ModeReadoutText.color = new Color(0.45f, 0.9f, 1f, 1f);
                    }
                    break;
                case ScannerMode.Spectral:
                    m_ModeReadoutText.text = "SPECTRAL";
                    m_ModeReadoutText.color = new Color(0.65f, 0.9f, 1f, 1f);
                    break;
                default:
                    m_ModeReadoutText.text = GetModeLabel(m_ScannerModeManager.CurrentMode).ToUpperInvariant();
                    m_ModeReadoutText.color = new Color(0.82f, 0.92f, 0.86f, 1f);
                    break;
            }
        }

        static string GetModeLabel(ScannerMode mode)
        {
            switch (mode)
            {
                case ScannerMode.EMF:
                    return "EMF";
                case ScannerMode.Thermal:
                    return "TEMP";
                case ScannerMode.Spectral:
                    return "Spectral";
                default:
                    return mode.ToString();
            }
        }

        static Color SignalColor(float signal)
        {
            signal = Mathf.Clamp01(signal);
            if (signal < 0.5f)
                return Color.Lerp(new Color(0.24f, 1f, 0.48f), new Color(1f, 0.85f, 0.2f), signal * 2f);

            return Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.16f, 0.08f), (signal - 0.5f) * 2f);
        }

        static Color TemperatureColor(float celsius)
        {
            if (celsius <= 0f)
                return Color.Lerp(new Color(0.25f, 0.45f, 1f), new Color(0.1f, 0.95f, 1f), Mathf.InverseLerp(-10f, 0f, celsius));

            if (celsius < 20f)
                return Color.Lerp(new Color(0.1f, 0.95f, 1f), new Color(0.2f, 1f, 0.35f), Mathf.InverseLerp(0f, 20f, celsius));

            if (celsius < 35f)
                return Color.Lerp(new Color(0.2f, 1f, 0.35f), new Color(1f, 0.85f, 0.05f), Mathf.InverseLerp(20f, 35f, celsius));

            return Color.Lerp(new Color(1f, 0.85f, 0.05f), new Color(1f, 0.12f, 0.03f), Mathf.InverseLerp(35f, 50f, celsius));
        }
    }
}
