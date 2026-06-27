using PhasmophobiAR.Game;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    public sealed class GhostBehaviorController : MonoBehaviour
    {
        const float k_DefaultGazeAngleDegrees = 12f;

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
        float m_GazeTime;
        float m_HiddenUntilTime;
        bool m_IsHidden;

        public GhostType GhostType => m_GhostType;
        public GhostProfile Profile => m_Profile;
        public bool IsVisible => !m_IsHidden;
        public bool IsRevealed => IsVisible;
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
            SetHidden(false);
        }

        void Update()
        {
            if (m_Profile == null)
                return;

            UpdateMovement();
            UpdateGazeHide();
            UpdateStaticVisuals();
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

        void UpdateGazeHide()
        {
            if (m_Profile.gazeHideThresholdSeconds <= 0f || m_ARCamera == null)
                return;

            if (m_IsHidden)
            {
                if (Time.time >= m_HiddenUntilTime)
                    SetHidden(false);
                return;
            }

            m_GazeTime = IsCameraLookingAtGhost() ? m_GazeTime + Time.deltaTime : 0f;
            if (m_GazeTime >= m_Profile.gazeHideThresholdSeconds)
            {
                m_GazeTime = 0f;
                m_HiddenUntilTime = Time.time + Mathf.Max(0.1f, m_Profile.hiddenDurationSeconds);
                SetHidden(true);
            }
        }

        bool IsCameraLookingAtGhost()
        {
            var toGhost = transform.position - m_ARCamera.position;
            if (toGhost.sqrMagnitude < 0.001f)
                return false;

            var angle = Vector3.Angle(m_ARCamera.forward, toGhost.normalized);
            return angle <= k_DefaultGazeAngleDegrees;
        }

        void UpdateStaticVisuals()
        {
            if (m_Profile.ghostType != GhostType.StaticGhost || m_IsHidden)
                return;

            var pulse = Mathf.PerlinNoise(Time.time * 18f, m_MoveSeed);
            var scaleJitter = Mathf.Lerp(0.96f, 1.08f, pulse);
            transform.localScale = m_InitialScale * scaleJitter;

            if (m_Renderers == null)
                return;

            var color = Color.Lerp(new Color(0.45f, 0.85f, 1f, 0.7f), Color.white, pulse);
            foreach (var renderer in m_Renderers)
                ApplyRendererColor(renderer, color);
        }

        void SetHidden(bool hidden)
        {
            m_IsHidden = hidden;
            CacheRenderers();

            if (m_Renderers == null)
                return;

            foreach (var renderer in m_Renderers)
            {
                if (renderer != null)
                    renderer.enabled = !hidden;
            }
        }

        void CacheRenderers()
        {
            if (m_Renderers == null || m_Renderers.Length == 0)
                m_Renderers = GetComponentsInChildren<Renderer>(true);
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
