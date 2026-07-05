using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using PhasmophobiAR.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    /// <summary>
    /// A restrained camera-device presentation which observes existing gameplay systems.
    /// The bounded screen surface supplies readable instrumentation while orbs, vapor and
    /// lens interference remain spatial. No evidence logic lives here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationVisualSystem : MonoBehaviour
    {
        [SerializeField] ScannerModeManager m_ModeManager;
        [SerializeField] GameStateManager m_GameState;
        [SerializeField] Camera m_Camera;
        [SerializeField, Range(.08f, .8f)] float m_TransitionSeconds = .24f;
        [SerializeField] AudioSource m_TransitionAudio;
        [SerializeField] AudioClip m_ModeSwitchClip;

        [Header("Scene-authored presentation references")]
        [SerializeField] Transform m_Rig;
        [SerializeField] RectTransform m_Panel;
        [SerializeField] CanvasGroup m_PanelGroup;
        [SerializeField] TMP_Text m_Title;
        [SerializeField] TMP_Text m_Readout;
        [SerializeField] TMP_Text m_Status;
        [SerializeField] RectTransform m_Needle;
        [SerializeField] RectTransform[] m_Bars;
        [SerializeField] Image[] m_Accents;
        [SerializeField] GameObject[] m_ModeDecorations;
        EMFSignalController m_Emf;
        ThermometerTool m_Thermometer;
        SpiritResponseTool m_Spirit;
        [SerializeField] CameraHorrorOverlay m_Overlay;
        [SerializeField] GhostOrbField m_Orbs;
        [SerializeField] FreezingBreathEffect m_ColdBreath;
        float m_Transition = 1f;
        float m_DisplayedEmf = 1f;
        float m_EmfJitter;
        float m_NextEmfJitterTime;
        float m_VisualActivity;
        float m_NextLegacySuppressionScan;
        ScannerMode m_Mode;

        void Awake()
        {
            m_ModeManager ??= ScannerModeManager.Instance;
            m_GameState ??= GameStateManager.Instance;
            m_Camera ??= Camera.main != null
                ? Camera.main
                : FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (m_Camera == null || m_Rig == null)
            {
                Debug.LogError("InvestigationVisualSystem requires the scene-authored Investigation Visual Rig. Author it from Tools/PhasmophobiAR/Author Investigation Visual Rig.", this);
                enabled = false;
            }

            ConfigureHUDLayout();
        }

        void ConfigureHUDLayout()
        {
            if (m_Panel != null)
            {
                m_Panel.anchorMin = m_Panel.anchorMax = Vector2.zero;
                m_Panel.pivot = Vector2.zero;
                m_Panel.anchoredPosition = new Vector2(18f, 18f);
                m_Panel.sizeDelta = new Vector2(292f, 108f);
            }

            if (m_PanelGroup != null)
            {
                m_PanelGroup.interactable = false;
                m_PanelGroup.blocksRaycasts = false;
            }

            PositionText(m_Title, new Vector2(32f, 99f), new Vector2(210f, 20f), 12f);
            PositionText(m_Readout, new Vector2(32f, 50f), new Vector2(190f, 38f), 27f);
            PositionText(m_Status, new Vector2(32f, 28f), new Vector2(200f, 18f), 10f);

            var switchObject = GameObject.Find("Switch Mode Button");
            if (switchObject != null && switchObject.transform is RectTransform switchRect)
            {
                switchRect.anchorMin = switchRect.anchorMax = Vector2.zero;
                switchRect.pivot = Vector2.zero;
                switchRect.anchoredPosition = new Vector2(222f, 28f);
                switchRect.sizeDelta = new Vector2(78f, 30f);
                var label = switchObject.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSizeMax = 11f;
            }

            var journalObject = GameObject.Find("Open Journal Button");
            if (journalObject != null && journalObject.transform is RectTransform journalRect)
            {
                journalRect.anchorMin = journalRect.anchorMax = Vector2.one;
                journalRect.pivot = Vector2.one;
                journalRect.anchoredPosition = new Vector2(-18f, -18f);
                journalRect.sizeDelta = new Vector2(86f, 34f);
                var label = journalObject.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = 13f;
            }
        }

        static void PositionText(TMP_Text text, Vector2 position, Vector2 size, float fontSize)
        {
            if (text == null || text.rectTransform == null) return;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
        }

        void OnEnable()
        {
            if (m_ModeManager != null) m_ModeManager.ModeChanged += OnModeChanged;
            if (m_GameState != null) m_GameState.PhaseChanged += OnPhaseChanged;
        }

        void Start()
        {
            var legacyUI = FindAnyObjectByType<ScannerModeUI>();
            if (legacyUI != null) legacyUI.SetLegacyLabelsVisible(false);
            m_Mode = m_ModeManager != null ? m_ModeManager.CurrentMode : ScannerMode.EMF;
            ApplyMode(true);
            ApplyPhase();
        }

        void OnDisable()
        {
            if (m_ModeManager != null) m_ModeManager.ModeChanged -= OnModeChanged;
            if (m_GameState != null) m_GameState.PhaseChanged -= OnPhaseChanged;
            var legacyUI = FindAnyObjectByType<ScannerModeUI>();
            if (legacyUI != null) legacyUI.SetLegacyLabelsVisible(true);
        }

        void Update()
        {
            if (m_Rig == null || !m_Rig.gameObject.activeSelf) return;
            ResolveTools();
            m_Transition = Mathf.MoveTowards(m_Transition, 1f, Time.unscaledDeltaTime / Mathf.Max(.05f, m_TransitionSeconds));
            var eased = 1f - Mathf.Pow(1f - m_Transition, 3f);
            m_Panel.anchoredPosition = new Vector2(Mathf.Lerp(-32f, 18f, eased), 18f);
            m_Panel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-2.5f, 0f, eased));
            UpdateReadout();
            var analogFlicker = .96f + Mathf.PerlinNoise(Time.unscaledTime * 12f, 3.7f) * .04f;
            if (m_Mode == ScannerMode.EMF && Random.value < Time.unscaledDeltaTime * (.15f + m_VisualActivity * .7f))
                analogFlicker *= Random.Range(.72f, .9f);
            m_PanelGroup.alpha = eased * analogFlicker;
        }

        void OnModeChanged(ScannerMode mode)
        {
            m_Mode = mode;
            m_Transition = 0f;
            ApplyMode(false);
            if (m_TransitionAudio != null && m_ModeSwitchClip != null)
                m_TransitionAudio.PlayOneShot(m_ModeSwitchClip); // Optional physical switch/static foley hook.
        }

        void OnPhaseChanged(GamePhase _) => ApplyPhase();

        void ApplyPhase()
        {
            var active = m_GameState == null || m_GameState.CurrentPhase == GamePhase.Investigation;
            if (m_Rig != null) m_Rig.gameObject.SetActive(active);
            if (m_Overlay != null) m_Overlay.SetVisible(active);
        }

        void ApplyMode(bool immediate)
        {
            if (immediate) m_Transition = 1f;
            var color = ModeColor(m_Mode);
            foreach (var accent in m_Accents)
                if (accent != null) accent.color = color;
            m_Title.text = ModeName(m_Mode);
            m_Title.color = color;
            m_Readout.color = Color.Lerp(Color.white, color, .3f);
            m_Status.color = new Color(color.r, color.g, color.b, .72f);
            if (m_ModeDecorations != null)
                for (var i = 0; i < m_ModeDecorations.Length; i++)
                    if (m_ModeDecorations[i] != null) m_ModeDecorations[i].SetActive(i == (int)m_Mode);
            m_Overlay?.SetMode(m_Mode);
            m_Orbs?.SetModeActive(m_Mode == ScannerMode.Spectral);
            m_ColdBreath?.SetModeActive(m_Mode == ScannerMode.Thermal);
        }

        void UpdateReadout()
        {
            var activity = .08f;
            switch (m_Mode)
            {
                case ScannerMode.EMF:
                    activity = m_Emf != null ? m_Emf.CurrentValue : 0f;
                    if (Time.unscaledTime >= m_NextEmfJitterTime)
                    {
                        m_NextEmfJitterTime = Time.unscaledTime + Random.Range(.08f, .2f);
                        m_EmfJitter = Random.Range(-.045f, .045f) * Mathf.Lerp(.25f, 1f, activity);
                    }
                    var targetEmf = Mathf.Clamp(1f + activity * 4f + m_EmfJitter, 1f, 5f);
                    m_DisplayedEmf = Mathf.Lerp(m_DisplayedEmf, targetEmf, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f));
                    m_Readout.text = $"{m_DisplayedEmf:0.0}  mG";
                    m_Status.text = $"FIELD STRENGTH   {(activity * 100f):00}%";
                    break;
                case ScannerMode.Thermal:
                    var c = m_Thermometer != null ? m_Thermometer.CurrentCelsius : 19.5f;
                    activity = Mathf.InverseLerp(20f, -6f, c);
                    m_Readout.text = $"{c:0.0} C";
                    m_Status.text = c <= 0f ? "FREEZING DETECTED" : "AMBIENT SCAN";
                    break;
                case ScannerMode.Spectral:
                    activity = .28f + Mathf.PerlinNoise(Time.time * .3f, 7f) * .42f;
                    m_Readout.text = "IR  NIGHT VISION";
                    m_Status.text = "ORB FILTER ACTIVE";
                    break;
                default:
                    var response = m_Spirit != null ? m_Spirit.CurrentResponse : "SEARCHING";
                    var idle = response.Contains("static");
                    activity = idle ? .22f : .85f;
                    m_Readout.text = idle ? "SCANNING..." : response.ToUpperInvariant();
                    m_Status.text = idle ? "87.5 - 108.0 MHZ" : "VOICE LOCK";
                    break;
            }

            m_VisualActivity = activity;

            for (var i = 0; i < m_Bars.Length; i++)
            {
                var noise = Mathf.PerlinNoise(i * .71f, Time.time * (2f + activity * 8f));
                var height = Mathf.Lerp(4f, 31f, noise * activity);
                m_Bars[i].sizeDelta = new Vector2(4f, height);
                m_Bars[i].anchoredPosition = new Vector2(i * 8f, height * .5f);
            }
            m_Needle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(58f, -58f, activity));
            m_Overlay?.SetInterference(activity);
        }

        void ResolveTools()
        {
            m_Emf ??= FindAnyObjectByType<EMFSignalController>();
            m_Thermometer ??= FindAnyObjectByType<ThermometerTool>();
            m_Spirit ??= FindAnyObjectByType<SpiritResponseTool>();
            m_ColdBreath?.SetThermometer(m_Thermometer);
            // Marker tools can spawn after the presentation rig. Keep their prefab visuals visible;
            // the camera HUD supplements the physical tools instead of replacing them.
            if (Time.unscaledTime < m_NextLegacySuppressionScan) return;
            m_NextLegacySuppressionScan = Time.unscaledTime + .75f;
            foreach (var emfTool in FindObjectsByType<EMFReaderTool>(FindObjectsSortMode.None))
                emfTool.SetLegacyVisualsVisible(true);
            foreach (var thermometer in FindObjectsByType<ThermometerTool>(FindObjectsSortMode.None))
                thermometer.SetLegacyVisualsVisible(true);
            foreach (var spirit in FindObjectsByType<SpiritResponseTool>(FindObjectsSortMode.None))
                spirit.SetLegacyVisualsVisible(true);
        }

        static string ModeName(ScannerMode mode) => mode switch
        {
            ScannerMode.EMF => "FIELD METER  //  MK II",
            ScannerMode.Thermal => "THERMAL PROBE  //  MK II",
            ScannerMode.Spectral => "LOW-LIGHT CAMERA  //  IR",
            _ => "SPIRIT RECEIVER  //  BAND III"
        };

        static Color ModeColor(ScannerMode mode) => mode switch
        {
            ScannerMode.EMF => new Color(.63f, .72f, .32f),
            ScannerMode.Thermal => new Color(.45f, .63f, .58f),
            ScannerMode.Spectral => new Color(.43f, .53f, .58f),
            _ => new Color(.66f, .48f, .3f)
        };
    }
}
