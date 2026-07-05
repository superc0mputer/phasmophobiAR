using NUnit.Framework;
using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using PhasmophobiAR.Scanning;
using UnityEngine;

namespace PhasmophobiAR.Tests.EditMode
{
    public sealed class GhostRevealCaptureStateMachineTests
    {
        [Test]
        public void RevealConditionsAreConfigurable()
        {
            var settings = new GhostRevealCaptureSettings
            {
                partialRevealDistanceMeters = 1f,
                partialRevealAngleDegrees = 10f,
                partialRevealHoldSeconds = 0.1f,
                revealDistanceMeters = 1f,
                revealAngleDegrees = 10f,
                revealHoldSeconds = 0.1f,
                captureSeconds = 1f,
                minimumTrackingConfidence = TrackingConfidence.Limited
            };

            var machine = new GhostRevealCaptureStateMachine(settings);
            machine.Reset();

            Assert.AreEqual(GhostRevealState.Hidden, machine.Tick(3f, 45f, TrackingConfidence.Good, 0.2f));

            Assert.AreEqual(GhostRevealState.Hidden, machine.Tick(0.5f, 5f, TrackingConfidence.Good, 0.05f));
            Assert.AreEqual(GhostRevealState.PartialReveal, machine.Tick(0.5f, 5f, TrackingConfidence.Good, 0.05f));
            Assert.AreEqual(GhostRevealState.Revealed, machine.Tick(0.5f, 5f, TrackingConfidence.Good, 0.1f));
        }

        [Test]
        public void CaptureProgressAdvancesAndCompletes()
        {
            var settings = new GhostRevealCaptureSettings
            {
                partialRevealDistanceMeters = 10f,
                partialRevealAngleDegrees = 180f,
                partialRevealHoldSeconds = 0.05f,
                revealDistanceMeters = 10f,
                revealAngleDegrees = 180f,
                revealHoldSeconds = 0.05f,
                captureSeconds = 0.2f,
                minimumTrackingConfidence = TrackingConfidence.Limited
            };

            var machine = new GhostRevealCaptureStateMachine(settings);
            machine.Reset();

            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.05f);
            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.05f);
            Assert.AreEqual(GhostRevealState.Revealed, machine.CurrentState);

            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.1f);
            Assert.AreEqual(GhostRevealState.Capturing, machine.CurrentState);
            Assert.Greater(machine.CaptureProgress, 0f);

            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.2f);
            Assert.AreEqual(GhostRevealState.Captured, machine.CurrentState);
            Assert.AreEqual(1f, machine.CaptureProgress, 0.001f);
        }

        [Test]
        public void CaptureProgressPausesAndResetsWhenConditionsFail()
        {
            var settings = new GhostRevealCaptureSettings
            {
                partialRevealDistanceMeters = 10f,
                partialRevealAngleDegrees = 180f,
                partialRevealHoldSeconds = 0.01f,
                revealDistanceMeters = 10f,
                revealAngleDegrees = 180f,
                revealHoldSeconds = 0.01f,
                captureSeconds = 1f,
                captureZoneDistanceMeters = 2f,
                captureZoneAngleDegrees = 25f,
                captureProgressResetDelaySeconds = 0.25f,
                minimumTrackingConfidence = TrackingConfidence.Limited
            };

            var machine = new GhostRevealCaptureStateMachine(settings);
            machine.Reset();

            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.05f);
            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.05f);
            Assert.AreEqual(GhostRevealState.Revealed, machine.CurrentState);

            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.1f);
            var capturedProgress = machine.CaptureProgress;
            Assert.Greater(capturedProgress, 0f);
            Assert.IsTrue(machine.IsGhostInsideCaptureZone);

            machine.Tick(6f, 90f, TrackingConfidence.Good, 0.1f);
            Assert.IsFalse(machine.IsGhostInsideCaptureZone);
            Assert.AreEqual(capturedProgress, machine.CaptureProgress, 0.0001f);

            machine.Tick(6f, 90f, TrackingConfidence.Good, 0.2f);
            Assert.AreEqual(0f, machine.CaptureProgress, 0.0001f);
        }

        [Test]
        public void UnstableTrackingBlocksCaptureProgress()
        {
            var settings = new GhostRevealCaptureSettings
            {
                partialRevealDistanceMeters = 10f,
                partialRevealAngleDegrees = 180f,
                partialRevealHoldSeconds = 0.01f,
                revealDistanceMeters = 10f,
                revealAngleDegrees = 180f,
                revealHoldSeconds = 0.01f,
                captureSeconds = 1f,
                captureZoneDistanceMeters = 2f,
                captureZoneAngleDegrees = 25f,
                minimumTrackingConfidence = TrackingConfidence.Limited
            };

            var machine = new GhostRevealCaptureStateMachine(settings);
            machine.Reset();

            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.05f);
            machine.Tick(0.5f, 1f, TrackingConfidence.Good, 0.05f);
            Assert.AreEqual(GhostRevealState.Revealed, machine.CurrentState);

            machine.Tick(0.5f, 1f, TrackingConfidence.Poor, 0.25f);
            Assert.AreEqual(0f, machine.CaptureProgress, 0.0001f);
            Assert.AreNotEqual(GhostRevealState.Captured, machine.CurrentState);
        }

        [Test]
        public void GhostVisibilityChangesWithRevealState()
        {
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var behavior = ghost.AddComponent<GhostBehaviorController>();
            behavior.Configure(null, null);

            behavior.SetRevealState(GhostRevealState.Hidden, 0f);
            Assert.IsFalse(behavior.IsVisible);

            behavior.SetRevealState(GhostRevealState.PartialReveal, 0.2f);
            Assert.IsTrue(behavior.IsVisible);
            Assert.AreEqual(GhostRevealState.PartialReveal, behavior.RevealState);

            behavior.SetRevealState(GhostRevealState.Captured, 1f);
            Assert.AreEqual(1f, behavior.CaptureProgress, 0.001f);

            Object.DestroyImmediate(ghost);
        }
    }
}