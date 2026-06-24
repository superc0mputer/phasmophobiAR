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
                EvidenceType.EMFSpike,
                EvidenceType.SpectralTrace),
            new GhostProfile(
                GhostType.ShyGhost,
                "Shy Ghost",
                "A quiet haunting that chills the room and only briefly leaves spectral traces.",
                EvidenceType.FreezingTemperature,
                EvidenceType.SpectralTrace),
            new GhostProfile(
                GhostType.FastGhost,
                "Fast Ghost",
                "A volatile entity that produces sharp EMF activity and sudden cold spots.",
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

            return s_Profiles.Length > 0 ? s_Profiles[0] : null;
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
