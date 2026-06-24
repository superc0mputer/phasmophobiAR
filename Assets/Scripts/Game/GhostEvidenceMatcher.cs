using System.Collections.Generic;

namespace PhasmophobiAR.Game
{
    public static class GhostEvidenceMatcher
    {
        public static GhostMatchResult Match(IEnumerable<EvidenceType> evidence)
        {
            var recorded = new HashSet<EvidenceType>();
            if (evidence != null)
            {
                foreach (var evidenceType in evidence)
                    recorded.Add(evidenceType);
            }

            var matches = new List<GhostProfile>();
            foreach (var profile in GhostProfileCatalog.Profiles)
            {
                if (profile == null || profile.requiredEvidence == null)
                    continue;

                if (RecordedEvidenceFitsProfile(recorded, profile.requiredEvidence))
                    matches.Add(profile);
            }

            var snapshot = new EvidenceType[recorded.Count];
            recorded.CopyTo(snapshot);
            return new GhostMatchResult(matches.ToArray(), snapshot, recorded.Count > 0 && matches.Count == 0);
        }

        static bool RecordedEvidenceFitsProfile(HashSet<EvidenceType> recorded, EvidenceType[] requiredEvidence)
        {
            foreach (var evidenceType in recorded)
            {
                if (!Contains(requiredEvidence, evidenceType))
                    return false;
            }

            return true;
        }

        static bool Contains(EvidenceType[] evidenceTypes, EvidenceType evidenceType)
        {
            if (evidenceTypes == null)
                return false;

            foreach (var candidate in evidenceTypes)
            {
                if (candidate == evidenceType)
                    return true;
            }

            return false;
        }
    }
}
