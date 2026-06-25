using System;
using UnityEngine;

namespace PhasmophobiAR.Scanning
{
    [Serializable]
    public sealed class EMFSignalSettings
    {
        [SerializeField]
        float m_MaxDistance = 8f;

        [SerializeField]
        float m_DirectionWeight = 0.6f;

        [SerializeField]
        float m_Level2Threshold = 0.18f;

        [SerializeField]
        float m_Level3Threshold = 0.38f;

        [SerializeField]
        float m_Level4Threshold = 0.58f;

        [SerializeField]
        float m_Level5Threshold = 0.78f;

        public float MaxDistance
        {
            get => Mathf.Max(0.01f, m_MaxDistance);
            set => m_MaxDistance = Mathf.Max(0.01f, value);
        }

        public float DirectionWeight
        {
            get => Mathf.Clamp01(m_DirectionWeight);
            set => m_DirectionWeight = Mathf.Clamp01(value);
        }

        public float Level2Threshold => Mathf.Clamp01(m_Level2Threshold);
        public float Level3Threshold => Mathf.Clamp01(m_Level3Threshold);
        public float Level4Threshold => Mathf.Clamp01(m_Level4Threshold);
        public float Level5Threshold => Mathf.Clamp01(m_Level5Threshold);
    }
}
