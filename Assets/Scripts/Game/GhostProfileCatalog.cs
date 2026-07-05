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
                "A restless presence that never settles in one spot. It circles its anchor in a broad, slow route, repeatedly crossing the same part of the room before drifting away again.",
                "Follow it instead of waiting beside one tool. Its steady movement produces changing EMF strength and spectral traces along its route, and it is generally willing to remain visible once found.",
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
                "A cautious ghost that stays close to its hiding place. It makes only small, infrequent movements and prefers to reveal itself through cold air and distant replies rather than direct appearances.",
                "Do not stare continuously when it begins to manifest. Prolonged attention makes it retreat for several seconds; indirect observation and carefully placed tools give the best chance of locating it.",
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
                "An almost motionless apparition bound tightly to one location. Instead of roaming, its outline jitters and pulses as though the image itself cannot hold a stable shape.",
                "Its fixed position makes it easier to locate, but severe static and spectral distortion obscure the capture window. Search for repeated EMF peaks coming from the same point in the room.",
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
                "A deceptive presence that moves in a modest area and deliberately resembles other hauntings. Its activity can suggest several ghost types before settling into a consistent pattern.",
                "Never trust a single strong reading. Compare spirit responses with repeated spectral traces from different positions; the Mimic is identified by contradictions between tools, not by one dramatic event.",
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
                "The quickest and widest-ranging ghost in the field guide. It sweeps rapidly between positions around its anchor, producing brief appearances and sudden changes in distance.",
                "Keep moving and watch for sharp EMF jumps followed by isolated cold spots. Its reveal window is short and its capture is less forgiving, so center it immediately when it crosses the camera.",
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

        static readonly GhostType[] s_MvpSelectableGhostTypes =
        {
            GhostType.Wanderer,
            GhostType.ShyGhost,
            GhostType.StaticGhost,
            GhostType.Mimic,
            GhostType.FastGhost
        };

        public static IReadOnlyList<GhostProfile> Profiles => s_Profiles;

        public static IReadOnlyList<GhostType> MvpSelectableGhostTypes => s_MvpSelectableGhostTypes;

        public static GhostProfile[] GetMvpSelectableProfiles()
        {
            var profiles = new List<GhostProfile>();
            foreach (var ghostType in s_MvpSelectableGhostTypes)
            {
                var profile = GetProfile(ghostType);
                if (profile != null)
                    profiles.Add(profile);
            }

            return profiles.ToArray();
        }

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
            if (s_MvpSelectableGhostTypes.Length == 0)
                return default;

            if (random == null)
                random = new Random();

            return s_MvpSelectableGhostTypes[random.Next(0, s_MvpSelectableGhostTypes.Length)];
        }
    }
}
