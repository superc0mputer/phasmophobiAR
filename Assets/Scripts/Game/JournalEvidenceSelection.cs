using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhasmophobiAR.Game
{
    public sealed class JournalEvidenceSelection : MonoBehaviour
    {
        public static JournalEvidenceSelection Instance { get; private set; }

        readonly HashSet<EvidenceType> m_SelectedEvidence = new HashSet<EvidenceType>();

        public event Action SelectionChanged;

        public IReadOnlyCollection<EvidenceType> SelectedEvidence => m_SelectedEvidence;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(JournalEvidenceSelection)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsSelected(EvidenceType evidenceType)
        {
            return m_SelectedEvidence.Contains(evidenceType);
        }

        public void Toggle(EvidenceType evidenceType)
        {
            if (!m_SelectedEvidence.Add(evidenceType))
                m_SelectedEvidence.Remove(evidenceType);

            SelectionChanged?.Invoke();
        }

        public EvidenceType[] GetSelectedEvidenceSnapshot()
        {
            var snapshot = new EvidenceType[m_SelectedEvidence.Count];
            m_SelectedEvidence.CopyTo(snapshot);
            return snapshot;
        }

        public void Clear()
        {
            if (m_SelectedEvidence.Count == 0)
                return;

            m_SelectedEvidence.Clear();
            SelectionChanged?.Invoke();
        }
    }
}
