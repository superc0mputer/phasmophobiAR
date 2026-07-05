using PhasmophobiAR.Tools;
using UnityEngine;

namespace PhasmophobiAR.UI
{
    /// <summary>Diegetic condensation emitted just beyond the camera, gated by real thermometer data.</summary>
    public sealed class FreezingBreathEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem m_Vapor;
        ThermometerTool m_Thermometer;
        bool m_ModeActive;
        Camera m_Camera;
        static Texture2D s_VaporTexture;

        void OnEnable()
        {
            if (m_Vapor == null)
            {
                var child = transform.Find("ENV FX - Freezing Condensation");
                if (child != null) m_Vapor = child.GetComponent<ParticleSystem>();
            }

            m_Camera = Camera.main;
            ConfigureCondensation();
        }

        public void SetThermometer(ThermometerTool thermometer) => m_Thermometer = thermometer;
        public void SetModeActive(bool active) => m_ModeActive = active;

        void Update()
        {
            if (m_Vapor == null) return;
            UpdateEmitterPosition();
            var freezing = m_ModeActive && m_Thermometer != null && m_Thermometer.CurrentCelsius <= 0f;
            var target = freezing ? Mathf.Lerp(7f, 22f, Mathf.InverseLerp(0f, -8f, m_Thermometer.CurrentCelsius)) : 0f;
            var emission = m_Vapor.emission;
            emission.rateOverTime = Mathf.MoveTowards(emission.rateOverTime.constant, target, Time.deltaTime * 18f);
        }

        void ConfigureCondensation()
        {
            if (m_Vapor == null) return;

            var main = m_Vapor.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.15f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.7f, 0.84f, 0.95f, 0.09f),
                new Color(0.92f, 0.98f, 1f, 0.2f));
            main.maxParticles = 55;

            var shape = m_Vapor.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.035f;
            shape.length = 0.16f;

            var velocity = m_Vapor.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.045f, 0.045f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.025f, 0.11f);
            velocity.z = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);

            var noise = m_Vapor.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.055f, 0.14f);
            noise.frequency = 0.55f;
            noise.scrollSpeed = 0.18f;
            noise.damping = true;
            noise.octaveCount = 2;

            var size = m_Vapor.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.28f, 1.15f),
                new Keyframe(1f, 2.5f)));

            var color = m_Vapor.colorOverLifetime;
            color.enabled = true;
            var alpha = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.14f),
                new GradientAlphaKey(0.35f, 0.52f),
                new GradientAlphaKey(0f, 1f)
            };
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.78f, 0.9f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                alpha);
            color.color = gradient;

            var renderer = m_Vapor.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) return;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.material = CreateVaporMaterial(renderer.sharedMaterial);
        }

        void UpdateEmitterPosition()
        {
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) return;

            var cameraTransform = m_Camera.transform;
            m_Vapor.transform.SetPositionAndRotation(
                cameraTransform.position + cameraTransform.forward * 0.24f - cameraTransform.up * 0.11f,
                cameraTransform.rotation);
        }

        static Material CreateVaporMaterial(Material source)
        {
            var material = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.name = "Runtime Soft Condensation";
            material.SetTexture("_BaseMap", GetVaporTexture());
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        static Texture2D GetVaporTexture()
        {
            if (s_VaporTexture != null) return s_VaporTexture;

            const int size = 64;
            s_VaporTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Procedural Condensation Puff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                var radius = uv.magnitude;
                var breakup = Mathf.PerlinNoise(x * 0.105f, y * 0.105f) * 0.28f;
                var density = Mathf.Clamp01((1f - radius) * 1.8f + breakup - 0.18f);
                density *= density * (3f - 2f * density);
                pixels[y * size + x] = new Color(1f, 1f, 1f, density);
            }
            s_VaporTexture.SetPixels32(pixels);
            s_VaporTexture.Apply(false, true);
            return s_VaporTexture;
        }
    }
}
