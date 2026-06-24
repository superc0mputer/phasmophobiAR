using System;

namespace PhasmophobiAR.Game
{
    [Serializable]
    public sealed class RoundResult
    {
        public GhostType actualGhostType;
        public GhostType selectedGhostType;
        public bool hasSelection;
        public bool isCorrect;
        public EvidenceType[] recordedEvidence;
        public GhostType[] possibleGhostTypes;

        public RoundResult(
            GhostType actualGhostType,
            GhostType selectedGhostType,
            bool hasSelection,
            bool isCorrect,
            EvidenceType[] recordedEvidence,
            GhostType[] possibleGhostTypes)
        {
            this.actualGhostType = actualGhostType;
            this.selectedGhostType = selectedGhostType;
            this.hasSelection = hasSelection;
            this.isCorrect = isCorrect;
            this.recordedEvidence = recordedEvidence ?? Array.Empty<EvidenceType>();
            this.possibleGhostTypes = possibleGhostTypes ?? Array.Empty<GhostType>();
        }
    }
}
