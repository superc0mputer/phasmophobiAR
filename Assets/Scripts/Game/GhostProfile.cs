using System;

namespace PhasmophobiAR.Game
{
    [Serializable]
    public sealed class GhostProfile
    {
        public GhostType ghostType;
        public string displayName;
        public string description;
        public EvidenceType[] requiredEvidence;

        public GhostProfile(GhostType ghostType, string displayName, string description, params EvidenceType[] requiredEvidence)
        {
            this.ghostType = ghostType;
            this.displayName = displayName;
            this.description = description;
            this.requiredEvidence = requiredEvidence ?? Array.Empty<EvidenceType>();
        }
    }
}
