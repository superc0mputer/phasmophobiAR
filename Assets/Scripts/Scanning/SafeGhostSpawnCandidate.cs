using System;
using UnityEngine;

namespace PhasmophobiAR.Scanning
{
    [Serializable]
    public struct SafeGhostSpawnCandidate
    {
        public Vector3 position;
        public float score;
        public string reason;

        public SafeGhostSpawnCandidate(Vector3 position, float score, string reason)
        {
            this.position = position;
            this.score = score;
            this.reason = reason;
        }
    }
}
