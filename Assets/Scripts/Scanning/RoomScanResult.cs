using System;
using UnityEngine;

namespace PhasmophobiAR.Scanning
{
    [Serializable]
    public sealed class RoomScanResult
    {
        public float progress;
        public TrackingConfidence confidence;
        public float elapsedSeconds;
        public float stableTrackingSeconds;
        public float movementMeters;
        public float lookDegrees;
        public int trackedPlaneCount;
        public int floorPlaneCount;
        public int wallPlaneCount;
        public int tablePlaneCount;
        public int featurePointCount;
        public float featurePointDensity;
        public bool hasEstimatedBounds;
        public Bounds estimatedBounds;
        public bool hasLiDARMeshData;
        public int meshCount;
        public int meshVertexCount;
        public bool hasDepthOcclusion;
        public SafeGhostSpawnCandidate[] safeSpawnCandidates = Array.Empty<SafeGhostSpawnCandidate>();
    }
}
