using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class GhostSpawnDebugUI : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        Button m_ToggleButton;

        [SerializeField]
        GameObject m_DebugPanel;

        [SerializeField]
        TMP_Text m_DebugText;

        bool m_IsVisible;

        public void Configure(GameStateManager gameStateManager, Button toggleButton, GameObject debugPanel, TMP_Text debugText)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_ToggleButton = toggleButton ?? m_ToggleButton;
            m_DebugPanel = debugPanel ?? m_DebugPanel;
            m_DebugText = debugText ?? m_DebugText;

            if (isActiveAndEnabled)
                Subscribe();

            SetVisible(false);
        }

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;
        }

        void OnEnable()
        {
            Subscribe();
            SetVisible(false);
        }

        void OnDisable()
        {
            if (m_ToggleButton != null)
                m_ToggleButton.onClick.RemoveListener(Toggle);
        }

        void Update()
        {
            if (!m_IsVisible || m_DebugText == null)
                return;

            m_DebugText.text = GhostSpawnController.LastSpawnDiagnostics;
        }

        void Subscribe()
        {
            if (m_ToggleButton == null)
                return;

            m_ToggleButton.onClick.RemoveListener(Toggle);
            m_ToggleButton.onClick.AddListener(Toggle);
        }

        void Toggle()
        {
            SetVisible(!m_IsVisible);
        }

        void SetVisible(bool visible)
        {
            m_IsVisible = visible && m_GameStateManager != null && m_GameStateManager.CurrentPhase == GamePhase.Investigation;

            if (m_DebugText != null)
            {
                if (m_IsVisible)
                    m_DebugText.text = GhostSpawnController.LastSpawnDiagnostics;
            }

            if (m_DebugPanel != null)
                m_DebugPanel.SetActive(m_IsVisible);
        }
    }
}
