using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class GhostCaptureUI : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GhostRevealCaptureController m_CaptureController;

        [SerializeField]
        GameObject m_CaptureRoot;

        [SerializeField]
        Slider m_CaptureProgressSlider;

        [SerializeField]
        TMP_Text m_CaptureProgressText;

        [SerializeField]
        TMP_Text m_CaptureStateText;

        bool m_IsSubscribed;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            ResolveStateText();
            ConfigureLayout();
        }

        void ResolveStateText()
        {
            if (m_CaptureStateText != null || m_CaptureRoot == null)
                return;

            foreach (var text in m_CaptureRoot.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "Instruction Text")
                {
                    m_CaptureStateText = text;
                    break;
                }
        }

        void ConfigureLayout()
        {
            if (m_CaptureRoot == null) return;
            if (m_CaptureRoot.transform is RectTransform root)
            {
                root.anchorMin = root.anchorMax = new Vector2(.5f, 1f);
                root.pivot = new Vector2(.5f, 1f);
                root.anchoredPosition = new Vector2(0f, -18f);
                root.sizeDelta = new Vector2(360f, 74f);
            }

            var background = m_CaptureRoot.GetComponent<Image>();
            if (background != null)
                background.color = new Color(.018f, .025f, .023f, .78f);

            foreach (var text in m_CaptureRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                text.raycastTarget = false;
                if (text.name == "Ghost Capture")
                    Place(text.rectTransform, new Vector2(14f, -11f), new Vector2(110f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f));
                else if (text.name == "Capture Text")
                    text.gameObject.SetActive(false);
            }

            if (m_CaptureProgressText != null)
            {
                Place(m_CaptureProgressText.rectTransform, new Vector2(-14f, -11f), new Vector2(100f, 18f), Vector2.one, Vector2.one);
                m_CaptureProgressText.alignment = TextAlignmentOptions.Right;
                m_CaptureProgressText.fontSize = 11f;
            }

            if (m_CaptureStateText != null)
            {
                Place(m_CaptureStateText.rectTransform, new Vector2(0f, -34f), new Vector2(320f, 20f), new Vector2(.5f, 1f), new Vector2(.5f, 1f));
                m_CaptureStateText.alignment = TextAlignmentOptions.Center;
                m_CaptureStateText.fontSize = 12f;
            }

            if (m_CaptureProgressSlider != null)
            {
                var sliderRect = m_CaptureProgressSlider.transform as RectTransform;
                Place(sliderRect, new Vector2(0f, 9f), new Vector2(328f, 6f), new Vector2(.5f, 0f), new Vector2(.5f, 0f));
            }
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor, Vector2 pivot)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        void OnEnable()
        {
            Subscribe();
            if (m_GameStateManager != null)
                OnPhaseChanged(m_GameStateManager.CurrentPhase);

            UpdateUI();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GameStateManager gameStateManager,
            GhostRevealCaptureController captureController,
            GameObject captureRoot,
            Slider captureProgressSlider,
            TMP_Text captureProgressText,
            TMP_Text captureStateText)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
            m_CaptureController = captureController ?? m_CaptureController;
            m_CaptureRoot = captureRoot ?? m_CaptureRoot;
            m_CaptureProgressSlider = captureProgressSlider ?? m_CaptureProgressSlider;
            m_CaptureProgressText = captureProgressText ?? m_CaptureProgressText;
            m_CaptureStateText = captureStateText ?? m_CaptureStateText;

            UpdateUI();
        }

        void Update()
        {
            UpdateUI();
        }

        void Subscribe()
        {
            if (m_IsSubscribed)
                return;

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_GameStateManager != null)
            {
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
                m_GameStateManager.PhaseChanged += OnPhaseChanged;
            }

            m_IsSubscribed = true;
        }

        void Unsubscribe()
        {
            if (!m_IsSubscribed)
                return;

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;

            m_IsSubscribed = false;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (m_CaptureRoot != null)
                m_CaptureRoot.SetActive(phase == GamePhase.Investigation);

            UpdateUI();
        }

        void UpdateUI()
        {
            if (m_CaptureController == null)
                m_CaptureController = FindAnyObjectByType<GhostRevealCaptureController>();

            var active = m_GameStateManager == null || m_GameStateManager.CurrentPhase == GamePhase.Investigation;
            if (m_CaptureRoot != null)
                m_CaptureRoot.SetActive(active);

            if (m_CaptureController == null || !active)
                return;

            var progress = m_CaptureController.CaptureProgress;
            if (m_CaptureProgressSlider != null)
            {
                m_CaptureProgressSlider.gameObject.SetActive(true);
                m_CaptureProgressSlider.value = progress;
            }

            if (m_CaptureProgressText != null)
            {
                m_CaptureProgressText.gameObject.SetActive(true);
                m_CaptureProgressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            if (m_CaptureStateText != null)
            {
                m_CaptureStateText.gameObject.SetActive(true);
                m_CaptureStateText.text = m_CaptureController.CurrentState switch
                {
                    GhostRevealState.Hidden => "Hold still to reveal",
                    GhostRevealState.PartialReveal => "Ghost is stirring",
                    GhostRevealState.Revealed => "Center the ghost",
                    GhostRevealState.Capturing => "Capturing...",
                    GhostRevealState.Captured => "Captured",
                    _ => ""
                };
            }
        }
    }
}
