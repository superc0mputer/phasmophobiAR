using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhasmophobiAR.Game
{
    public sealed class EvidenceRegistry : MonoBehaviour
    {
        public static EvidenceRegistry Instance { get; private set; }

        readonly HashSet<EvidenceType> m_RecordedEvidence = new HashSet<EvidenceType>();

        public event Action<EvidenceType> EvidenceRecorded;
        public event Action EvidenceChanged;
        public event Action EvidenceCleared;

        public IReadOnlyCollection<EvidenceType> RecordedEvidence => m_RecordedEvidence;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(EvidenceRegistry)} found on {name}; disabling this instance.");
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

        public bool HasEvidence(EvidenceType evidenceType)
        {
            return m_RecordedEvidence.Contains(evidenceType);
        }

        public bool RecordEvidence(EvidenceType evidenceType)
        {
            if (!m_RecordedEvidence.Add(evidenceType))
                return false;

            Debug.Log($"Evidence recorded: {evidenceType}");
            EvidenceRecorded?.Invoke(evidenceType);
            EvidenceChanged?.Invoke();
            return true;
        }

        public EvidenceType[] GetRecordedEvidenceSnapshot()
        {
            var snapshot = new EvidenceType[m_RecordedEvidence.Count];
            m_RecordedEvidence.CopyTo(snapshot);
            return snapshot;
        }

        public void Clear()
        {
            if (m_RecordedEvidence.Count == 0)
                return;

            m_RecordedEvidence.Clear();
            EvidenceCleared?.Invoke();
            EvidenceChanged?.Invoke();
        }
    }
}
