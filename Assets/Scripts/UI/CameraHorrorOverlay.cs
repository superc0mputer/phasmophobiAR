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
        Renderer m_ScreenRenderer;
        Transform m_Screen;

        // AR Foundation replaces the camera projection matrix on-device. Keeping the
        // overlay at an authored distance can therefore put it inside Android's near
        // clip plane, even though it is visible in the Editor.
        const float ScreenDepthPadding = .05f;
        const float ScreenOverscan = 1.03f;

        void OnEnable()
        {
            m_Camera ??= Camera.main;
            m_Volume ??= GetComponent<Volume>();
            if (m_ScreenMaterial == null)
            {
                var screen = transform.Find("Lens Noise and Scanlines");
                if (screen != null && screen.TryGetComponent<Renderer>(out var renderer)) m_ScreenMaterial = renderer.sharedMaterial;
            }
            CacheScreen();
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
            FitScreenToCamera();
        }

        void CacheScreen()
        {
            m_Screen ??= transform.Find("Lens Noise and Scanlines");
            if (m_Screen != null && m_ScreenRenderer == null)
                m_Screen.TryGetComponent(out m_ScreenRenderer);
            if (m_ScreenMaterial == null && m_ScreenRenderer != null)
                m_ScreenMaterial = m_ScreenRenderer.sharedMaterial;
        }

        void FitScreenToCamera()
        {
            CacheScreen();
            if (m_Screen == null) return;

            // Use the runtime projection rather than Camera.fieldOfView: AR cameras
            // can supply an asymmetric, orientation-dependent projection on Android.
            var depth = Mathf.Max(m_Camera.nearClipPlane + ScreenDepthPadding, .15f);
            var bottomLeft = m_Camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            var bottomRight = m_Camera.ViewportToWorldPoint(new Vector3(1f, 0f, depth));
            var topLeft = m_Camera.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
            var center = m_Camera.ViewportToWorldPoint(new Vector3(.5f, .5f, depth));

            m_Screen.SetPositionAndRotation(center, m_Camera.transform.rotation);
            m_Screen.localScale = new Vector3(
                Vector3.Distance(bottomLeft, bottomRight) * ScreenOverscan,
                Vector3.Distance(bottomLeft, topLeft) * ScreenOverscan,
                1f);

            // AR background renderers may use unusual depth state. This overlay is
            // explicitly transparent and must be submitted after normal geometry.
            if (m_ScreenRenderer != null)
            {
                m_ScreenRenderer.shadowCastingMode = ShadowCastingMode.Off;
                m_ScreenRenderer.receiveShadows = false;
            }
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
