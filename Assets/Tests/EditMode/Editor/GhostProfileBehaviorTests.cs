using NUnit.Framework;
using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using PhasmophobiAR.Scanning;
using UnityEngine;

namespace PhasmophobiAR.Tests.EditMode
{
    public sealed class GhostProfileBehaviorTests
    {
        [Test]
        public void MvpGhostProfilesExistWithEvidence()
        {
            AssertProfileIsValid(GhostType.Wanderer);
            AssertProfileIsValid(GhostType.ShyGhost);
            AssertProfileIsValid(GhostType.StaticGhost);
        }

        [Test]
        public void MvpGhostProfilesHaveDistinctBehaviorTuning()
        {
            var wanderer = GhostProfileCatalog.GetProfile(GhostType.Wanderer);
            var shyGhost = GhostProfileCatalog.GetProfile(GhostType.ShyGhost);
            var staticGhost = GhostProfileCatalog.GetProfile(GhostType.StaticGhost);

            Assert.Greater(wanderer.movementRadiusMeters, shyGhost.movementRadiusMeters);
            Assert.Greater(shyGhost.gazeHideThresholdSeconds, 0f);
            Assert.Greater(staticGhost.emfSignalMultiplier, wanderer.emfSignalMultiplier);
            Assert.Greater(staticGhost.spectralTraceMultiplier, shyGhost.spectralTraceMultiplier);
        }

        [Test]
        public void ExtendableProfilesRemainValid()
        {
            AssertProfileIsValid(GhostType.Mimic);
            AssertProfileIsValid(GhostType.FastGhost);
        }

        [Test]
        public void EmfSignalUsesProfileMultiplier()
        {
            var neutral = CreateGhost("neutral", null, new Vector3(0f, 0f, 1f));
            var boosted = CreateGhost("boosted", GhostProfileCatalog.GetProfile(GhostType.StaticGhost), new Vector3(0f, 0f, 1f));
            var settings = new EMFSignalSettings
            {
                DirectionWeight = 0f,
                MaxDistance = 4f
            };

            var neutralSignal = EMFSignalCalculator.Calculate(Vector3.zero, Vector3.forward, new[] { neutral.transform }, settings, false);
            var boostedSignal = EMFSignalCalculator.Calculate(Vector3.zero, Vector3.forward, new[] { boosted.transform }, settings, false);

            Object.DestroyImmediate(neutral);
            Object.DestroyImmediate(boosted);

            Assert.Greater(boostedSignal, neutralSignal);
        }

        [Test]
        public void BehaviorControllerExposesProfileMultipliersAndDifficulty()
        {
            var ghost = CreateGhost("shy", GhostProfileCatalog.GetProfile(GhostType.ShyGhost), Vector3.zero);
            var behavior = ghost.GetComponent<GhostBehaviorController>();

            Assert.AreEqual(GhostType.ShyGhost, behavior.GhostType);
            Assert.Greater(behavior.TemperatureInfluenceMultiplier, 1f);
            Assert.Less(behavior.SpectralTraceMultiplier, 1f);
            Assert.Greater(behavior.RevealDifficulty, 0.5f);
            Assert.Greater(behavior.CaptureDifficulty, 0.5f);

            Object.DestroyImmediate(ghost);
        }

        static void AssertProfileIsValid(GhostType ghostType)
        {
            var profile = GhostProfileCatalog.GetProfile(ghostType);
            Assert.NotNull(profile);
            Assert.IsNotEmpty(profile.displayName);
            Assert.IsNotEmpty(profile.description);
            Assert.IsNotEmpty(profile.behaviorSummary);
            Assert.NotNull(profile.requiredEvidence);
            Assert.Greater(profile.requiredEvidence.Length, 0);
            Assert.Greater(profile.emfSignalMultiplier, 0f);
            Assert.Greater(profile.temperatureInfluenceMultiplier, 0f);
            Assert.Greater(profile.spectralTraceMultiplier, 0f);
        }

        static GameObject CreateGhost(string name, GhostProfile profile, Vector3 position)
        {
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ghost.name = name;
            ghost.transform.position = position;
            var behavior = ghost.AddComponent<GhostBehaviorController>();
            behavior.Configure(profile, null);
            return ghost;
        }
    }
}
