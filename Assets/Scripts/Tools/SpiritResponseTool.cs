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
        GhostCaseController m_GhostCaseController;

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [SerializeField]
        Camera m_ARCamera;

        [Header("Response")]
        [SerializeField]
        float m_MaxResponseDistanceMeters = 4f;

        [SerializeField]
        float m_ResponseIntervalSeconds = 2.6f;

        [SerializeField]
        string m_IdleText = "SPIRIT BOX";

        [SerializeField]
        string m_ListeningText = "LISTENING...";

        [Header("3D UI")]
        [SerializeField]
        TMP_Text m_ResponseText;

        [SerializeField]
        Vector3 m_TextLocalPosition = new Vector3(0f, 0.22f, 0f);

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

            if (m_GhostCaseController == null)
                m_GhostCaseController = GhostCaseController.Instance;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main;

            EnsureResponseText();
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
            BillboardText();

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

            var profile = m_GhostCaseController != null ? m_GhostCaseController.CurrentProfile : null;
            var canRespond = HasSpiritResponse(profile);
            SetResponse(canRespond ? GetGhostPhrase(profile) : "...");

            if (canRespond)
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

        string GetGhostPhrase(GhostProfile profile)
        {
            var phrases = GetPhrases(profile);
            if (phrases == null || phrases.Length == 0)
                return "I am here.";

            var phrase = phrases[m_ResponseIndex % phrases.Length];
            m_ResponseIndex++;
            return phrase;
        }

        static string[] GetPhrases(GhostProfile profile)
        {
            if (profile == null)
                return new[] { "I am here.", "Behind you.", "Leave." };

            switch (profile.ghostType)
            {
                case GhostType.ShyGhost:
                    return new[] { "I am close.", "Please leave.", "Cold..." };
                case GhostType.Mimic:
                    return new[] { "I can be anyone.", "Your voice is mine.", "Listen closer." };
                default:
                    return new[] { "I am here.", "Behind you.", "Leave." };
            }
        }

        static bool HasSpiritResponse(GhostProfile profile)
        {
            if (profile == null || profile.requiredEvidence == null)
                return false;

            foreach (var evidenceType in profile.requiredEvidence)
            {
                if (evidenceType == EvidenceType.SpiritResponse)
                    return true;
            }

            return false;
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

        void EnsureResponseText()
        {
            if (m_ResponseText != null)
                return;

            var textObject = new GameObject("Spirit Response Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = m_TextLocalPosition;
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one * 0.01f;

            var text = textObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 3.2f;
            text.enableWordWrapping = true;
            text.rectTransform.sizeDelta = new Vector2(26f, 8f);
            text.color = new Color(0.72f, 0.96f, 1f, 1f);
            m_ResponseText = text;
        }

        void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
                return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.04f, 0f);
            box.size = new Vector3(0.12f, 0.08f, 0.14f);
        }

        void BillboardText()
        {
            if (m_ResponseText == null)
                return;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main;

            if (m_ARCamera == null)
                return;

            var toCamera = m_ResponseText.transform.position - m_ARCamera.transform.position;
            if (toCamera.sqrMagnitude < 0.001f)
                return;

            m_ResponseText.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        void SetResponse(string response)
        {
            m_CurrentResponse = response;
            if (m_ResponseText != null)
                m_ResponseText.text = response;
        }
    }
}
