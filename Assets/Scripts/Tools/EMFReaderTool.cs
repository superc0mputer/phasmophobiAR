using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using TMPro;
using UnityEngine;

namespace PhasmophobiAR.Tools
{
    public sealed class EMFReaderTool : MonoBehaviour
    {
        const int k_MaxEMFLevel = 5;

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [SerializeField]
        EMFSignalSettings m_SignalSettings = new EMFSignalSettings();

        [Header("Visuals")]
        [SerializeField]
        string m_ModelResourcePath = "Models/emf-reader-game-ready/source/untitled";

        [SerializeField]
        Vector3 m_ModelLocalScale = Vector3.one;

        [SerializeField]
        string m_ModelVariantName = "EMF Reader White";

        [SerializeField]
        string m_BaseColorTexturePath = "Models/emf-reader-game-ready/textures/2046_White_EMF_Reader_Basecolor";

        [SerializeField]
        string m_RoughnessTexturePath = "Models/emf-reader-game-ready/textures/2046_White_EMF_Reader_Roughness";

        [SerializeField]
        string m_NormalTexturePath = "Models/emf-reader-game-ready/textures/2046_EMF_Reader_Normal";

        [SerializeField]
        Renderer[] m_LevelRenderers;

        [SerializeField]
        Renderer m_DisplayRenderer;

        [SerializeField]
        Light m_SpikeLight;

        [SerializeField]
        Transform m_HologramRoot;

        [SerializeField]
        Renderer[] m_HologramArcSegments;

        [SerializeField]
        Renderer[] m_HologramBars;

        [SerializeField]
        TMP_Text m_HologramValueText;

        [SerializeField]
        Transform m_HologramPointer;

        [SerializeField]
        float m_RuntimeHologramScale = 0.65f;

        [Header("Audio")]
        [SerializeField]
        AudioSource m_AudioSource;

        [SerializeField]
        float m_MinBeepIntervalSeconds = 0.12f;

        [SerializeField]
        float m_MaxBeepIntervalSeconds = 1.2f;

        [SerializeField]
        float m_BeepVolume = 0.35f;

        float m_CurrentSignal;
        int m_CurrentLevel;
        float m_NextBeepTime;
        Vector3 m_AuthoredHologramScale = Vector3.one;
        AudioClip m_BeepClip;
        bool m_HasRecordedSpike;

        public float CurrentSignal => m_CurrentSignal;
        public int CurrentLevel => m_CurrentLevel;
        public bool HasRecordedSpike => m_HasRecordedSpike;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;

            EnsureModel();
            ApplyRuntimeHologramScale();
            EnsureAudio();
            EnsureCollider();
            UpdateFeedback(0f, 0);
        }

        void Update()
        {
            if (m_GameStateManager != null && m_GameStateManager.CurrentPhase != GamePhase.Investigation)
            {
                SetSignal(0f);
                return;
            }

            var signal = EMFSignalCalculator.CalculateFromSpawnedGhosts(
                transform.position,
                transform.forward,
                m_SignalSettings,
                false);

            SetSignal(signal);
            TickAudio();
            TryRecordSpikeEvidence();
        }

        void SetSignal(float signal)
        {
            m_CurrentSignal = Mathf.Clamp01(signal);
            m_CurrentLevel = EMFSignalCalculator.ToEMFLevel(m_CurrentSignal, m_SignalSettings);
            UpdateFeedback(m_CurrentSignal, m_CurrentLevel);
        }

        void ApplyRuntimeHologramScale()
        {
            if (m_HologramRoot == null)
                return;

            m_AuthoredHologramScale = m_HologramRoot.localScale;
            m_HologramRoot.localScale = m_AuthoredHologramScale * Mathf.Max(0.01f, m_RuntimeHologramScale);
        }

        void TryRecordSpikeEvidence()
        {
            if (m_HasRecordedSpike || m_CurrentLevel < k_MaxEMFLevel)
                return;

            m_HasRecordedSpike = true;
            if (m_EvidenceRegistry == null)
            {
                m_EvidenceRegistry = EvidenceRegistry.Instance;
                if (m_EvidenceRegistry == null)
                {
                    Debug.LogWarning("EMF spike evidence could not be recorded because no EvidenceRegistry exists in the scene.");
                    return;
                }
            }

            m_EvidenceRegistry.RecordEvidence(EvidenceType.EMFSpike);
        }

        void TickAudio()
        {
            if (m_AudioSource == null || m_BeepClip == null || m_CurrentLevel <= 0)
                return;

            if (Time.time < m_NextBeepTime)
                return;

            var interval = Mathf.Lerp(m_MaxBeepIntervalSeconds, m_MinBeepIntervalSeconds, m_CurrentSignal);
            m_AudioSource.pitch = Mathf.Lerp(0.85f, 1.75f, m_CurrentSignal);
            m_AudioSource.PlayOneShot(m_BeepClip, Mathf.Lerp(0.12f, m_BeepVolume, m_CurrentSignal));
            m_NextBeepTime = Time.time + interval;
        }

