using System.Collections;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Plays a moving sequence of spatial footsteps across the estimated real-world floor.</summary>
    public sealed class EnvironmentalFootstepEvent : HorrorEvent
    {
        [Header("Footstep sequence")]
        [SerializeField] Vector2Int m_StepCountRange = new Vector2Int(4, 7);
        [SerializeField] Vector2 m_StepIntervalRangeSeconds = new Vector2(0.32f, 0.58f);
        [SerializeField] Vector2 m_StartDistanceRangeMeters = new Vector2(1.4f, 2.8f);
        [SerializeField] float m_StrideMeters = 0.48f;
        [SerializeField, Range(0f, 1f)] float m_ApproachPlayerChance = 0.65f;

        [Header("Floor placement")]
        [SerializeField] float m_RaycastHeightMeters = 1.5f;
        [SerializeField] float m_RaycastDistanceMeters = 4f;
        [SerializeField] float m_FallbackFloorBelowCameraMeters = 1.35f;

        [Header("Placeholder audio")]
        [SerializeField] AudioClip[] m_FootstepClips;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.85f;
        [SerializeField] Vector2 m_PitchRange = new Vector2(0.82f, 1.08f);
        [SerializeField, Range(0f, 1f)] float m_SpatialBlend = 0.65f;

        GameObject m_AudioObject;
        AudioClip m_GeneratedPlaceholder;

        protected override IEnumerator Play(HorrorDirector director)
        {
            var cameraTransform = director.ARCamera;
            if (cameraTransform == null) yield break;

            m_AudioObject = new GameObject("Environmental Footsteps");
            var source = m_AudioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = m_SpatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.35f;
            source.maxDistance = 7f;

            var stepCount = Random.Range(Mathf.Min(m_StepCountRange.x, m_StepCountRange.y), Mathf.Max(m_StepCountRange.x, m_StepCountRange.y) + 1);
            var startDistance = Random.Range(m_StartDistanceRangeMeters.x, m_StartDistanceRangeMeters.y);
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            var start = cameraTransform.position + radial * startDistance;
            var approach = Random.value <= m_ApproachPlayerChance;
            var direction = approach ? -radial : Vector3.Cross(Vector3.up, radial) * (Random.value < 0.5f ? -1f : 1f);

            for (var i = 0; i < stepCount && director != null && director.isActiveAndEnabled; i++)
            {
                var lateralFoot = Vector3.Cross(Vector3.up, direction) * (i % 2 == 0 ? -0.11f : 0.11f);
                var desiredPosition = start + direction * (m_StrideMeters * i) + lateralFoot;
                m_AudioObject.transform.position = FindFloorPosition(desiredPosition, cameraTransform, director.GhostBehavior);
                source.pitch = Random.Range(Mathf.Min(m_PitchRange.x, m_PitchRange.y), Mathf.Max(m_PitchRange.x, m_PitchRange.y));
                var clip = GetFootstepClip();
                source.PlayOneShot(clip, m_Volume * Random.Range(0.82f, 1f));

                if (i < stepCount - 1)
                    yield return new WaitForSecondsRealtime(Random.Range(
                        Mathf.Min(m_StepIntervalRangeSeconds.x, m_StepIntervalRangeSeconds.y),
                        Mathf.Max(m_StepIntervalRangeSeconds.x, m_StepIntervalRangeSeconds.y)));
                else if (clip != null)
                    yield return new WaitForSecondsRealtime(clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch)));
            }

            ClearAudioObject();
            director.AddTension(0.035f);
        }

        Vector3 FindFloorPosition(Vector3 desiredPosition, Transform cameraTransform, GhostBehaviorController ghost)
        {
            var rayOrigin = desiredPosition + Vector3.up * m_RaycastHeightMeters;
            var hits = Physics.RaycastAll(rayOrigin, Vector3.down, m_RaycastDistanceMeters, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var foundFloor = false;
            var floorPoint = Vector3.zero;
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.normal.y < 0.65f || hit.point.y > cameraTransform.position.y + 0.3f)
                    continue;
                if (!foundFloor || hit.point.y < floorPoint.y)
                {
                    foundFloor = true;
                    floorPoint = hit.point;
                }
            }
            if (foundFloor) return floorPoint + Vector3.up * 0.025f;

            var fallbackY = ghost != null
                ? ghost.transform.position.y - 0.25f
                : cameraTransform.position.y - m_FallbackFloorBelowCameraMeters;
            desiredPosition.y = fallbackY;
            return desiredPosition;
        }

        AudioClip GetFootstepClip()
        {
            if (m_FootstepClips != null && m_FootstepClips.Length > 0)
            {
                var clip = m_FootstepClips[Random.Range(0, m_FootstepClips.Length)];
                if (clip != null) return clip;
            }
            m_GeneratedPlaceholder ??= CreatePlaceholderFootstep();
            return m_GeneratedPlaceholder;
        }

        static AudioClip CreatePlaceholderFootstep()
        {
            const int sampleRate = 44100;
            const float duration = 0.19f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / sampleRate;
                var attack = Mathf.Clamp01(t / 0.008f);
                var decay = Mathf.Exp(-t * 25f);
                var thump = Mathf.Sin(2f * Mathf.PI * (82f - t * 100f) * t) * 0.86f;
                var texture = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 34f);
                samples[i] = Mathf.Clamp((thump + texture) * attack * decay, -0.85f, 0.85f);
            }
            var clip = AudioClip.Create("Placeholder Floor Footstep", samples.Length, 1, sampleRate, false);
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
