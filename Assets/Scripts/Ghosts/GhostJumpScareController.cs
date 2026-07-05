using System.Collections;
using PhasmophobiAR.Game;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>A camera-relative scare that can strike while the player is moving or turning.</summary>
    [DisallowMultipleComponent]
    public sealed class GhostJumpScareController : MonoBehaviour
    {
        [Header("Frequency")]
        [SerializeField] float m_FirstScareDelaySeconds = 22f;
        [SerializeField] Vector2 m_CooldownRangeSeconds = new Vector2(38f, 65f);
        [SerializeField, Range(0, 10)] int m_MaxScaresPerInvestigation = 5;
        [SerializeField, Range(0f, 1f)] float m_MinimumTension = 0.2f;

        [Header("Presentation")]
        [SerializeField] float m_StartDistanceMeters = 1.65f;
        [SerializeField] float m_StopDistanceMeters = 0.48f;
        [SerializeField] float m_RushDurationSeconds = 0.42f;
        [SerializeField] float m_HoldDurationSeconds = 0.16f;
        [SerializeField] float m_DisappearDurationSeconds = 0.12f;
        [SerializeField] float m_VisualScale = 0.62f;
        [SerializeField] AudioClip m_JumpScareClip;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.72f;

        static bool s_IsAnyScarePlaying;

        GameStateManager m_GameState;
        GhostBehaviorController m_GhostBehavior;
        Transform m_ARCamera;
        AudioSource m_AudioSource;
        AudioClip m_FallbackClip;
        float m_NextScareTime;
        int m_ScareCount;
        bool m_IsConfigured;
        GameObject m_ActiveScareObject;
        HorrorDirector m_HorrorDirector;

        public bool IsScarePlaying => m_ActiveScareObject != null;

        public void Configure(GameStateManager gameState, Transform arCamera, GhostBehaviorController ghostBehavior, HorrorDirector horrorDirector = null)
        {
            m_GameState = gameState != null ? gameState : GameStateManager.Instance;
            m_ARCamera = arCamera != null ? arCamera : Camera.main != null ? Camera.main.transform : null;
            m_GhostBehavior = ghostBehavior != null ? ghostBehavior : GetComponent<GhostBehaviorController>();
            m_HorrorDirector = horrorDirector;
            EnsureAudioSource();
            ResetSchedule();
            m_IsConfigured = true;
        }

        void OnEnable()
        {
            if (m_GameState == null) m_GameState = GameStateManager.Instance;
            if (m_GameState != null) m_GameState.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            if (m_GameState != null) m_GameState.PhaseChanged -= OnPhaseChanged;
            StopAllCoroutines();
            ClearActiveScare();
        }

        void Update()
        {
            if (!m_IsConfigured || m_ARCamera == null || !CanArmScare())
                return;

            if (Time.unscaledTime >= m_NextScareTime)
                StartCoroutine(PlayJumpScare());
        }

        bool CanArmScare()
        {
            if (s_IsAnyScarePlaying || m_ScareCount >= m_MaxScaresPerInvestigation) return false;
            if (m_HorrorDirector == null || m_HorrorDirector.IsPlayingEvent || m_HorrorDirector.Tension < m_MinimumTension) return false;
            if (m_GameState != null && m_GameState.CurrentPhase != GamePhase.Investigation) return false;
            return m_GhostBehavior == null || m_GhostBehavior.RevealState != GhostRevealState.Captured;
        }

        IEnumerator PlayJumpScare()
        {
            s_IsAnyScarePlaying = true;
            m_ScareCount++;
            ScheduleNextScare();

            var selectedPrefab = m_HorrorDirector != null ? m_HorrorDirector.SelectedVisualPrefab : null;
            if (selectedPrefab == null)
            {
                var prefabs = GhostVisualCatalog.LoadPrefabs();
                if (prefabs.Length == 0)
                {
                    s_IsAnyScarePlaying = false;
                    yield break;
                }
                selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
            }

            var scareObject = Instantiate(selectedPrefab, m_ARCamera);
            m_ActiveScareObject = scareObject;
            scareObject.name = "JumpScareGhost";
            scareObject.transform.localRotation = Quaternion.Euler(0f, 180f, Random.Range(-3f, 3f));
            scareObject.transform.localPosition = Vector3.zero;
            scareObject.transform.localScale = Vector3.one * m_VisualScale;

            // The visual prefabs are centered around the full body. Aim the upper-head
            // region at screen center instead, and preserve that framing while scaling.
            var headOffset = GetHeadOffsetInCameraSpace(scareObject);
            var side = Random.Range(-0.12f, 0.12f);
            var height = Random.Range(-0.08f, 0.1f);
            const float startScaleRatio = 0.72f;
            var start = new Vector3(side, height, m_StartDistanceMeters) - headOffset * startScaleRatio;
            var stop = new Vector3(side * 0.15f, height * 0.15f, m_StopDistanceMeters) - headOffset;
            scareObject.transform.localPosition = start;
            scareObject.transform.localScale = Vector3.one * m_VisualScale * startScaleRatio;
            PlayScareAudio();

            var elapsed = 0f;
            while (elapsed < m_RushDurationSeconds && IsInvestigationActive())
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, m_RushDurationSeconds));
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                scareObject.transform.localPosition = Vector3.LerpUnclamped(start, stop, eased);
                scareObject.transform.localScale = Vector3.one * m_VisualScale * Mathf.Lerp(startScaleRatio, 1f, eased);
                yield return null;
            }

            if (IsInvestigationActive()) yield return new WaitForSecondsRealtime(m_HoldDurationSeconds);

            elapsed = 0f;
            var disappearStartScale = scareObject.transform.localScale;
            while (elapsed < m_DisappearDurationSeconds && IsInvestigationActive())
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, m_DisappearDurationSeconds));
                scareObject.transform.localScale = Vector3.Lerp(disappearStartScale, disappearStartScale * 0.82f, t);
                yield return null;
            }

            Destroy(scareObject);
            m_ActiveScareObject = null;
            s_IsAnyScarePlaying = false;
        }

        void PlayScareAudio()
        {
            EnsureAudioSource();
            if (m_JumpScareClip == null) m_FallbackClip ??= CreateFallbackScareClip();
            var clip = m_JumpScareClip != null ? m_JumpScareClip : m_FallbackClip;
            if (m_AudioSource != null && clip != null) m_AudioSource.PlayOneShot(clip, m_Volume);
        }

        void EnsureAudioSource()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null) m_AudioSource = gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.spatialBlend = 0f;
        }

        static AudioClip CreateFallbackScareClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.62f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = Mathf.Clamp01(t / 0.018f) * Mathf.Pow(1f - t / duration, 1.6f);
                var growl = Mathf.Sin(2f * Mathf.PI * (92f - t * 35f) * t) * 0.48f;
                var rasp = (Random.value * 2f - 1f) * Mathf.Lerp(0.72f, 0.15f, t / duration);
                samples[i] = Mathf.Clamp((growl + rasp) * envelope, -0.88f, 0.88f);
            }
            var clip = AudioClip.Create("Jump Scare Growl", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Investigation) ResetSchedule();
            else
            {
                StopAllCoroutines();
                ClearActiveScare();
            }
        }

        void ResetSchedule()
        {
            m_ScareCount = 0;
            m_NextScareTime = Time.unscaledTime + Mathf.Max(5f, m_FirstScareDelaySeconds);
        }

        void ScheduleNextScare()
        {
            var min = Mathf.Max(10f, Mathf.Min(m_CooldownRangeSeconds.x, m_CooldownRangeSeconds.y));
            var max = Mathf.Max(min, Mathf.Max(m_CooldownRangeSeconds.x, m_CooldownRangeSeconds.y));
            var aggressionMultiplier = m_HorrorDirector != null ? Mathf.Lerp(1f, 0.58f, m_HorrorDirector.Aggression) : 1f;
            m_NextScareTime = Time.unscaledTime + Random.Range(min, max) * aggressionMultiplier;
        }

        bool IsInvestigationActive() => m_GameState == null || m_GameState.CurrentPhase == GamePhase.Investigation;

        Vector3 GetHeadOffsetInCameraSpace(GameObject scareObject)
        {
            var renderers = scareObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return Vector3.zero;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Roughly 86% from the model's feet: eye/head level for both current meshes.
            var headWorldPosition = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.72f, bounds.center.z);
            return m_ARCamera.InverseTransformPoint(headWorldPosition);
        }

        void ClearActiveScare()
        {
            if (m_ActiveScareObject == null) return;
            Destroy(m_ActiveScareObject);
            m_ActiveScareObject = null;
            s_IsAnyScarePlaying = false;
        }
    }
}
