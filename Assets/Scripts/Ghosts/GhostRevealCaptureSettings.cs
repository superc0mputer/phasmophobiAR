using System;
using PhasmophobiAR.Scanning;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    [Serializable]
    public sealed class GhostRevealCaptureSettings
    {
        [Header("Visibility")]
        public float partialRevealDistanceMeters = 3f;
        public float partialRevealAngleDegrees = 30f;
        public float revealDistanceMeters = 2.4f;
        public float revealAngleDegrees = 22f;
        public float partialRevealHoldSeconds = 0.35f;
        public float revealHoldSeconds = 1f;

        [Header("Capture")]
        public float captureSeconds = 3f;
        public TrackingConfidence minimumTrackingConfidence = TrackingConfidence.Limited;
        public float captureZoneDistanceMeters = 3f;
        public float captureZoneAngleDegrees = 24f;
        public float captureProgressResetDelaySeconds = 0.75f;
        public float captureProgressBonusAtCenter = 0.25f;

        public GhostRevealCaptureSettings Copy()
        {
            return (GhostRevealCaptureSettings)MemberwiseClone();
        }
    }
}
