using System;
using System.Collections.Generic;

namespace PhasmophobiAR.Game
{
    public static class GhostProfileCatalog
    {
        static readonly GhostProfile[] s_Profiles =
        {
            new GhostProfile(
                GhostType.Wanderer,
                "Wanderer",
                "A roaming presence that leaves clear EMF spikes and spectral traces.",
                "Moves slowly around its anchor and is usually willing to reveal itself.",
                0.18f,
                0.65f,
                0f,
                0f,
                1.1f,
                0.8f,
                1.0f,
                0.45f,
                0.55f,
                EvidenceType.EMFSpike,
                EvidenceType.SpectralTrace),
            new GhostProfile(
                GhostType.ShyGhost,
                "Shy Ghost",
                "A quiet haunting that chills the room and answers through the spirit box.",
                "Moves very little and hides when watched for too long.",
                0.05f,
                0.2f,
                1.35f,
                3.0f,
                0.75f,
                1.35f,
                0.65f,
                0.8f,
                0.75f,
                EvidenceType.FreezingTemperature,
                EvidenceType.SpiritResponse),
            new GhostProfile(
                GhostType.StaticGhost,
                "Static Ghost",
                "A broken signal that appears through EMF surges and visual spectral noise.",
                "Barely moves, but jitters with visible static and stronger scanner distortion.",
                0.02f,
                0.08f,
                0f,
                0f,
                1.45f,
                0.7f,
                1.55f,
                0.35f,
                0.65f,
                EvidenceType.EMFSpike,
                EvidenceType.SpectralTrace),
            new GhostProfile(
                GhostType.Mimic,
                "Mimic",
                "A deceptive presence that answers back and leaves traces resembling other ghosts.",
                "Uses neutral MVP behavior while its deceptive profile remains data-driven.",
                0.1f,
                0.35f,
                0f,
                0f,
                1.0f,
                1.0f,
                1.0f,
                0.65f,
                0.7f,
                EvidenceType.SpiritResponse,
                EvidenceType.SpectralTrace),
            new GhostProfile(
                GhostType.FastGhost,
                "Fast Ghost",
                "A volatile entity that produces sharp EMF activity and sudden cold spots.",
                "Uses neutral MVP behavior with higher future movement and capture tuning.",
                0.25f,
                0.8f,
                0f,
                0f,
                1.2f,
                1.15f,
                0.9f,
                0.7f,
                0.85f,
                EvidenceType.EMFSpike,
                EvidenceType.FreezingTemperature)
        };

        public static IReadOnlyList<GhostProfile> Profiles => s_Profiles;

        public static GhostProfile GetProfile(GhostType ghostType)
        {
            foreach (var profile in s_Profiles)
            {
                if (profile.ghostType == ghostType)
                    return profile;
            }

            return null;
        }

        public static GhostType GetRandomGhostType(Random random)
        {
            if (s_Profiles.Length == 0)
                return default;

            if (random == null)
                random = new Random();

            return s_Profiles[random.Next(0, s_Profiles.Length)].ghostType;
        }
    }
}
