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
        public CaptureOutcome captureOutcome;
        public float captureProgress;
        public float captureDurationSeconds;
        public string captureReason;

        public JournalCaseEntry(RoundResult result)
        {
            createdAtUtc = DateTime.UtcNow.ToString("o");
            actualGhostType = result != null ? result.actualGhostType : default;
            selectedGhostType = result != null ? result.selectedGhostType : default;
            hasSelection = result != null && result.hasSelection;
            isCorrect = result != null && result.isCorrect;
            recordedEvidence = result != null ? result.recordedEvidence : Array.Empty<EvidenceType>();
            captureOutcome = result != null ? result.captureOutcome : CaptureOutcome.None;
            captureProgress = result != null ? result.captureProgress : 0f;
            captureDurationSeconds = result != null ? result.captureDurationSeconds : 0f;
            captureReason = result != null ? result.captureReason : string.Empty;
        }
    }
}
