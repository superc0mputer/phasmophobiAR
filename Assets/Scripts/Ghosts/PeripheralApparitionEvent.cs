using System.Collections;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Places a transient ghost at the edge of the AR view; noticing it makes it vanish.</summary>
    public sealed class PeripheralApparitionEvent : HorrorEvent
    {
        [SerializeField] Vector2 m_DistanceRangeMeters = new Vector2(2.4f, 3.5f);
        [SerializeField] float m_EdgeViewportPosition = 0.94f;
        [SerializeField] float m_MinimumVisibleSeconds = 0.2f;
        [SerializeField] float m_MaximumVisibleSeconds = 1.2f;
        [SerializeField] float m_VisualScale = 0.22f;
        [SerializeField, Range(0f, 1f)] float m_Visibility = 0.32f;
        [SerializeField] AudioClip m_PlaceholderSound;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.35f;

        GameObject m_ActiveApparition;

        internal override bool CanTrigger(HorrorDirector director)
        {
            if (!base.CanTrigger(director) || director.SelectedVisualPrefab == null) return false;
            var behavior = director.GhostBehavior;
            return behavior == null || behavior.RevealState == GhostRevealState.Hidden;
        }

        protected override IEnumerator Play(HorrorDirector director)
        {
            var cameraTransform = director.ARCamera;
            var camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            var selectedPrefab = director.SelectedVisualPrefab;
            if (cameraTransform == null || camera == null || selectedPrefab == null) yield break;

            var side = Random.value < 0.5f ? -1f : 1f;
            var distance = Random.Range(m_DistanceRangeMeters.x, m_DistanceRangeMeters.y);
            var viewportX = side < 0f ? 1f - m_EdgeViewportPosition : m_EdgeViewportPosition;
            var position = camera.ViewportToWorldPoint(new Vector3(viewportX, Random.Range(0.38f, 0.62f), distance));

            m_ActiveApparition = Instantiate(selectedPrefab, position, Quaternion.identity);
            m_ActiveApparition.name = "Peripheral Apparition";
            m_ActiveApparition.transform.rotation = Quaternion.LookRotation(cameraTransform.position - position, Vector3.up);
            m_ActiveApparition.transform.localScale = Vector3.one * m_VisualScale;
            MakeSubtle(m_ActiveApparition);
            PlayPlaceholderSound(cameraTransform);

            var elapsed = 0f;
            while (elapsed < m_MaximumVisibleSeconds && IsInvestigationActive(director))
            {
                elapsed += Time.unscaledDeltaTime;
                var viewport = camera.WorldToViewportPoint(GetApparitionCenter(m_ActiveApparition));
                var playerLookedAtIt = elapsed >= m_MinimumVisibleSeconds
                    && viewport.z > 0f
                    && viewport.x > 0.28f && viewport.x < 0.72f
                    && viewport.y > 0.2f && viewport.y < 0.8f;
                if (playerLookedAtIt) break;
                yield return null;
            }

            ClearApparition();
            director.AddTension(0.045f);
        }

        void OnDisable() => ClearApparition();
        protected override void OnCancel() => ClearApparition();

        void ClearApparition()
        {
            if (m_ActiveApparition == null) return;
            Destroy(m_ActiveApparition);
            m_ActiveApparition = null;
        }

        static Vector3 GetApparitionCenter(GameObject apparition)
        {
            var renderers = apparition.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return apparition.transform.position;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        void MakeSubtle(GameObject apparition)
        {
            var tint = new Color(m_Visibility, m_Visibility, m_Visibility, 1f);
            foreach (var renderer in apparition.GetComponentsInChildren<Renderer>(true))
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                block.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(block);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void PlayPlaceholderSound(Transform cameraTransform)
        {
            if (m_PlaceholderSound == null) return;
            AudioSource.PlayClipAtPoint(m_PlaceholderSound, cameraTransform.position, m_Volume);
        }

        static bool IsInvestigationActive(HorrorDirector director)
        {
            return director != null && director.isActiveAndEnabled;
        }
    }
}
