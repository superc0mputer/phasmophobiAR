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
        [SerializeField] Color m_FootprintColor = new Color(0.62f, 0.9f, 1f, 0.48f);
        [SerializeField] Vector2 m_FootprintSizeMeters = new Vector2(0.22f, 0.42f);
        [SerializeField, Min(0.1f)] float m_FootprintLifetimeSeconds = 8.5f;
        [SerializeField, Min(0f)] float m_FootprintFadeSeconds = 3.25f;
        [SerializeField, Min(0f)] float m_FootprintHoverHeightMeters = 0.06f;
        [SerializeField] Color m_FootprintGlowColor = new Color(0.78f, 0.98f, 1f, 0.18f);

        GameObject m_AudioObject;
        AudioClip m_GeneratedPlaceholder;
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

            var stepCount = Random.Range(
                Mathf.Min(m_StepCountRange.x, m_StepCountRange.y),
                Mathf.Max(m_StepCountRange.x, m_StepCountRange.y) + 1);
            var startDistance = Random.Range(m_StartDistanceRangeMeters.x, m_StartDistanceRangeMeters.y);
            var randomAngle = Random.Range(0f, Mathf.PI * 2f);
            var radial = new Vector3(Mathf.Sin(randomAngle), 0f, Mathf.Cos(randomAngle));
            radial = Vector3.ProjectOnPlane(radial, Vector3.up).normalized;
            if (radial.sqrMagnitude < 0.001f)
                radial = Vector3.forward;

            var start = cameraTransform.position + radial * startDistance;
            var approach = Random.value <= m_ApproachPlayerChance;
            var direction = approach
                ? -radial
                : Vector3.Cross(Vector3.up, radial) * (Random.value < 0.5f ? -1f : 1f);

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
            EnsureFootprintMesh();
            var footprint = new GameObject(leftFoot ? "Left Ghost Footprint" : "Right Ghost Footprint");
            footprint.transform.SetPositionAndRotation(
                position + Vector3.up * m_FootprintHoverHeightMeters,
                Quaternion.LookRotation(walkingDirection.normalized, Vector3.up));
            footprint.transform.localScale = new Vector3(
                (leftFoot ? -1f : 1f) * Mathf.Max(0.01f, m_FootprintSizeMeters.x),
                1f,
                Mathf.Max(0.01f, m_FootprintSizeMeters.y));

            footprint.AddComponent<MeshFilter>().sharedMesh = m_FootprintMesh;
            var renderer = footprint.AddComponent<MeshRenderer>();
            var material = CreateFootprintMaterial(m_FootprintColor);
            material.renderQueue = (int)RenderQueue.Transparent + 50;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            m_ActiveFootprints.Add(footprint);
            StartCoroutine(FadeFootprint(footprint, material));
        }

        void EnsureFootprintMesh()
        {
            if (m_FootprintMesh != null)
                return;

            var outline = new[]
            {
                new Vector2(0f, -1.0f),
                new Vector2(0.28f, -0.9f),
                new Vector2(0.38f, -0.66f),
                new Vector2(0.33f, -0.28f),
                new Vector2(0.22f, 0.06f),
                new Vector2(0.38f, 0.34f),
                new Vector2(0.42f, 0.52f),
                new Vector2(0.34f, 0.76f),
                new Vector2(0.2f, 0.98f),
                new Vector2(0.08f, 1.08f),
                new Vector2(-0.04f, 1.12f),
                new Vector2(-0.16f, 1.08f),
                new Vector2(-0.28f, 0.98f),
                new Vector2(-0.38f, 0.84f),
                new Vector2(-0.44f, 0.66f),
                new Vector2(-0.47f, 0.46f),
                new Vector2(-0.34f, 0.18f),
                new Vector2(-0.19f, -0.06f),
                new Vector2(-0.16f, -0.28f),
                new Vector2(-0.22f, -0.56f),
                new Vector2(-0.16f, -0.82f)
            };

            var vertices = new Vector3[outline.Length + 1];
            var uvs = new Vector2[vertices.Length];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (var i = 0; i < outline.Length; i++)
            {
                vertices[i + 1] = new Vector3(outline[i].x, 0f, outline[i].y * 0.5f);
                uvs[i + 1] = new Vector2(outline[i].x * 0.5f + 0.5f, outline[i].y * 0.25f + 0.5f);
            }

            var triangles = new int[outline.Length * 3];
            for (var i = 0; i < outline.Length; i++)
            {
                var next = i + 1 < outline.Length ? i + 2 : 1;
                var tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = next;
            }

            m_FootprintMesh = new Mesh { name = "Runtime Ghost Footprint" };
            m_FootprintMesh.vertices = vertices;
            m_FootprintMesh.uv = uvs;
            m_FootprintMesh.triangles = triangles;
            m_FootprintMesh.RecalculateNormals();
            m_FootprintMesh.RecalculateBounds();
        }

        IEnumerator FadeFootprint(GameObject footprint, Material material)
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
                    ApplyFootprintColor(material, color);
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

        Material CreateFootprintMaterial(Color color)
        {
            var shader = Shader.Find("PhasmophobiAR/Ghost Footprint");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning("No suitable shader found for ghost footprints.", this);
                return new Material(Shader.Find("Standard"));
            }

            var material = new Material(shader);
            ApplyFootprintMaterial(material, color);
            return material;
        }

        void ApplyFootprintMaterial(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_GlowColor"))
                material.SetColor("_GlowColor", m_FootprintGlowColor);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        static void ApplyFootprintColor(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
        }
    }
}
