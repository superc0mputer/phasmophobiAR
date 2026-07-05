using PhasmophobiAR.Scanning;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    public sealed class GhostRevealCaptureStateMachine
    {
        readonly GhostRevealCaptureSettings m_Settings;

        float m_PartialRevealTime;
        float m_RevealTime;
        float m_CaptureProgress;
        float m_FailedCaptureTime;

        public GhostRevealState CurrentState { get; private set; } = GhostRevealState.Hidden;
        public bool IsGhostInsideCaptureZone { get; private set; }
        public float CaptureProgress => m_CaptureProgress;

        public GhostRevealCaptureStateMachine(GhostRevealCaptureSettings settings = null)
        {
            m_Settings = settings != null ? settings.Copy() : new GhostRevealCaptureSettings();
        }

        public void Reset()
        {
            m_PartialRevealTime = 0f;
            m_RevealTime = 0f;
            m_CaptureProgress = 0f;
            m_FailedCaptureTime = 0f;
            IsGhostInsideCaptureZone = false;
            CurrentState = GhostRevealState.Hidden;
        }

        public GhostRevealState Tick(float distanceMeters, float viewAngleDegrees, TrackingConfidence trackingConfidence, float deltaTime, bool canCapture = true)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            var hasStableTracking = trackingConfidence >= m_Settings.minimumTrackingConfidence;
            var partialScore = EvaluateVisibility(distanceMeters, viewAngleDegrees, m_Settings.partialRevealDistanceMeters, m_Settings.partialRevealAngleDegrees);
            var revealScore = EvaluateVisibility(distanceMeters, viewAngleDegrees, m_Settings.revealDistanceMeters, m_Settings.revealAngleDegrees);
            IsGhostInsideCaptureZone = EvaluateCaptureZone(distanceMeters, viewAngleDegrees);

            UpdateRevealTimers(hasStableTracking, partialScore, revealScore, deltaTime);
            UpdateState(hasStableTracking, partialScore, revealScore, deltaTime, canCapture);

            return CurrentState;
        }

        void UpdateRevealTimers(bool hasStableTracking, float partialScore, float revealScore, float deltaTime)
        {
            if (hasStableTracking && partialScore > 0f)
                m_PartialRevealTime += deltaTime;
            else
                m_PartialRevealTime = Mathf.Max(0f, m_PartialRevealTime - deltaTime);

            if (hasStableTracking && revealScore > 0f)
                m_RevealTime += deltaTime;
            else
                m_RevealTime = Mathf.Max(0f, m_RevealTime - deltaTime);
        }

        void UpdateState(bool hasStableTracking, float partialScore, float revealScore, float deltaTime, bool canCapture)
        {
            if (CurrentState == GhostRevealState.Captured)
            {
                m_CaptureProgress = 1f;
                return;
            }

            if (CurrentState == GhostRevealState.Hidden && m_PartialRevealTime >= m_Settings.partialRevealHoldSeconds)
                CurrentState = GhostRevealState.PartialReveal;

            if ((CurrentState == GhostRevealState.Hidden || CurrentState == GhostRevealState.PartialReveal) && m_RevealTime >= m_Settings.revealHoldSeconds)
                CurrentState = GhostRevealState.Revealed;

            if ((CurrentState == GhostRevealState.Revealed || CurrentState == GhostRevealState.Capturing) && canCapture)
                UpdateCaptureState(hasStableTracking, partialScore, revealScore, deltaTime);
            else if (CurrentState == GhostRevealState.Capturing)
                CurrentState = GhostRevealState.Revealed;

            if (CurrentState == GhostRevealState.PartialReveal && partialScore <= 0f && m_PartialRevealTime <= 0f)
                CurrentState = GhostRevealState.Hidden;
        }

        void UpdateCaptureState(bool hasStableTracking, float partialScore, float revealScore, float deltaTime)
        {
            if (hasStableTracking && IsGhostInsideCaptureZone)
            {
                CurrentState = GhostRevealState.Capturing;
                m_FailedCaptureTime = 0f;

                var captureSeconds = Mathf.Max(0.1f, m_Settings.captureSeconds);
                var captureDelta = deltaTime / captureSeconds;
                captureDelta *= Mathf.Lerp(0.85f, 1.15f, Mathf.Clamp01(revealScore + m_Settings.captureProgressBonusAtCenter));
                m_CaptureProgress = Mathf.Clamp01(m_CaptureProgress + captureDelta);

                if (m_CaptureProgress >= 1f)
                    CurrentState = GhostRevealState.Captured;

                return;
            }

            m_FailedCaptureTime += deltaTime;
            if (m_FailedCaptureTime >= Mathf.Max(0f, m_Settings.captureProgressResetDelaySeconds))
            {
                m_CaptureProgress = 0f;
                m_FailedCaptureTime = 0f;
            }

            CurrentState = revealScore > 0f ? GhostRevealState.Revealed : partialScore > 0f ? GhostRevealState.PartialReveal : GhostRevealState.Hidden;
        }

        static float EvaluateVisibility(float distanceMeters, float angleDegrees, float maxDistanceMeters, float maxAngleDegrees)
        {
            var distanceScore = Mathf.Clamp01(1f - distanceMeters / Mathf.Max(0.01f, maxDistanceMeters));
            var angleScore = Mathf.Clamp01(1f - angleDegrees / Mathf.Max(0.01f, maxAngleDegrees));
            return distanceScore * angleScore;
        }

        bool EvaluateCaptureZone(float distanceMeters, float viewAngleDegrees)
        {
            return distanceMeters <= Mathf.Max(0.01f, m_Settings.captureZoneDistanceMeters)
                && viewAngleDegrees <= Mathf.Max(0.01f, m_Settings.captureZoneAngleDegrees);
        }
    }
}
