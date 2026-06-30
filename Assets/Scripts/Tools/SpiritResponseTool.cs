using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using PhasmophobiAR.Scanning;
using TMPro;
using UnityEngine;

namespace PhasmophobiAR.Tools
{
    public sealed class SpiritResponseTool : MonoBehaviour
    {
        [SerializeField]
        ScannerModeManager m_ScannerModeManager;

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [Header("Response")]
        [SerializeField]
        float m_MaxResponseDistanceMeters = 4f;

        [SerializeField]
        float m_ResponseIntervalSeconds = 2.6f;

        [SerializeField, Range(0f, 1f)]
        float m_ResponseChance = 0.35f;

        [SerializeField]
        string m_IdleText = "*static*";

        [SerializeField]
        string m_ListeningText = "*static*";

        [SerializeField]
        string[] m_DefaultResponses =
        {
            "I AM HERE",
            "BEHIND YOU",
            "Leave.",
            "GET OUT",
            "COLD",
            "Run.",
            "I SEE YOU",
            "DO NOT STAY",
            "Close.",
            "HELP ME",
            "TURN AROUND",
            "NOT ALONE",
            "LISTEN",
            "IN THE DARK",
            "COME CLOSER"
        };

        [Header("Overlay UI")]
        [SerializeField]
        TMP_Text m_ResponseText;

        float m_NextResponseTime;
        int m_ResponseIndex;
        bool m_HasRecordedSpiritResponse;
        string m_CurrentResponse;

        public string CurrentResponse => string.IsNullOrEmpty(m_CurrentResponse) ? m_IdleText : m_CurrentResponse;

        void Awake()
        {
            if (m_ScannerModeManager == null)
                m_ScannerModeManager = ScannerModeManager.Instance;

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            EnsureCollider();
            SetResponse(m_IdleText);
        }

        void Start()
        {
            if (m_ScannerModeManager != null)
                m_ScannerModeManager.TrySwitchMode(ScannerMode.SpiritResponse);
        }

        void OnEnable()
        {
            if (m_EvidenceRegistry != null)
                m_EvidenceRegistry.EvidenceCleared += OnEvidenceCleared;
        }

        void OnDisable()
        {
            if (m_EvidenceRegistry != null)
                m_EvidenceRegistry.EvidenceCleared -= OnEvidenceCleared;
        }

        void Update()
        {
            if (!CanListen())
            {
                SetResponse(m_IdleText);
                return;
            }

            if (Time.time < m_NextResponseTime)
                return;

            m_NextResponseTime = Time.time + Mathf.Max(0.25f, m_ResponseIntervalSeconds);
            UpdateSpiritResponse();
        }

        bool CanListen()
        {
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
                return false;

            return m_ScannerModeManager != null && m_ScannerModeManager.CurrentMode == ScannerMode.SpiritResponse;
        }

        void UpdateSpiritResponse()
        {
            if (!TryGetNearestGhostDistance(out var distance) || distance > m_MaxResponseDistanceMeters)
            {
                SetResponse(m_ListeningText);
                return;
            }

            if (Random.value > m_ResponseChance)
            {
                SetResponse(m_ListeningText);
                return;
            }

            SetResponse(GetGhostPhrase());
            TryRecordSpiritResponseEvidence();
        }

        bool TryGetNearestGhostDistance(out float nearestDistance)
        {
            nearestDistance = float.PositiveInfinity;

            var ghosts = GhostSpawnController.GetSpawnedGhostInfos();
            if (ghosts == null || ghosts.Length == 0)
                return false;

            var found = false;
            foreach (var ghost in ghosts)
            {
                if (ghost == null)
                    continue;

                var distance = Vector3.Distance(transform.position, ghost.WorldPosition);
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                found = true;
            }

            return found;
        }

        string GetGhostPhrase()
        {
            if (m_DefaultResponses == null || m_DefaultResponses.Length == 0)
                return "I am here.";

            var phrase = m_DefaultResponses[m_ResponseIndex % m_DefaultResponses.Length];
            m_ResponseIndex++;
            return phrase;
        }

        void TryRecordSpiritResponseEvidence()
        {
            if (m_HasRecordedSpiritResponse)
                return;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            if (m_EvidenceRegistry == null)
            {
                Debug.LogWarning("Spirit response evidence could not be recorded because no EvidenceRegistry exists in the scene.");
                return;
            }

            m_HasRecordedSpiritResponse = true;
            m_EvidenceRegistry.RecordEvidence(EvidenceType.SpiritResponse);
        }

        void OnEvidenceCleared()
        {
            m_HasRecordedSpiritResponse = false;
        }

        void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
                return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.04f, 0f);
            box.size = new Vector3(0.12f, 0.08f, 0.14f);
        }

        void SetResponse(string response)
        {
            m_CurrentResponse = response;
            if (m_ResponseText != null)
                m_ResponseText.text = response;
        }
    }
}
