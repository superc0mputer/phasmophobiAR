using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GhostCaptureAudioController : MonoBehaviour
    {
        [SerializeField]
        GhostRevealCaptureController m_CaptureController;

        [SerializeField]
        float m_SuccessToneDurationSeconds = 0.22f;

        [SerializeField]
        float m_InterruptedToneDurationSeconds = 0.35f;

        [SerializeField]
        float m_SuccessToneFrequency = 920f;

        [SerializeField]
        float m_InterruptedToneFrequency = 170f;

        AudioSource m_AudioSource;
        AudioClip m_SuccessClip;
        AudioClip m_InterruptedClip;

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.spatialBlend = 1f;
            m_AudioSource.rolloffMode = AudioRolloffMode.Linear;
            m_AudioSource.minDistance = 0.2f;
            m_AudioSource.maxDistance = 4f;

            m_SuccessClip = CreateToneClip("Capture Success", m_SuccessToneFrequency, m_SuccessToneDurationSeconds, 0.85f, false);
            m_InterruptedClip = CreateToneClip("Capture Interrupted", m_InterruptedToneFrequency, m_InterruptedToneDurationSeconds, 0.9f, true);
        }

        void OnEnable()
        {
            ResolveReferences();

            if (m_CaptureController != null)
            {
                m_CaptureController.CaptureSucceeded -= OnCaptureSucceeded;
                m_CaptureController.CaptureSucceeded += OnCaptureSucceeded;
                m_CaptureController.CaptureInterrupted -= OnCaptureInterrupted;
                m_CaptureController.CaptureInterrupted += OnCaptureInterrupted;
            }
        }

        void OnDisable()
        {
            if (m_CaptureController != null)
            {
                m_CaptureController.CaptureSucceeded -= OnCaptureSucceeded;
                m_CaptureController.CaptureInterrupted -= OnCaptureInterrupted;
            }
        }

        public void Configure(GhostRevealCaptureController captureController)
        {
            m_CaptureController = captureController ?? m_CaptureController;
        }

        void ResolveReferences()
        {
            if (m_CaptureController == null)
                m_CaptureController = GetComponent<GhostRevealCaptureController>();
        }

        void OnCaptureSucceeded()
        {
            PlayClip(m_SuccessClip, 0.75f);
        }

        void OnCaptureInterrupted(string reason)
        {
            PlayClip(m_InterruptedClip, 0.65f);
        }

        void PlayClip(AudioClip clip, float volume)
        {
            if (m_AudioSource == null || clip == null)
                return;

            m_AudioSource.PlayOneShot(clip, volume);
        }

        static AudioClip CreateToneClip(string name, float frequency, float durationSeconds, float volume, bool noiseTail)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds * sampleRate));
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = Mathf.Clamp01(1f - t / durationSeconds);
                var wave = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;

                if (noiseTail && t > durationSeconds * 0.65f)
                    wave += (Random.value - 0.5f) * 0.35f * envelope;

                samples[i] = wave * volume;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}