using PhasmophobiAR.Ghosts;
using UnityEngine;

namespace PhasmophobiAR.Scanning
{
    public static class EMFSignalCalculator
    {
        public static float CalculateFromSpawnedGhosts(
            Vector3 sourcePosition,
            Vector3 sourceForward,
            EMFSignalSettings settings,
            bool useDirectionWeight)
        {
            return Calculate(sourcePosition, sourceForward, GhostSpawnController.GetSpawnedGhosts(), settings, useDirectionWeight);
        }

        public static float Calculate(
            Vector3 sourcePosition,
            Vector3 sourceForward,
            Transform[] ghosts,
            EMFSignalSettings settings,
            bool useDirectionWeight)
        {
            if (ghosts == null || ghosts.Length == 0)
                return 0f;

            if (settings == null)
                settings = new EMFSignalSettings();
            var maxDistance = settings.MaxDistance;
            var directionWeight = useDirectionWeight ? settings.DirectionWeight : 0f;
            var forward = Vector3.ProjectOnPlane(sourceForward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            var best = 0f;
            foreach (var ghost in ghosts)
            {
                if (ghost == null)
                    continue;

                var toGhost = ghost.position - sourcePosition;
                var distance = toGhost.magnitude;
                var distanceFactor = Mathf.Clamp01(1f - distance / maxDistance);

                var directionFactor = 1f;
                if (directionWeight > 0f)
                {
                    var direction = Vector3.ProjectOnPlane(toGhost, Vector3.up);
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        direction.Normalize();
                        directionFactor = Mathf.Clamp01(Vector3.Dot(forward, direction));
                    }
                }

                var strength = distanceFactor * ((1f - directionWeight) + directionWeight * directionFactor);
                if (strength > best)
                    best = strength;
            }

            return best;
        }

        public static int ToEMFLevel(float signal, EMFSignalSettings settings)
        {
            if (settings == null)
                settings = new EMFSignalSettings();
            signal = Mathf.Clamp01(signal);

            if (signal >= settings.Level5Threshold)
                return 5;
            if (signal >= settings.Level4Threshold)
                return 4;
            if (signal >= settings.Level3Threshold)
                return 3;
            if (signal >= settings.Level2Threshold)
                return 2;

            return signal > 0.01f ? 1 : 0;
        }
    }
}
