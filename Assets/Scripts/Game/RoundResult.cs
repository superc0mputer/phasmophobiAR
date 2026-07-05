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
        public CaptureOutcome captureOutcome;
        public float captureProgress;
        public float captureDurationSeconds;
        public string captureReason;

        public bool captureSucceeded => captureOutcome == CaptureOutcome.Success;
        public bool captureInterrupted => captureOutcome == CaptureOutcome.Interrupted || captureOutcome == CaptureOutcome.Failed;

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
            captureOutcome = CaptureOutcome.None;
            captureProgress = 0f;
            captureDurationSeconds = 0f;
            captureReason = string.Empty;
        }
    }
}
