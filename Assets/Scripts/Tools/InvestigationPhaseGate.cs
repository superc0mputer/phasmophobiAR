using PhasmophobiAR.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.Tools
{
    public sealed class InvestigationPhaseGate : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GameObject[] m_GameObjectsToEnable;

        [SerializeField]
        Behaviour[] m_BehavioursToEnable;

        [SerializeField]
        Collider[] m_CollidersToEnable;

        [SerializeField]
        Button[] m_ButtonsToEnable;

        bool m_IsSubscribed;

        public void Configure(
            GameStateManager gameStateManager,
            GameObject[] gameObjectsToEnable,
            Behaviour[] behavioursToEnable,
            Collider[] collidersToEnable,
            Button[] buttonsToEnable)
        {
            m_GameStateManager = gameStateManager;
            m_GameObjectsToEnable = gameObjectsToEnable;
            m_BehavioursToEnable = behavioursToEnable;
            m_CollidersToEnable = collidersToEnable;
            m_ButtonsToEnable = buttonsToEnable;

            if (isActiveAndEnabled)
                Subscribe();
        }

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Start()
        {
            if (m_GameStateManager != null)
                Apply(m_GameStateManager.CurrentPhase);
        }

        void OnPhaseChanged(GamePhase phase)
        {
            Apply(phase);
        }

        void Apply(GamePhase phase)
        {
            var enabledForInvestigation = phase == GamePhase.Investigation;

            foreach (var target in m_GameObjectsToEnable)
            {
                if (target != null)
                    target.SetActive(enabledForInvestigation);
            }

            foreach (var target in m_BehavioursToEnable)
            {
                if (target != null)
                    target.enabled = enabledForInvestigation;
            }

            foreach (var target in m_CollidersToEnable)
            {
                if (target != null)
                    target.enabled = enabledForInvestigation;
            }

            foreach (var target in m_ButtonsToEnable)
            {
                if (target != null)
                    target.interactable = enabledForInvestigation;
            }
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;

            m_IsSubscribed = true;
        }

        void Unsubscribe()
        {
            if (!m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;

            m_IsSubscribed = false;
        }
    }
}
