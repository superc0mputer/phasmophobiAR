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

        [SerializeField]
        Camera m_ARCamera;

        [Header("Response")]
        [SerializeField]
        float m_MaxResponseDistanceMeters = 4f;

        [SerializeField]
        float m_ResponseIntervalSeconds = 2.6f;

        [SerializeField]
        string m_IdleText = "*static*";

        [SerializeField]
        string m_ListeningText = "*static*";

        [SerializeField]
        string[] m_DefaultResponses =
        {
            "I am here.",
            "Behind you.",
            "Leave.",
            "Get out.",
            "Cold...",
            "Run.",
            "I see you.",
            "Do not stay.",
            "Close.",
            "Help me.",
            "Turn around.",
            "Not alone.",
            "Listen closer.",
            "Your voice is mine.",
            "In the dark.",
            "Come closer."
        };

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

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main;

            EnsureDeviceVisual();
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

        void EnsureDeviceVisual()
        {
            if (transform.Find("Spirit Response Body") != null)
                return;

            CreateDeviceVisual(transform);
        }

        static void CreateDeviceVisual(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Spirit Response Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            body.transform.localScale = new Vector3(0.095f, 0.018f, 0.115f);
            DestroyPrimitiveCollider(body);
            SetColor(body, new Color(0.06f, 0.05f, 0.09f));

            var speaker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            speaker.name = "Spirit Response Speaker";
            speaker.transform.SetParent(parent, false);
            speaker.transform.localPosition = new Vector3(0f, 0.052f, 0.022f);
            speaker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            speaker.transform.localScale = new Vector3(0.026f, 0.006f, 0.026f);
            DestroyPrimitiveCollider(speaker);
            SetColor(speaker, new Color(0.22f, 0.18f, 0.32f));

            var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = "Spirit Response Display";
            display.transform.SetParent(parent, false);
            display.transform.localPosition = new Vector3(0f, 0.053f, -0.025f);
            display.transform.localScale = new Vector3(0.06f, 0.006f, 0.028f);
            DestroyPrimitiveCollider(display);
            SetColor(display, new Color(0.55f, 0.9f, 1f));
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

            var toCamera = m_ARCamera.transform.position - m_ResponseText.transform.position;
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

        static void DestroyPrimitiveCollider(GameObject primitive)
        {
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        static void SetColor(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.material.color = color;
        }
    }
}
