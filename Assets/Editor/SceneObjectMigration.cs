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
    const string UILayoutSessionKey = "PhasmophobiAR.CleanHUDLayout.V1";

    [InitializeOnLoadMethod]
    static void RunOnceAfterReload()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                if (GameObject.Find("Scene Authored Runtime Objects") == null)
                    Run();
            };
        }

        if (!SessionState.GetBool(UILayoutSessionKey, false))
        {
            SessionState.SetBool(UILayoutSessionKey, true);
            EditorApplication.delayCall += ApplyCleanHUDLayout;
        }
    }

    [MenuItem("Tools/PhasmophobiAR/Author All Runtime Objects Into Scene")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var root = GameObject.Find("PhasmophobiAR Game Root");
        if (root == null) throw new System.InvalidOperationException("Game root not found.");

        var authoredRoot = GameObject.Find("Scene Authored Runtime Objects") ?? new GameObject("Scene Authored Runtime Objects");
        authoredRoot.transform.SetParent(root.transform, false);

        ConfigureGhost(authoredRoot.transform);
        ConfigureTracePool(authoredRoot.transform);
        ConfigureRestartButton();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
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

    [MenuItem("Tools/PhasmophobiAR/Apply Clean Investigation HUD Layout")]
    public static void ApplyCleanHUDLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ApplyCleanHUDLayout;
            return;
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        ConfigureInvestigationHUD();
        ConfigureCaptureHUD();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Applied clean investigation HUD layout directly to SampleScene.");
    }

    static void ConfigureInvestigationHUD()
    {
        var visualSystem = Object.FindAnyObjectByType<InvestigationVisualSystem>(FindObjectsInactive.Include);
        var serialized = new SerializedObject(visualSystem);
        var panel = serialized.FindProperty("m_Panel").objectReferenceValue as RectTransform;
        var group = serialized.FindProperty("m_PanelGroup").objectReferenceValue as CanvasGroup;
        var title = serialized.FindProperty("m_Title").objectReferenceValue as TMP_Text;
        var readout = serialized.FindProperty("m_Readout").objectReferenceValue as TMP_Text;
        var status = serialized.FindProperty("m_Status").objectReferenceValue as TMP_Text;

        SetRect(panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(16f, 16f), new Vector2(270f, 94f));
        if (panel != null)
        {
            panel.localRotation = Quaternion.identity;
            for (var i = 0; i < panel.childCount; i++)
                panel.GetChild(i).gameObject.SetActive(false);
            var background = panel.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(.018f, .024f, .022f, .82f);
                background.raycastTarget = false;
            }
        }
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        ConfigureText(title, new Vector2(28f, 84f), new Vector2(180f, 16f), 10f, TextAlignmentOptions.Left);
        ConfigureText(readout, new Vector2(28f, 47f), new Vector2(168f, 32f), 23f, TextAlignmentOptions.Left);
        ConfigureText(status, new Vector2(28f, 27f), new Vector2(174f, 15f), 9f, TextAlignmentOptions.Left);

        var teal = new Color(.36f, .95f, .76f, 1f);
        if (title != null) title.color = teal;
        if (readout != null) readout.color = Color.white;
        if (status != null) status.color = new Color(teal.r, teal.g, teal.b, .78f);

        var switchObject = GameObject.Find("Switch Mode Button");
        if (switchObject != null)
        {
            SetRect(switchObject.transform as RectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(204f, 26f), new Vector2(72f, 27f));
            var label = switchObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "Switch";
                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
                label.fontSizeMax = 10f;
            }
        }

        var journalObject = GameObject.Find("Open Journal Button");
        if (journalObject != null)
        {
            SetRect(journalObject.transform as RectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-18f, -18f), new Vector2(86f, 34f));
            var label = journalObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.fontSize = 13f;
        }
    }

    static void ConfigureCaptureHUD()
    {
        var captureUI = Object.FindAnyObjectByType<GhostCaptureUI>(FindObjectsInactive.Include);
        var serialized = new SerializedObject(captureUI);
        var rootObject = serialized.FindProperty("m_CaptureRoot").objectReferenceValue as GameObject;
        var progress = serialized.FindProperty("m_CaptureProgressSlider").objectReferenceValue as Slider;
        var progressText = serialized.FindProperty("m_CaptureProgressText").objectReferenceValue as TMP_Text;
        if (rootObject == null) return;

        SetRect(rootObject.transform as RectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -18f), new Vector2(360f, 74f));
        var background = rootObject.GetComponent<Image>();
        if (background != null) background.color = new Color(.018f, .025f, .023f, .78f);

        TMP_Text stateText = null;
        foreach (var text in rootObject.GetComponentsInChildren<TMP_Text>(true))
        {
            text.raycastTarget = false;
            if (text.name == "Ghost Capture")
                ConfigureText(text, new Vector2(14f, -11f), new Vector2(110f, 18f), 11f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f));
            else if (text.name == "Capture Text")
                text.gameObject.SetActive(false);
            else if (text.name == "Instruction Text")
                stateText = text;
        }

        ConfigureText(progressText, new Vector2(-14f, -11f), new Vector2(100f, 18f), 11f, TextAlignmentOptions.Right, Vector2.one, Vector2.one);
        ConfigureText(stateText, new Vector2(0f, -34f), new Vector2(320f, 20f), 12f, TextAlignmentOptions.Center, new Vector2(.5f, 1f), new Vector2(.5f, 1f));
        if (progress != null)
            SetRect(progress.transform as RectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 9f), new Vector2(328f, 6f));

        serialized.FindProperty("m_CaptureStateText").objectReferenceValue = stateText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Vector2? anchor = null, Vector2? pivot = null)
    {
        if (text == null) return;
        var selectedAnchor = anchor ?? Vector2.zero;
        SetRect(text.rectTransform, selectedAnchor, selectedAnchor, pivot ?? Vector2.zero, position, size);
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.alignment = alignment;
        text.raycastTarget = false;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
