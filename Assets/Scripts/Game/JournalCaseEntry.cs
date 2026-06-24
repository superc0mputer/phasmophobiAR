using System;

namespace PhasmophobiAR.Game
{
    [Serializable]
    public sealed class JournalCaseEntry
    {
        public string createdAtUtc;
        public GhostType actualGhostType;
        public GhostType selectedGhostType;
        public bool hasSelection;
        public bool isCorrect;
        public EvidenceType[] recordedEvidence;

        public JournalCaseEntry(RoundResult result)
        {
            createdAtUtc = DateTime.UtcNow.ToString("o");
            actualGhostType = result != null ? result.actualGhostType : default;
            selectedGhostType = result != null ? result.selectedGhostType : default;
            hasSelection = result != null && result.hasSelection;
            isCorrect = result != null && result.isCorrect;
            recordedEvidence = result != null ? result.recordedEvidence : Array.Empty<EvidenceType>();
        }
    }
}
