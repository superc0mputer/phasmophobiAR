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
                captureProgressDecayPerSecond = 0f,
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