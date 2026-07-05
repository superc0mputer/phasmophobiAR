using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Chooses a finished visual prefab without consulting the gameplay ghost type.</summary>
    public sealed class GhostVisualRandomizer : MonoBehaviour
    {
        const string VisualPrefabResourcesPath = "Ghosts/Prefabs";

        public string SelectedVisualName { get; private set; }

        public bool Randomize()
        {
            var visualPrefabs = Resources.LoadAll<GameObject>(VisualPrefabResourcesPath);
            if (visualPrefabs == null || visualPrefabs.Length == 0)
            {
                Debug.LogError($"No ghost visual prefabs found in Resources/{VisualPrefabResourcesPath}.", this);
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
}
