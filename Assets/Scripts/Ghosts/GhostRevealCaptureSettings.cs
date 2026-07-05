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
        public float revealDistanceMeters = 1.6f;
        public float revealAngleDegrees = 14f;
        public float partialRevealHoldSeconds = 0.35f;
        public float revealHoldSeconds = 1f;

        [Header("Capture")]
        public float captureSeconds = 3f;
        public TrackingConfidence minimumTrackingConfidence = TrackingConfidence.Limited;
        public float captureZoneDistanceMeters = 1.8f;
        public float captureZoneAngleDegrees = 12f;
        public float captureProgressResetDelaySeconds = 0.75f;
        public float captureProgressBonusAtCenter = 0.25f;

        public GhostRevealCaptureSettings Copy()
        {
            return (GhostRevealCaptureSettings)MemberwiseClone();
        }
    }
}