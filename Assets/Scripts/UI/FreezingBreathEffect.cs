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

        void OnEnable()
        {
            if (m_Vapor == null)
            {
                var child = transform.Find("ENV FX - Freezing Condensation");
                if (child != null) m_Vapor = child.GetComponent<ParticleSystem>();
            }
        }

        public void SetThermometer(ThermometerTool thermometer) => m_Thermometer = thermometer;
        public void SetModeActive(bool active) => m_ModeActive = active;

        void Update()
        {
            if (m_Vapor == null) return;
            var freezing = m_ModeActive && m_Thermometer != null && m_Thermometer.CurrentCelsius <= 0f;
            var target = freezing ? Mathf.Lerp(4f, 15f, Mathf.InverseLerp(0f, -8f, m_Thermometer.CurrentCelsius)) : 0f;
            var emission = m_Vapor.emission;
            emission.rateOverTime = Mathf.MoveTowards(emission.rateOverTime.constant, target, Time.deltaTime * 12f);
        }
    }
}
