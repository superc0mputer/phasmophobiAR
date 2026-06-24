using System;
using UnityEngine;

namespace PhasmophobiAR.Game
{
    public sealed class GhostCaseController : MonoBehaviour
    {
        public static GhostCaseController Instance { get; private set; }

        [SerializeField]
        bool m_RandomizeGhostType = true;

        [SerializeField]
        GhostType m_ForcedGhostType = GhostType.Wanderer;

        [SerializeField]
        GhostType m_CurrentGhostType = GhostType.Wanderer;

        bool m_HasCase;
        System.Random m_Random;

        public event Action<GhostType> CurrentGhostChanged;

        public GhostType CurrentGhostType
        {
            get
            {
                EnsureCase();
                return m_CurrentGhostType;
            }
        }

        public GhostProfile CurrentProfile => GhostProfileCatalog.GetProfile(CurrentGhostType);
        public bool HasCase => m_HasCase;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(GhostCaseController)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;
            m_Random = new System.Random();
        }

        void Start()
        {
            EnsureCase();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginNewCase()
        {
            m_CurrentGhostType = m_RandomizeGhostType
                ? GhostProfileCatalog.GetRandomGhostType(m_Random)
                : m_ForcedGhostType;
            m_HasCase = true;
            Debug.Log($"New ghost case started: {GhostProfileCatalog.GetProfile(m_CurrentGhostType)?.displayName ?? m_CurrentGhostType.ToString()}");
            CurrentGhostChanged?.Invoke(m_CurrentGhostType);
        }

        public void ForceGhostType(GhostType ghostType)
        {
            m_CurrentGhostType = ghostType;
            m_HasCase = true;
            CurrentGhostChanged?.Invoke(m_CurrentGhostType);
        }

        public void EnsureCase()
        {
            if (!m_HasCase)
                BeginNewCase();
        }
    }
}
