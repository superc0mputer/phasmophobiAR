using System;
using UnityEngine;

namespace PhasmophobiAR.Game
{
    public sealed class IdentificationController : MonoBehaviour
    {
        public static IdentificationController Instance { get; private set; }

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [SerializeField]
        GhostCaseController m_GhostCaseController;

        GhostType m_SelectedGhostType;
        bool m_HasSelection;

        public event Action SelectionChanged;
        public event Action<RoundResult> ResultEvaluated;

        public GhostType SelectedGhostType => m_SelectedGhostType;
        public bool HasSelection => m_HasSelection;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(IdentificationController)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(EvidenceRegistry evidenceRegistry, GhostCaseController ghostCaseController)
        {
            m_EvidenceRegistry = evidenceRegistry ?? m_EvidenceRegistry;
            m_GhostCaseController = ghostCaseController ?? m_GhostCaseController;
        }

        public void SelectGhost(GhostType ghostType)
        {
            m_SelectedGhostType = ghostType;
            m_HasSelection = true;
            SelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            if (!m_HasSelection)
                return;

            m_HasSelection = false;
            SelectionChanged?.Invoke();
        }

        public RoundResult Evaluate()
        {
            ResolveReferences();
            if (m_GhostCaseController != null)
                m_GhostCaseController.EnsureCase();

            var recordedEvidence = m_EvidenceRegistry != null
                ? m_EvidenceRegistry.GetRecordedEvidenceSnapshot()
                : Array.Empty<EvidenceType>();
            var matchResult = GhostEvidenceMatcher.Match(recordedEvidence);
            var possibleGhostTypes = new GhostType[matchResult.possibleMatches.Length];
            for (var i = 0; i < matchResult.possibleMatches.Length; i++)
                possibleGhostTypes[i] = matchResult.possibleMatches[i].ghostType;

            var actualGhostType = m_GhostCaseController != null ? m_GhostCaseController.CurrentGhostType : default;
            var result = new RoundResult(
                actualGhostType,
                m_SelectedGhostType,
                m_HasSelection,
                m_HasSelection && m_SelectedGhostType == actualGhostType,
                recordedEvidence,
                possibleGhostTypes);

            ResultEvaluated?.Invoke(result);
            return result;
        }

        void ResolveReferences()
        {
            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            if (m_GhostCaseController == null)
                m_GhostCaseController = GhostCaseController.Instance;
        }
    }
}
