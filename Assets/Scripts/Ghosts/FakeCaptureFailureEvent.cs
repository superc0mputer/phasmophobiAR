using System.Collections;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Briefly fakes signal loss without deleting genuine capture progress.</summary>
    public sealed class FakeCaptureFailureEvent : HorrorEvent
    {
        [SerializeField, Range(0f, 1f)] float m_MinimumCaptureProgress = 0.5f;
        [SerializeField, Range(0f, 1f)] float m_MaximumCaptureProgress = 0.9f;
        [SerializeField] float m_FailureDurationSeconds = 0.65f;
        [SerializeField] AudioClip m_PlaceholderSignalLossSound;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.45f;

        internal override bool CanTrigger(HorrorDirector director)
        {
            if (!base.CanTrigger(director) || director.Tension < 0.42f) return false;
            var capture = director.CaptureController;
            return capture != null
                && !capture.IsFakeCaptureFailureActive
                && capture.CurrentState == GhostRevealState.Capturing
                && capture.CaptureProgress >= m_MinimumCaptureProgress
                && capture.CaptureProgress <= m_MaximumCaptureProgress;
        }

        internal override bool WantsImmediateTrigger(HorrorDirector director) => CanTrigger(director);

        protected override IEnumerator Play(HorrorDirector director)
        {
            var capture = director.CaptureController;
            if (capture == null || !capture.BeginFakeCaptureFailure(m_FailureDurationSeconds)) yield break;

            if (m_PlaceholderSignalLossSound != null && director.ARCamera != null)
                AudioSource.PlayClipAtPoint(m_PlaceholderSignalLossSound, director.ARCamera.position, m_Volume);

            director.AddTension(0.12f);
            while (capture != null && capture.IsFakeCaptureFailureActive)
                yield return null;
        }
    }
}
