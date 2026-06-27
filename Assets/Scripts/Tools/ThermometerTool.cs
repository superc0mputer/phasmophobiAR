using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using TMPro;
using UnityEngine;

namespace PhasmophobiAR.Tools
{
    public sealed class ThermometerTool : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [Header("Temperature")]
        [SerializeField]
        float m_MaxGhostInfluenceDistance = 8f;

        [SerializeField]
        float m_AmbientCelsius = 19.5f;

        [SerializeField]
        float m_MinGhostCelsius = -6f;

        [SerializeField]
        float m_FreezingEvidenceThresholdCelsius = 0f;

        [SerializeField]
        float m_Smoothing = 5f;

        [Header("Prefab References")]
        [SerializeField]
        Transform m_HologramRoot;

        [SerializeField]
        TMP_Text m_TemperatureText;

        [SerializeField]
        Transform m_TemperatureFill;

        [SerializeField]
        Renderer m_TemperatureFillRenderer;

        [SerializeField]
        Renderer m_BulbRenderer;

        [SerializeField]
        AudioSource m_AudioSource;

        [SerializeField]
        float m_RuntimeHologramScale = 0.65f;

        [SerializeField]
        float m_ScaleMinCelsius = -30f;

        [SerializeField]
        float m_ScaleMaxCelsius = 30f;

        [SerializeField]
        float m_FillBottomY = -0.235f;

        [SerializeField]
        float m_FillTopY = 0.27f;

        [SerializeField]
        Vector3 m_FillBaseScale = new Vector3(0.027f, 0.01f, 0.027f);

        float m_CurrentCelsius;
        Vector3 m_AuthoredHologramScale = Vector3.one;
        bool m_HasRecordedFreezing;

