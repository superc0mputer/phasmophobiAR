using PhasmophobiAR.Game;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    public sealed class GhostBehaviorController : MonoBehaviour
    {
        [SerializeField]
        GhostType m_GhostType;

        [SerializeField]
        GhostProfile m_Profile;

        [SerializeField]
        Transform m_ARCamera;

        Renderer[] m_Renderers;
        Vector3 m_AnchorLocalPosition;
        Vector3 m_InitialScale;
        float m_MoveSeed;
        GhostRevealState m_RevealState = GhostRevealState.Hidden;
        float m_CaptureProgress;

        public GhostType GhostType => m_GhostType;
        public GhostProfile Profile => m_Profile;
        public GhostRevealState RevealState => m_RevealState;
        public bool IsVisible => m_RevealState != GhostRevealState.Hidden;
        public bool IsRevealed => m_RevealState == GhostRevealState.Revealed || m_RevealState == GhostRevealState.Capturing || m_RevealState == GhostRevealState.Captured;
        public float CaptureProgress => m_CaptureProgress;
        public float RevealDifficulty => m_Profile != null ? Mathf.Clamp01(m_Profile.revealDifficulty) : 0.5f;
        public float CaptureDifficulty => m_Profile != null ? Mathf.Clamp01(m_Profile.captureDifficulty) : 0.5f;
        public float EMFSignalMultiplier => GetPositiveMultiplier(m_Profile != null ? m_Profile.emfSignalMultiplier : 1f);
        public float TemperatureInfluenceMultiplier => GetPositiveMultiplier(m_Profile != null ? m_Profile.temperatureInfluenceMultiplier : 1f);
        public float SpectralTraceMultiplier => GetPositiveMultiplier(m_Profile != null ? m_Profile.spectralTraceMultiplier : 1f);

        void Awake()
        {
            CacheRenderers();
            m_AnchorLocalPosition = transform.localPosition;
            m_InitialScale = transform.localScale;
            m_MoveSeed = Random.value * 100f;
        }

        public void Configure(GhostProfile profile, Transform arCamera)
        {
            m_Profile = profile;
            m_GhostType = profile != null ? profile.ghostType : default;
            m_ARCamera = arCamera != null ? arCamera : m_ARCamera;
            CacheRenderers();
            SetRevealState(GhostRevealState.Hidden, 0f);
        }

        void Update()
        {
            if (m_Profile == null)
                return;

            UpdateMovement();
            UpdateRevealVisuals();
        }

        void UpdateMovement()
        {
            var radius = Mathf.Max(0f, m_Profile.movementRadiusMeters);
            var speed = Mathf.Max(0f, m_Profile.movementSpeedMetersPerSecond);
            if (radius <= 0f || speed <= 0f)
                return;

            var phase = Time.time * speed + m_MoveSeed;
            var offset = new Vector3(
                Mathf.Sin(phase) * radius,
                0f,
                Mathf.Cos(phase * 0.73f) * radius);

            transform.localPosition = m_AnchorLocalPosition + offset;
        }

        void UpdateRevealVisuals()
        {
            if (m_RevealState == GhostRevealState.Hidden)
            {
                SetRenderersEnabled(false);
                return;
            }

            SetRenderersEnabled(true);

            var pulse = m_Profile != null && m_Profile.ghostType == GhostType.StaticGhost
                ? Mathf.PerlinNoise(Time.time * 18f, m_MoveSeed)
                : 0f;
            var color = GetStateColor(m_RevealState, m_CaptureProgress, pulse);
            transform.localScale = GetRevealScale(pulse);

            foreach (var renderer in m_Renderers)
                ApplyRendererColor(renderer, color);
        }

        Vector3 GetRevealScale(float pulse)
        {
            if (m_Profile == null)
                return m_InitialScale;

            if (m_Profile.ghostType != GhostType.StaticGhost || m_RevealState == GhostRevealState.Hidden)
                return m_InitialScale;

            var scaleJitter = Mathf.Lerp(0.96f, 1.08f, pulse);
            return m_InitialScale * scaleJitter;
        }

        public void SetRevealState(GhostRevealState state, float captureProgress = 0f)
        {
            m_RevealState = state;
            m_CaptureProgress = Mathf.Clamp01(captureProgress);
            CacheRenderers();
            UpdateRevealVisuals();
        }

        void CacheRenderers()
        {
            if (m_Renderers == null || m_Renderers.Length == 0)
                m_Renderers = GetComponentsInChildren<Renderer>(true);
        }

        void SetRenderersEnabled(bool enabled)
        {
            CacheRenderers();

            if (m_Renderers == null)
                return;

            foreach (var renderer in m_Renderers)
            {
                if (renderer != null)
                    renderer.enabled = enabled;
            }
        }

        static Color GetStateColor(GhostRevealState state, float captureProgress, float pulse)
        {
            switch (state)
            {
                case GhostRevealState.PartialReveal:
                    return Color.Lerp(new Color(0.35f, 0.75f, 1f, 0.2f), new Color(0.6f, 0.9f, 1f, 0.45f), pulse);
                case GhostRevealState.Revealed:
                    return Color.Lerp(new Color(0.8f, 0.95f, 1f, 0.75f), Color.white, pulse * 0.35f);
                case GhostRevealState.Capturing:
                    return Color.Lerp(new Color(1f, 0.78f, 0.25f, 0.85f), new Color(1f, 0.95f, 0.7f, 1f), Mathf.Clamp01(captureProgress + pulse * 0.15f));
                case GhostRevealState.Captured:
                    return new Color(1f, 0.9f, 0.55f, 1f);
                default:
                    return Color.clear;
            }
        }

        static void ApplyRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            var material = renderer.material;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
        }

        static float GetPositiveMultiplier(float value)
        {
            return Mathf.Max(0.01f, value);
        }

        public static GhostBehaviorController Get(Transform ghost)
        {
            return ghost != null ? ghost.GetComponent<GhostBehaviorController>() : null;
        }

        public static float GetEMFSignalMultiplier(Transform ghost)
        {
            var behavior = Get(ghost);
            return behavior != null ? behavior.EMFSignalMultiplier : 1f;
        }

        public static float GetTemperatureInfluenceMultiplier(Transform ghost)
        {
            var behavior = Get(ghost);
            return behavior != null ? behavior.TemperatureInfluenceMultiplier : 1f;
        }

        public static float GetSpectralTraceMultiplier(Transform ghost)
        {
            var behavior = Get(ghost);
            return behavior != null ? behavior.SpectralTraceMultiplier : 1f;
        }
    }
}
