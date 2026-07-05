using PhasmophobiAR.Ghosts;
using PhasmophobiAR.Markers;
using PhasmophobiAR.Scanning;
using PhasmophobiAR.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneObjectMigration
{
    const string SessionKey = "PhasmophobiAR.SceneObjectMigration.Completed";

    [InitializeOnLoadMethod]
    static void RunOnceAfterReload()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (GameObject.Find("Scene Authored Runtime Objects") == null)
                Run();
        };
    }

    [MenuItem("Tools/PhasmophobiAR/Author All Runtime Objects Into Scene")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var root = GameObject.Find("PhasmophobiAR Game Root");
        if (root == null) throw new System.InvalidOperationException("Game root not found.");

        var authoredRoot = GameObject.Find("Scene Authored Runtime Objects") ?? new GameObject("Scene Authored Runtime Objects");
        authoredRoot.transform.SetParent(root.transform, false);

        ConfigureTools(authoredRoot.transform);
        ConfigureGhost(authoredRoot.transform);
        ConfigureTracePool(authoredRoot.transform);
        ConfigureRestartButton();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static void ConfigureTools(Transform parent)
    {
        var controller = Object.FindFirstObjectByType<MarkerToolSpawner>(FindObjectsInactive.Include);
        var serialized = new SerializedObject(controller);
        AssignPrefab(serialized, "m_EMFTool", "Assets/Resources/Tools/EMFReader.prefab", "Scene EMF Reader", parent);
        AssignPrefab(serialized, "m_ThermometerTool", "Assets/Resources/Tools/Thermometer.prefab", "Scene Thermometer", parent);
        AssignPrefab(serialized, "m_SpiritResponseTool", "Assets/Resources/Tools/SpiritResponse.prefab", "Scene Spirit Response", parent);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AssignPrefab(SerializedObject owner, string property, string path, string name, Transform parent)
    {
        var existing = GameObject.Find(name);
        if (existing == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            existing.name = name;
        }
        owner.FindProperty(property).objectReferenceValue = existing;
        existing.SetActive(false);
    }

    static void ConfigureGhost(Transform parent)
    {
        var controller = Object.FindFirstObjectByType<GhostSpawnController>(FindObjectsInactive.Include);
        var ghost = GameObject.Find("Scene Ghost");
        if (ghost == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Ghost.prefab");
            ghost = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            ghost.name = "Scene Ghost";
        }
        if (ghost.GetComponent<GhostBehaviorController>() == null) ghost.AddComponent<GhostBehaviorController>();
        if (ghost.GetComponent<GhostCaptureAudioController>() == null) ghost.AddComponent<GhostCaptureAudioController>();
        ghost.SetActive(false);

        var serialized = new SerializedObject(controller);
        var ghosts = serialized.FindProperty("m_SceneGhosts");
        ghosts.arraySize = 1;
        ghosts.GetArrayElementAtIndex(0).objectReferenceValue = ghost;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureTracePool(Transform parent)
    {
        var controller = Object.FindFirstObjectByType<SpectralTraceController>(FindObjectsInactive.Include);
        var pool = parent.Find("Spectral Trace Pool");
        if (pool == null)
        {
            var poolObject = new GameObject("Spectral Trace Pool");
            poolObject.transform.SetParent(parent, false);
            pool = poolObject.transform;
        }

        var serialized = new SerializedObject(controller);
        var traces = serialized.FindProperty("m_SceneTraces");
        traces.arraySize = 8;
        for (var i = 0; i < traces.arraySize; i++)
        {
            var name = $"Spectral Trace {i + 1:00}";
            var child = pool.Find(name)?.gameObject;
            if (child == null)
            {
                child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                child.name = name;
                child.transform.SetParent(pool, false);
                Object.DestroyImmediate(child.GetComponent<Collider>());
                var renderer = child.GetComponent<Renderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.sharedMaterial.SetColor("_BaseColor", new Color(.6f, .9f, 1f, .8f));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            child.SetActive(false);
            traces.GetArrayElementAtIndex(i).objectReferenceValue = child;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureRestartButton()
    {
        var journal = Object.FindFirstObjectByType<GhostJournalUI>(FindObjectsInactive.Include);
        var serialized = new SerializedObject(journal);
        var restartProperty = serialized.FindProperty("m_RestartButton");
        if (restartProperty.objectReferenceValue == null)
        {
            var close = serialized.FindProperty("m_CloseButton").objectReferenceValue as Button;
            var restart = Object.Instantiate(close.gameObject, close.transform.parent);
            restart.name = "Restart Journal Button";
            var rect = restart.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(86f, -34f);
            rect.sizeDelta = new Vector2(84f, 24f);
            restart.GetComponentInChildren<TMP_Text>(true).text = "Restart";
            var button = restart.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            restartProperty.objectReferenceValue = button;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