        void UpdateFeedback(float signal, int level)
        {
            if (m_LevelRenderers != null)
            {
                for (var i = 0; i < m_LevelRenderers.Length; i++)
                {
                    if (m_LevelRenderers[i] == null)
                        continue;

                    var active = i < level;
                    SetRendererColor(m_LevelRenderers[i], active ? LevelColor(i + 1) : new Color(0.02f, 0.03f, 0.025f));
                }
            }

            if (m_DisplayRenderer != null)
                SetRendererColor(m_DisplayRenderer, Color.Lerp(new Color(0.02f, 0.12f, 0.08f), new Color(0.1f, 1f, 0.3f), signal));

            UpdateHologram(signal);

            if (m_SpikeLight != null)
            {
                m_SpikeLight.enabled = level >= k_MaxEMFLevel;
                m_SpikeLight.intensity = level >= k_MaxEMFLevel ? Mathf.Lerp(0.4f, 2.4f, signal) : 0f;
            }
        }

        void EnsureModel()
        {
            if (transform.Find("EMF Model") != null)
                return;

            var modelPrefab = Resources.Load<GameObject>(m_ModelResourcePath);
            if (modelPrefab == null)
                return;

            var model = Instantiate(modelPrefab, transform);
            model.name = "EMF Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = m_ModelLocalScale;
            KeepConfiguredModelVariant(model.transform);
            ApplyModelTextures(model.transform);
        }

        void KeepConfiguredModelVariant(Transform modelRoot)
        {
            if (modelRoot == null || string.IsNullOrEmpty(m_ModelVariantName))
                return;

            var selectedVariant = FindChildRecursive(modelRoot, m_ModelVariantName);
            if (selectedVariant == null)
                return;

            for (var i = modelRoot.childCount - 1; i >= 0; i--)
            {
                var child = modelRoot.GetChild(i);
                if (child == selectedVariant)
                    continue;

                Destroy(child.gameObject);
            }
        }

        void ApplyModelTextures(Transform modelRoot)
        {
            var baseColor = Resources.Load<Texture2D>(m_BaseColorTexturePath);
            var roughness = Resources.Load<Texture2D>(m_RoughnessTexturePath);
            var normal = Resources.Load<Texture2D>(m_NormalTexturePath);

            if (baseColor == null && roughness == null && normal == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            foreach (var renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var material = shader != null ? new Material(shader) : new Material(renderer.sharedMaterial);
                if (baseColor != null)
                    SetMaterialTexture(material, "_BaseMap", "_MainTex", baseColor);

                if (normal != null)
                {
                    SetMaterialTexture(material, "_BumpMap", "_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }

                if (roughness != null)
                    SetMaterialTexture(material, "_MetallicGlossMap", "_MetallicGlossMap", roughness);

                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.45f);
                renderer.material = material;
            }
        }

        void UpdateHologram(float signal)
        {
            if (m_HologramValueText != null)
            {
                m_HologramValueText.text = (signal * 5f).ToString("0.0");
                m_HologramValueText.color = SignalColor(signal);
            }

            if (m_HologramPointer != null)
                m_HologramPointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(180f, 0f, Mathf.Clamp01(signal)));

            UpdateHologramRenderers(m_HologramArcSegments, signal);
            UpdateHologramRenderers(m_HologramBars, signal);
        }

        static void UpdateHologramRenderers(Renderer[] renderers, float signal)
        {
            if (renderers == null)
                return;

            signal = Mathf.Clamp01(signal);
            var activeCount = Mathf.CeilToInt(signal * renderers.Length);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                var t = renderers.Length == 1 ? 0f : i / (float)(renderers.Length - 1);
                var color = SignalColor(t);
                renderers[i].enabled = i < activeCount;

                if (renderers[i].enabled)
                    SetRendererColor(renderers[i], color * 1.35f);
            }
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
            m_AudioSource.maxDistance = 4f;
            m_BeepClip = CreateBeepClip();
        }

        void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
                return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.025f, 0f);
            box.size = new Vector3(0.12f, 0.05f, 0.18f);
        }

        static AudioClip CreateBeepClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.055f;
            const float frequency = 1200f;

            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            var clip = AudioClip.Create("EMF Beep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static Color LevelColor(int level)
        {
            if (level >= 5)
                return new Color(1f, 0.08f, 0.05f);
            if (level >= 4)
                return new Color(1f, 0.76f, 0.08f);

            return new Color(0.08f, 1f, 0.25f);
        }

        static Color SignalColor(float signal)
        {
            signal = Mathf.Clamp01(signal);
            if (signal < 0.5f)
                return Color.Lerp(new Color(0.05f, 1f, 0.2f), new Color(1f, 0.9f, 0.05f), signal * 2f);

            return Color.Lerp(new Color(1f, 0.9f, 0.05f), new Color(1f, 0.05f, 0.03f), (signal - 0.5f) * 2f);
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

        static void SetMaterialTexture(Material material, string primaryProperty, string fallbackProperty, Texture texture)
        {
            if (material == null || texture == null)
                return;

            if (material.HasProperty(primaryProperty))
                material.SetTexture(primaryProperty, texture);
            else if (material.HasProperty(fallbackProperty))
                material.SetTexture(fallbackProperty, texture);
        }

        static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                var nested = FindChildRecursive(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

    }
}
