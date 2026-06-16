using System;
using UnityEngine;

namespace PhasmophobiAR.Markers
{
    [Serializable]
    public sealed class MarkerToolDefinition
    {
        [SerializeField]
        string m_MarkerName;

        [SerializeField]
        string m_DisplayName;

        [SerializeField]
        string m_TextureResourcePath;

        [SerializeField]
        MarkerToolType m_ToolType;

        [SerializeField]
        GameObject m_ToolPrefab;

        [SerializeField]
        float m_PhysicalWidthMeters = 0.12f;

        public string MarkerName => m_MarkerName;
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? m_MarkerName : m_DisplayName;
        public string TextureResourcePath => m_TextureResourcePath;
        public MarkerToolType ToolType => m_ToolType;
        public GameObject ToolPrefab => m_ToolPrefab;
        public float PhysicalWidthMeters => m_PhysicalWidthMeters;

        public MarkerToolDefinition(
            string markerName,
            string displayName,
            string textureResourcePath,
            MarkerToolType toolType,
            GameObject toolPrefab,
            float physicalWidthMeters)
        {
            m_MarkerName = markerName;
            m_DisplayName = displayName;
            m_TextureResourcePath = textureResourcePath;
            m_ToolType = toolType;
            m_ToolPrefab = toolPrefab;
            m_PhysicalWidthMeters = physicalWidthMeters;
        }
    }
}
