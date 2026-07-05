using PhasmophobiAR.Game;
using UnityEngine;

namespace PhasmophobiAR.UI
{
    /// <summary>Camera-local 3D particles with depth, drift and soft pulses; never screen-space icons.</summary>
    public sealed class GhostOrbField : MonoBehaviour
    {
        [SerializeField] ParticleSystem m_Particles;
        bool m_ModeActive;

        void OnEnable()
        {
            if (m_Particles == null)
            {
                var child = transform.Find("ENV FX - Ghost Orb Volume");
                if (child != null) m_Particles = child.GetComponent<ParticleSystem>();
            }
        }

        public void SetModeActive(bool active)
        {
            m_ModeActive = active;
            UpdateEmission();
        }

        void Update() => UpdateEmission();

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
