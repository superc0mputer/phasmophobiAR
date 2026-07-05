using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Chooses a finished visual prefab without consulting the gameplay ghost type.</summary>
    public sealed class GhostVisualRandomizer : MonoBehaviour
    {
        public string SelectedVisualName { get; private set; }
        public GameObject SelectedVisualPrefab { get; private set; }

        public bool Randomize()
        {
            var visualPrefabs = GhostVisualCatalog.LoadPrefabs();
            if (visualPrefabs.Length == 0)
            {
                Debug.LogError("No ghost visual prefabs were found in Resources/Ghosts/Prefabs.", this);
                return false;
            }

            ClearExistingVisuals(visualPrefabs);

            var selectedPrefab = visualPrefabs[Random.Range(0, visualPrefabs.Length)];
            SelectedVisualPrefab = selectedPrefab;
            var visual = Instantiate(selectedPrefab, transform);
            visual.name = selectedPrefab.name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            SelectedVisualName = selectedPrefab.name;
            return true;
        }

        void ClearExistingVisuals(GameObject[] visualPrefabs)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && IsGhostVisual(child.gameObject, visualPrefabs))
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            SelectedVisualName = null;
            SelectedVisualPrefab = null;
        }

        static bool IsGhostVisual(GameObject candidate, GameObject[] visualPrefabs)
        {
            if (candidate == null || visualPrefabs == null)
                return false;

            foreach (var prefab in visualPrefabs)
            {
                if (prefab != null && candidate.name == prefab.name)
                    return true;
            }

            return false;
        }

    }

    static class GhostVisualCatalog
    {
        const string VisualPrefabResourcesPath = "Ghosts/Prefabs";

        public static GameObject[] LoadPrefabs()
        {
            return Resources.LoadAll<GameObject>(VisualPrefabResourcesPath);
        }
    }
}
