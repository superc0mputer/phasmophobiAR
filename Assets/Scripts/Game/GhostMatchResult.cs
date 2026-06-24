using System;

namespace PhasmophobiAR.Game
{
    [Serializable]
    public sealed class GhostMatchResult
    {
        public GhostProfile[] possibleMatches;
        public EvidenceType[] recordedEvidence;
        public bool hasMismatchedEvidence;

        public GhostMatchResult(GhostProfile[] possibleMatches, EvidenceType[] recordedEvidence, bool hasMismatchedEvidence)
        {
            this.possibleMatches = possibleMatches ?? Array.Empty<GhostProfile>();
            this.recordedEvidence = recordedEvidence ?? Array.Empty<EvidenceType>();
            this.hasMismatchedEvidence = hasMismatchedEvidence;
        }
    }
}
