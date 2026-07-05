using System.Collections;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Plays close breathing beside the tablet as an independent low-tension event.</summary>
    public sealed class CloseBreathingEvent : HorrorEvent
    {
        [SerializeField] Vector2 m_DurationRangeSeconds = new Vector2(2.8f, 4.8f);
        [SerializeField] AudioClip m_BreathingClip;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.52f;
        [SerializeField, Range(0f, 1f)] float m_SpatialBlend = 0.05f;
        [SerializeField, Range(0f, 1f)] float m_StereoPan = 0.48f;
        [SerializeField] Vector2 m_PitchRange = new Vector2(0.88f, 1.04f);

        GameObject m_AudioObject;
        AudioClip m_GeneratedPlaceholder;

        protected override IEnumerator Play(HorrorDirector director)
        {
            if (director.ARCamera == null) yield break;

            m_AudioObject = new GameObject("Close Breathing");
            m_AudioObject.transform.SetParent(director.ARCamera, false);
            m_AudioObject.transform.localPosition = new Vector3(Random.value < 0.5f ? -0.12f : 0.12f, -0.04f, 0.08f);

            var source = m_AudioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = m_SpatialBlend;
            source.panStereo = Mathf.Sign(m_AudioObject.transform.localPosition.x) * m_StereoPan;
            source.pitch = Random.Range(Mathf.Min(m_PitchRange.x, m_PitchRange.y), Mathf.Max(m_PitchRange.x, m_PitchRange.y));
            source.volume = m_Volume;
            source.clip = m_BreathingClip != null ? m_BreathingClip : GetPlaceholder();
            source.Play();

            var duration = Random.Range(Mathf.Min(m_DurationRangeSeconds.x, m_DurationRangeSeconds.y), Mathf.Max(m_DurationRangeSeconds.x, m_DurationRangeSeconds.y));
            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, duration));

            ClearAudioObject();
            director.AddTension(0.04f);
        }

        AudioClip GetPlaceholder()
        {
            m_GeneratedPlaceholder ??= CreatePlaceholderBreathing();
            return m_GeneratedPlaceholder;
        }

        static AudioClip CreatePlaceholderBreathing()
        {
            const int sampleRate = 44100;
            const float duration = 2.4f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            var filteredNoise = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                var cycle = t / duration;
                var inhale = Mathf.Sin(Mathf.PI * Mathf.Clamp01(cycle / 0.42f));
                var exhale = Mathf.Sin(Mathf.PI * Mathf.Clamp01((cycle - 0.5f) / 0.5f));
                var envelope = Mathf.Max(inhale * 0.62f, exhale * 0.9f);
                filteredNoise = Mathf.Lerp(filteredNoise, Random.value * 2f - 1f, 0.075f);
                var throat = Mathf.Sin(2f * Mathf.PI * 96f * t) * 0.055f;
                samples[i] = Mathf.Clamp((filteredNoise * 0.52f + throat) * envelope, -0.65f, 0.65f);
            }
            var clip = AudioClip.Create("Placeholder Close Breathing", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        protected override void OnCancel() => ClearAudioObject();
        void OnDisable() => ClearAudioObject();

        void ClearAudioObject()
        {
            if (m_AudioObject == null) return;
            Destroy(m_AudioObject);
            m_AudioObject = null;
        }
    }
}
