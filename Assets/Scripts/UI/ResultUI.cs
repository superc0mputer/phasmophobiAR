using System.Text;
using PhasmophobiAR.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class ResultUI : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GameObject m_ResultRoot;

        [SerializeField]
        TMP_Text m_ResultText;

        [SerializeField]
        Button m_ResetButton;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;
        }

        void OnEnable()
        {
            if (m_GameStateManager != null)
            {
                m_GameStateManager.PhaseChanged += OnPhaseChanged;
                m_GameStateManager.ResultPrepared += OnResultPrepared;
            }

            if (m_ResetButton != null)
            {
                m_ResetButton.onClick.RemoveListener(ResetRound);
                m_ResetButton.onClick.AddListener(ResetRound);
            }
        }

        void OnDisable()
        {
            if (m_GameStateManager != null)
            {
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
                m_GameStateManager.ResultPrepared -= OnResultPrepared;
            }

            if (m_ResetButton != null)
                m_ResetButton.onClick.RemoveListener(ResetRound);
        }

        public void Configure(GameStateManager gameStateManager, GameObject resultRoot, TMP_Text resultText, Button resetButton)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_ResultRoot = resultRoot ?? m_ResultRoot;
            m_ResultText = resultText ?? m_ResultText;
            m_ResetButton = resetButton ?? m_ResetButton;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (m_ResultRoot != null)
                m_ResultRoot.SetActive(phase == GamePhase.Result);
        }

        void OnResultPrepared(RoundResult result)
        {
            if (m_ResultText == null || result == null)
                return;

            var actual = GhostProfileCatalog.GetProfile(result.actualGhostType)?.displayName ?? result.actualGhostType.ToString();
            var selected = result.hasSelection
                ? GhostProfileCatalog.GetProfile(result.selectedGhostType)?.displayName ?? result.selectedGhostType.ToString()
                : "None";

            var builder = new StringBuilder();
            builder.AppendLine(result.isCorrect ? "Identification correct" : "Identification incorrect");
            builder.AppendLine($"Ghost: {actual}");
            builder.AppendLine($"Selected: {selected}");
            builder.Append("Evidence: ");
            builder.Append(FormatEvidence(result.recordedEvidence));
            m_ResultText.text = builder.ToString();
        }

        void ResetRound()
        {
            m_GameStateManager?.ResetRound();
        }

        static string FormatEvidence(EvidenceType[] evidence)
        {
            if (evidence == null || evidence.Length == 0)
                return "None";

            var builder = new StringBuilder();
            for (var i = 0; i < evidence.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(evidence[i]);
            }

            return builder.ToString();
        }
    }
}
