using System;
using System.Collections.Generic;
using PhasmophobiAR.Ghosts;
using PhasmophobiAR.Scanning;
using PhasmophobiAR.Tools;
using PhasmophobiAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace PhasmophobiAR.Game
{
    public static class RoomScanRuntimeBootstrap
    {
        const string k_RootName = "PhasmophobiAR Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapCurrentScene()
        {
            SceneManager.sceneLoaded += (_, _) => Bootstrap();
            Bootstrap();
        }

        static void Bootstrap()
        {
            if (GameObject.Find(k_RootName) != null)
                return;

            var root = new GameObject(k_RootName);
            UnityEngine.Object.DontDestroyOnLoad(root);

            var gameStateManager = root.AddComponent<GameStateManager>();
            var arCamera = Camera.main;
            var planeManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();

            var scanController = root.AddComponent<RoomScanController>();
            scanController.Configure(gameStateManager, arCamera, planeManager);

            var ghostSpawnController = root.AddComponent<GhostSpawnController>();
            _ = ghostSpawnController;

            CreateHud(root.transform, gameStateManager, scanController);
            GateTemplatePlacement(root.transform, gameStateManager);
        }

        static void CreateHud(Transform parent, GameStateManager gameStateManager, RoomScanController scanController)
        {
            var canvasObject = new GameObject("Room Scan HUD", typeof(RectTransform));
            canvasObject.transform.SetParent(parent);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scanRoot = CreatePanel(canvasObject.transform, "Scan Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(520f, 128f));
            var investigationRoot = CreatePanel(canvasObject.transform, "Investigation Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(420f, 76f));

            var scanTitle = CreateText(scanRoot.transform, "Scan Title", "Scan the room", 24, TextAlignmentOptions.Center);
            SetRect(scanTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(480f, 32f));

            var trackingText = CreateText(scanRoot.transform, "Tracking Text", "Tracking: Unavailable", 18, TextAlignmentOptions.Left);
            SetRect(trackingText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -56f), new Vector2(220f, 28f));

            var progressText = CreateText(scanRoot.transform, "Progress Text", "0%", 18, TextAlignmentOptions.Right);
            SetRect(progressText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -56f), new Vector2(120f, 28f));

            var slider = CreateSlider(scanRoot.transform);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(470f, 18f));

            var instructionText = CreateText(scanRoot.transform, "Instruction Text", "Move slowly and look around.", 16, TextAlignmentOptions.Center);
            SetRect(instructionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(480f, 26f));

            var investigationText = CreateText(investigationRoot.transform, "Investigation Text", "Investigation started", 22, TextAlignmentOptions.Center);
            SetRect(investigationText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 42f));

            var ui = canvasObject.AddComponent<RoomScanUI>();
            ui.Configure(gameStateManager, scanController, scanRoot, investigationRoot, slider, progressText, trackingText, instructionText);
        }

        static void GateTemplatePlacement(Transform parent, GameStateManager gameStateManager)
        {
            var behaviours = new List<Behaviour>();
            var buttons = new List<Button>();

            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null)
                    continue;

                var typeName = behaviour.GetType().Name;
                if (typeName == "ObjectSpawner")
                    behaviours.Add(behaviour);
            }

            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0)
                    buttons.Add(button);
            }

            var gateObject = new GameObject("Investigation Phase Gate");
            gateObject.transform.SetParent(parent);
            var gate = gateObject.AddComponent<InvestigationPhaseGate>();
            gate.Configure(gameStateManager, Array.Empty<GameObject>(), behaviours.ToArray(), Array.Empty<Collider>(), buttons.ToArray());
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.02f, 0.025f, 0.03f, 0.82f);

            SetRect(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPosition, sizeDelta);
            return panel;
        }

        static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.enableWordWrapping = false;
            return label;
        }

        static Slider CreateSlider(Transform parent)
        {
            var sliderObject = new GameObject("Scan Progress Slider", typeof(RectTransform));
            sliderObject.transform.SetParent(parent, false);
            var slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;

            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(sliderObject.transform, false);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0.18f);
            SetRect(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            SetRect(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, 0f));

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.9f, 0.72f, 1f);
            SetRect(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            slider.targetGraphic = fillImage;
            slider.fillRect = fill.GetComponent<RectTransform>();
            return slider;
        }

        static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