        public float CurrentCelsius => m_CurrentCelsius;
        public bool HasRecordedFreezing => m_HasRecordedFreezing;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            m_CurrentCelsius = m_AmbientCelsius;
            ApplyRuntimeHologramScale();
            EnsureAudio();
            EnsureCollider();
            UpdateFeedback(0f);
        }

        void Update()
        {
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
            {
                SmoothTemperature(m_AmbientCelsius);
                return;
            }

            var ghostInfluence = CalculateGhostInfluence();
            var targetTemperature = Mathf.Lerp(m_AmbientCelsius, m_MinGhostCelsius, ghostInfluence);
            SmoothTemperature(targetTemperature);
            TryRecordFreezingEvidence();
        }

        float CalculateGhostInfluence()
        {
            var ghosts = GhostSpawnController.GetSpawnedGhosts();
            if (ghosts == null || ghosts.Length == 0)
                return 0f;

            var best = 0f;
            foreach (var ghost in ghosts)
            {
                if (ghost == null)
                    continue;

                var distance = Vector3.Distance(transform.position, ghost.position);
                var influence = Mathf.Clamp01(1f - distance / Mathf.Max(0.01f, m_MaxGhostInfluenceDistance))
                    * GhostBehaviorController.GetTemperatureInfluenceMultiplier(ghost);
                if (influence > best)
                    best = influence;
            }

            return Mathf.Clamp01(best);
        }

        void ApplyRuntimeHologramScale()
        {
            if (m_HologramRoot == null)
                return;

            m_AuthoredHologramScale = m_HologramRoot.localScale;
            m_HologramRoot.localScale = m_AuthoredHologramScale * Mathf.Max(0.01f, m_RuntimeHologramScale);
        }

        void SmoothTemperature(float targetTemperature)
        {
            m_CurrentCelsius = Mathf.Lerp(m_CurrentCelsius, targetTemperature, Mathf.Clamp01(Time.deltaTime * m_Smoothing));
            var coldAmount = Mathf.InverseLerp(m_AmbientCelsius, m_MinGhostCelsius, m_CurrentCelsius);
            UpdateFeedback(coldAmount);
        }

        void UpdateFeedback(float coldAmount)
        {
            coldAmount = Mathf.Clamp01(coldAmount);

            if (m_TemperatureText != null)
            {
                m_TemperatureText.text = $"{m_CurrentCelsius:0.0} C";
                m_TemperatureText.color = TemperatureScaleColor(m_CurrentCelsius);
            }

            UpdateThermometerFill(coldAmount);
        }

        void UpdateThermometerFill(float coldAmount)
        {
            var scaleAmount = Mathf.InverseLerp(m_ScaleMinCelsius, m_ScaleMaxCelsius, m_CurrentCelsius);
            var fillHeight = Mathf.Lerp(0.02f, Mathf.Max(0.02f, m_FillTopY - m_FillBottomY), scaleAmount);

            if (m_TemperatureFill != null)
            {
                m_TemperatureFill.localScale = new Vector3(m_FillBaseScale.x, fillHeight * 0.5f, m_FillBaseScale.z);
                m_TemperatureFill.localPosition = new Vector3(
                    m_TemperatureFill.localPosition.x,
                    m_FillBottomY + fillHeight * 0.5f,
                    m_TemperatureFill.localPosition.z);
            }

            var color = TemperatureScaleColor(m_CurrentCelsius);
            if (m_TemperatureFillRenderer != null)
                SetRendererColor(m_TemperatureFillRenderer, color * 1.35f);

            if (m_BulbRenderer != null)
                SetRendererColor(m_BulbRenderer, color * 1.35f);
        }

        void TryRecordFreezingEvidence()
        {
            if (m_HasRecordedFreezing || m_CurrentCelsius > m_FreezingEvidenceThresholdCelsius)
                return;

            m_HasRecordedFreezing = true;
            if (m_EvidenceRegistry == null)
            {
                m_EvidenceRegistry = EvidenceRegistry.Instance;
                if (m_EvidenceRegistry == null)
                {
                    Debug.LogWarning("Freezing temperature evidence could not be recorded because no EvidenceRegistry exists in the scene.");
                    return;
                }
            }

            m_EvidenceRegistry.RecordEvidence(EvidenceType.FreezingTemperature);
        }

        void EnsureAudio()
        {
            if (m_AudioSource == null)
            {
                m_AudioSource = GetComponent<AudioSource>();
                if (m_AudioSource == null)
                    m_AudioSource = gameObject.AddComponent<AudioSource>();
            }

            m_AudioSource.playOnAwake = false;
            m_AudioSource.spatialBlend = 1f;
            m_AudioSource.rolloffMode = AudioRolloffMode.Linear;
            m_AudioSource.minDistance = 0.15f;
            m_AudioSource.maxDistance = 3f;
        }

        void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
                return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.04f, 0f);
            box.size = new Vector3(0.08f, 0.06f, 0.18f);
        }

        static Color TemperatureColor(float coldAmount)
        {
            coldAmount = Mathf.Clamp01(coldAmount);
            if (coldAmount < 0.5f)
                return Color.Lerp(new Color(0.1f, 1f, 0.45f), new Color(0.12f, 0.85f, 1f), coldAmount * 2f);

            return Color.Lerp(new Color(0.12f, 0.85f, 1f), new Color(0.65f, 0.35f, 1f), (coldAmount - 0.5f) * 2f);
        }

        static Color TemperatureScaleColor(float celsius)
        {
            if (celsius <= 0f)
                return Color.Lerp(new Color(0.25f, 0.45f, 1f), new Color(0.1f, 0.95f, 1f), Mathf.InverseLerp(-10f, 0f, celsius));

            if (celsius < 20f)
                return Color.Lerp(new Color(0.1f, 0.95f, 1f), new Color(0.2f, 1f, 0.35f), Mathf.InverseLerp(0f, 20f, celsius));

            if (celsius < 35f)
                return Color.Lerp(new Color(0.2f, 1f, 0.35f), new Color(1f, 0.85f, 0.05f), Mathf.InverseLerp(20f, 35f, celsius));

            return Color.Lerp(new Color(1f, 0.85f, 0.05f), new Color(1f, 0.12f, 0.03f), Mathf.InverseLerp(35f, 50f, celsius));
        }

        static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            var material = renderer.material;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            else
                material.color = color;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
        }
    }
}
