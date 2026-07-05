using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Chooses a finished visual prefab without consulting the gameplay ghost type.</summary>
    public sealed class GhostVisualRandomizer : MonoBehaviour
    {
        public string SelectedVisualName { get; private set; }

        public bool Randomize()
        {
            var visualPrefabs = GhostVisualCatalog.LoadPrefabs();
            if (visualPrefabs.Length == 0)
            {
                Debug.LogError("No ghost visual prefabs were found in Resources/Ghosts/Prefabs.", this);
                return false;
            }

            var selectedPrefab = visualPrefabs[Random.Range(0, visualPrefabs.Length)];
            var visual = Instantiate(selectedPrefab, transform);
            visual.name = selectedPrefab.name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            SelectedVisualName = selectedPrefab.name;
            return true;
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
