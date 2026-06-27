using System;

namespace PhasmophobiAR.Game
{
    [Serializable]
    public sealed class GhostProfile
    {
        public GhostType ghostType;
        public string displayName;
        public string description;
        public string behaviorSummary;
        public EvidenceType[] requiredEvidence;
        public float movementSpeedMetersPerSecond;
        public float movementRadiusMeters;
        public float gazeHideThresholdSeconds;
        public float hiddenDurationSeconds;
        public float emfSignalMultiplier;
        public float temperatureInfluenceMultiplier;
        public float spectralTraceMultiplier;
        public float revealDifficulty;
        public float captureDifficulty;

        public GhostProfile(
            GhostType ghostType,
            string displayName,
            string description,
            string behaviorSummary,
            float movementSpeedMetersPerSecond,
            float movementRadiusMeters,
            float gazeHideThresholdSeconds,
            float hiddenDurationSeconds,
            float emfSignalMultiplier,
            float temperatureInfluenceMultiplier,
            float spectralTraceMultiplier,
            float revealDifficulty,
            float captureDifficulty,
            params EvidenceType[] requiredEvidence)
        {
            this.ghostType = ghostType;
            this.displayName = displayName;
            this.description = description;
            this.behaviorSummary = behaviorSummary;
            this.movementSpeedMetersPerSecond = movementSpeedMetersPerSecond;
            this.movementRadiusMeters = movementRadiusMeters;
            this.gazeHideThresholdSeconds = gazeHideThresholdSeconds;
            this.hiddenDurationSeconds = hiddenDurationSeconds;
            this.emfSignalMultiplier = emfSignalMultiplier;
            this.temperatureInfluenceMultiplier = temperatureInfluenceMultiplier;
            this.spectralTraceMultiplier = spectralTraceMultiplier;
            this.revealDifficulty = revealDifficulty;
            this.captureDifficulty = captureDifficulty;
            this.requiredEvidence = requiredEvidence ?? Array.Empty<EvidenceType>();
        }
    }
}
