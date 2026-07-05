using PhasmophobiAR.Game;
using UnityEngine;

namespace PhasmophobiAR.UI
{
    /// <summary>Camera-local 3D particles with depth, drift and soft pulses; never screen-space icons.</summary>
    public sealed class GhostOrbField : MonoBehaviour
    {
        [SerializeField] ParticleSystem m_Particles;
        bool m_ModeActive;
        Camera m_Camera;

        void OnEnable()
        {
            if (m_Particles == null)
            {
                var child = transform.Find("ENV FX - Ghost Orb Volume");
                if (child != null) m_Particles = child.GetComponent<ParticleSystem>();
            }

            m_Camera = Camera.main;
            ConfigureRoundOrbs();
        }

        public void SetModeActive(bool active)
        {
            m_ModeActive = active;
            UpdateEmission();
        }

        void Update()
        {
            UpdateEmitterPosition();
            UpdateEmission();
        }

        void ConfigureRoundOrbs()
        {
            if (m_Particles == null) return;

            var main = m_Particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.07f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.maxParticles = 28;

            var shape = m_Particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.8f, 1.8f, 3.5f);

            var noise = m_Particles.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.08f;
            noise.damping = true;

            var renderer = m_Particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) return;

            // A sphere mesh keeps the evidence genuinely round from every viewing angle.
            var sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            if (sphere != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = sphere;
                renderer.enableGPUInstancing = true;
            }

            if (renderer.sharedMaterial != null)
            {
                var material = new Material(renderer.sharedMaterial) { name = "Runtime Spectral Orb" };
                material.SetColor("_BaseColor", new Color(0.72f, 0.9f, 1f, 0.72f));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                renderer.material = material;
            }
        }

        void UpdateEmitterPosition()
        {
            if (m_Particles == null) return;
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) return;

            var cameraTransform = m_Camera.transform;
            m_Particles.transform.SetPositionAndRotation(
                cameraTransform.position + cameraTransform.forward * 2.5f,
                cameraTransform.rotation);
        }

        void UpdateEmission()
        {
            if (m_Particles == null) return;
            var visible = m_ModeActive && CurrentCaseHasOrbs();
            if (visible && !m_Particles.isPlaying) m_Particles.Play();
            else if (!visible && m_Particles.isPlaying) m_Particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        static bool CurrentCaseHasOrbs()
        {
            var profile = GhostCaseController.Instance != null ? GhostCaseController.Instance.CurrentProfile : null;
            if (profile?.requiredEvidence == null) return false;
            foreach (var evidence in profile.requiredEvidence)
                if (evidence == EvidenceType.SpectralTrace) return true;
            return false;
        }
    }
}
