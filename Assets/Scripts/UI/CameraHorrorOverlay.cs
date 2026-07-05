using PhasmophobiAR.Scanning;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PhasmophobiAR.UI
{
    /// <summary>Low-cost URP lens treatment. All values are intentionally conservative for mobile AR readability.</summary>
    public sealed class CameraHorrorOverlay : MonoBehaviour
    {
        [SerializeField] Volume m_Volume;
        Vignette m_Vignette;
        FilmGrain m_Grain;
        ChromaticAberration m_Chromatic;
        LensDistortion m_Distortion;
        float m_Interference;
        ScannerMode m_Mode;
        [SerializeField] Material m_ScreenMaterial;
        [SerializeField] Camera m_Camera;

        void OnEnable()
        {
            m_Camera ??= Camera.main;
            m_Volume ??= GetComponent<Volume>();
            if (m_ScreenMaterial == null)
            {
                var screen = transform.Find("Lens Noise and Scanlines");
                if (screen != null && screen.TryGetComponent<Renderer>(out var renderer)) m_ScreenMaterial = renderer.sharedMaterial;
            }
            var profile = m_Volume != null ? m_Volume.sharedProfile : null;
            if (profile == null) return;
            profile.TryGet(out m_Vignette);
            profile.TryGet(out m_Grain);
            profile.TryGet(out m_Chromatic);
            profile.TryGet(out m_Distortion);
        }

        void LateUpdate()
        {
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) return;
            transform.SetPositionAndRotation(m_Camera.transform.position, m_Camera.transform.rotation);
        }

        public void SetVisible(bool visible) { if (m_Volume != null) m_Volume.enabled = visible; }
        public void SetMode(ScannerMode mode) => m_Mode = mode;
        public void SetInterference(float value) => m_Interference = Mathf.Clamp01(value);

        void Update()
        {
            if (m_Volume == null || !m_Volume.enabled || m_Grain == null || m_Chromatic == null || m_Distortion == null) return;
            var spike = Random.value < Time.unscaledDeltaTime * (.25f + m_Interference * 2f) ? Random.Range(.05f, .18f) : 0f;
            var spectral = m_Mode == ScannerMode.Spectral ? .045f : 0f;
            m_Grain.intensity.value = Mathf.Lerp(m_Grain.intensity.value, .1f + m_Interference * .12f + spike, .2f);
            m_Chromatic.intensity.value = Mathf.Lerp(m_Chromatic.intensity.value, .02f + spectral + m_Interference * .035f + spike, .18f);
            m_Distortion.intensity.value = -.055f - spike * .2f;
            if (m_ScreenMaterial != null)
            {
                m_ScreenMaterial.SetFloat("_Interference", m_Interference);
                m_ScreenMaterial.SetFloat("_Flicker", spike);
                m_ScreenMaterial.SetColor("_Tint", m_Mode == ScannerMode.Spectral
                    ? new Color(.3f, .48f, .8f, 1f)
                    : new Color(.18f, .45f, .3f, 1f));
            }
        }
    }
}
