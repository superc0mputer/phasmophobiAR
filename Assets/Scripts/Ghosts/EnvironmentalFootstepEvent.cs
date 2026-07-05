using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        [Header("Visible footprints")]
        [SerializeField] Color m_FootprintColor = new Color(0.16f, 0.2f, 0.26f, 0.92f);
        [SerializeField] Vector2 m_FootprintSizeMeters = new Vector2(0.19f, 0.38f);
        [SerializeField, Min(0.1f)] float m_FootprintLifetimeSeconds = 7.5f;
        [SerializeField, Min(0f)] float m_FootprintFadeSeconds = 2.75f;

        GameObject m_AudioObject;
        AudioClip m_GeneratedPlaceholder;
        Texture2D m_FootprintTexture;
        Mesh m_FootprintMesh;
        readonly List<GameObject> m_ActiveFootprints = new List<GameObject>();

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
                var floorPosition = FindFloorPosition(desiredPosition, cameraTransform, director.GhostBehavior);
                m_AudioObject.transform.position = floorPosition;
                SpawnFootprint(floorPosition, direction, i % 2 == 0);
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

        void SpawnFootprint(Vector3 position, Vector3 walkingDirection, bool leftFoot)
        {
            var shader = Shader.Find("PhasmophobiAR/Ghost Footprint");
            if (shader == null)
            {
                Debug.LogWarning("Ghost footprint shader could not be found.", this);
                return;
            }

            EnsureFootprintResources();
            var footprint = new GameObject(leftFoot ? "Left Ghost Footprint" : "Right Ghost Footprint");
            footprint.transform.SetPositionAndRotation(position, Quaternion.LookRotation(walkingDirection.normalized, Vector3.up));
            footprint.transform.localScale = new Vector3(
                (leftFoot ? -1f : 1f) * Mathf.Max(0.01f, m_FootprintSizeMeters.x),
                1f,
                Mathf.Max(0.01f, m_FootprintSizeMeters.y));

            footprint.AddComponent<MeshFilter>().sharedMesh = m_FootprintMesh;
            var renderer = footprint.AddComponent<MeshRenderer>();
            var material = new Material(shader);
            material.SetTexture("_BaseMap", m_FootprintTexture);
            material.SetColor("_BaseColor", m_FootprintColor);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            m_ActiveFootprints.Add(footprint);
            StartCoroutine(FadeFootprint(footprint, renderer, material));
        }

        void EnsureFootprintResources()
        {
            if (m_FootprintMesh == null)
            {
                m_FootprintMesh = new Mesh { name = "Runtime Ghost Footprint" };
                m_FootprintMesh.vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f)
                };
                m_FootprintMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
                m_FootprintMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
                m_FootprintMesh.RecalculateBounds();
            }

            if (m_FootprintTexture != null) return;
            const int width = 64;
            const int height = 128;
            m_FootprintTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Runtime Ghost Footprint Mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width * 2f - 1f;
                var v = (y + 0.5f) / height * 2f - 1f;
                var heel = EllipseMask(u, v, 0f, -0.62f, 0.43f, 0.31f);
                var arch = EllipseMask(u, v, -0.11f, -0.12f, 0.34f, 0.48f);
                var ball = EllipseMask(u, v, 0.05f, 0.32f, 0.49f, 0.34f);
                var toe = EllipseMask(u, v, 0.18f, 0.72f, 0.28f, 0.20f);
                var smallToe = EllipseMask(u, v, -0.22f, 0.64f, 0.18f, 0.14f);
                var alpha = Mathf.Clamp01(Mathf.Max(heel, arch, ball, toe, smallToe));
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            m_FootprintTexture.SetPixels32(pixels);
            m_FootprintTexture.Apply(false, true);
        }

        static float EllipseMask(float x, float y, float centerX, float centerY, float radiusX, float radiusY)
        {
            var distance = Mathf.Sqrt(
                Mathf.Pow((x - centerX) / radiusX, 2f) +
                Mathf.Pow((y - centerY) / radiusY, 2f));
            return 1f - Mathf.SmoothStep(0.72f, 1f, distance);
        }

        IEnumerator FadeFootprint(GameObject footprint, Renderer renderer, Material material)
        {
            var lifetime = Mathf.Max(0.1f, m_FootprintLifetimeSeconds);
            var fadeDuration = Mathf.Min(lifetime, Mathf.Max(0f, m_FootprintFadeSeconds));
            var fadeStart = lifetime - fadeDuration;
            var elapsed = 0f;
            while (elapsed < lifetime && footprint != null)
            {
                elapsed += Time.unscaledDeltaTime;
                if (fadeDuration > 0f && elapsed > fadeStart)
                {
                    var color = m_FootprintColor;
                    color.a *= 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeDuration);
                    material.SetColor("_BaseColor", color);
                }
                yield return null;
            }

            m_ActiveFootprints.Remove(footprint);
            if (footprint != null) Destroy(footprint);
            if (material != null) Destroy(material);
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

        protected override void OnCancel()
        {
            ClearAudioObject();
            ClearFootprints();
        }

        void OnDisable()
        {
            ClearAudioObject();
            ClearFootprints();
        }

        void OnDestroy()
        {
            if (m_FootprintTexture != null) Destroy(m_FootprintTexture);
            if (m_FootprintMesh != null) Destroy(m_FootprintMesh);
        }

        void ClearAudioObject()
        {
            if (m_AudioObject == null) return;
            Destroy(m_AudioObject);
            m_AudioObject = null;
        }

        void ClearFootprints()
        {
            foreach (var footprint in m_ActiveFootprints)
            {
                if (footprint == null) continue;
                var renderer = footprint.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    Destroy(renderer.sharedMaterial);
                Destroy(footprint);
            }
            m_ActiveFootprints.Clear();
        }
    }
}
