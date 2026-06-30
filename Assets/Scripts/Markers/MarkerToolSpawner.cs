using System;
using System.Collections.Generic;
using PhasmophobiAR.Game;
using PhasmophobiAR.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PhasmophobiAR.Markers
{
    public sealed class MarkerToolSpawner : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        ARTrackedImageManager m_TrackedImageManager;

        [SerializeField]
        MarkerToolDefinition[] m_ToolDefinitions;

        [SerializeField]
        TMP_Text m_StatusText;

        readonly Dictionary<string, MarkerToolDefinition> m_DefinitionsByMarkerName = new Dictionary<string, MarkerToolDefinition>();
        readonly Dictionary<Guid, MarkerToolDefinition> m_DefinitionsByTextureGuid = new Dictionary<Guid, MarkerToolDefinition>();
        readonly Dictionary<string, GameObject> m_SpawnedToolsByMarkerName = new Dictionary<string, GameObject>();

        public void Configure(
            GameStateManager gameStateManager,
            ARTrackedImageManager trackedImageManager,
            MarkerToolDefinition[] toolDefinitions)
        {
            Configure(gameStateManager, trackedImageManager, toolDefinitions, null);
        }

        public void Configure(
            GameStateManager gameStateManager,
            ARTrackedImageManager trackedImageManager,
            MarkerToolDefinition[] toolDefinitions,
            TMP_Text statusText)
        {
            m_GameStateManager = gameStateManager;
            m_TrackedImageManager = trackedImageManager;
            m_ToolDefinitions = HasDefinitions(toolDefinitions) ? toolDefinitions : MarkerToolDefaults.CreateDefinitions();
            m_StatusText = statusText ?? m_StatusText;
            RebuildDefinitionLookup();
        }

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            RebuildDefinitionLookup();
        }

        void OnEnable()
        {
            if (m_StatusText == null)
            {
                var statusObject = GameObject.Find("Marker Status Text");
                if (statusObject != null)
                    m_StatusText = statusObject.GetComponent<TMP_Text>();
            }

            Debug.Log($"Marker tool spawner enabled. Image manager: {(m_TrackedImageManager != null ? m_TrackedImageManager.name : "none")}. Definitions: {m_DefinitionsByMarkerName.Count}.");
            SetStatus("Ready your tool cards.");

            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);

            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            Debug.Log($"Marker tool spawner phase changed to {phase}. CanPlaceTools={m_GameStateManager != null && m_GameStateManager.CanPlaceTools}.");

            if (phase != GamePhase.Investigation || m_TrackedImageManager == null)
            {
                SetStatus("Tool cards unlock after the scan.");
                return;
            }

            Debug.Log($"Checking {m_TrackedImageManager.trackables.count} existing tracked image(s) for tool placement.");
            SetStatus("Searching for tool cards...");
            foreach (var trackedImage in m_TrackedImageManager.trackables)
                HandleTrackedImage(trackedImage, "existing");
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            Debug.Log($"Tracked images changed. Added={eventArgs.added.Count}, Updated={eventArgs.updated.Count}, Removed={eventArgs.removed.Count}.");

            foreach (var trackedImage in eventArgs.added)
                HandleTrackedImage(trackedImage, "added");

            foreach (var trackedImage in eventArgs.updated)
                HandleTrackedImage(trackedImage, "updated");

            foreach (var removed in eventArgs.removed)
            {
                var markerName = removed.Value != null ? removed.Value.referenceImage.name : removed.Key.ToString();
                Debug.Log($"Tool marker '{markerName}' removed by AR tracking.");
                RemoveTool(markerName);
            }
        }

        void HandleTrackedImage(ARTrackedImage trackedImage, string lifecycle)
        {
            if (trackedImage == null)
                return;

            if (!TryGetDefinition(trackedImage, out var markerName, out var definition))
            {
                Debug.Log($"Tracked image ignored. Name='{trackedImage.referenceImage.name}', textureGuid={trackedImage.referenceImage.textureGuid}, sourceImageId={trackedImage.trackableId}, size={trackedImage.size}, position={trackedImage.transform.position}, state={trackedImage.trackingState}. This image is not in the marker reference library.");
                SetStatus("Unknown card. Use an investigation tool marker.");
                return;
            }

            Debug.Log($"Tool marker '{markerName}' {lifecycle}; state={trackedImage.trackingState}.");
            SetStatus($"{definition.DisplayName} card detected.");

            if (m_GameStateManager != null && !m_GameStateManager.CanPlaceTools)
            {
                Debug.Log($"Tool marker '{markerName}' ignored until investigation starts.");
                SetStatus($"Start the investigation to place {definition.DisplayName}.");
                return;
            }

            if (trackedImage.trackingState != TrackingState.Tracking)
            {
                Debug.Log($"Tool marker '{markerName}' is not currently tracking. Waiting for full tracking before attaching the tool.");
                SetStatus($"Hold the {definition.DisplayName} card steady.");
                return;
            }

            if (!m_SpawnedToolsByMarkerName.TryGetValue(markerName, out var tool) || tool == null)
            {
                tool = SpawnTool(definition, trackedImage.transform);
                m_SpawnedToolsByMarkerName[markerName] = tool;
                Debug.Log($"Spawned {definition.DisplayName} for marker '{markerName}'.");
                SetStatus($"{definition.DisplayName} tracking.");
            }
            else
            {
                AttachToMarker(tool.transform, trackedImage.transform);
                Debug.Log($"Updated {definition.DisplayName} to follow marker '{markerName}'.");
                SetStatus($"{definition.DisplayName} following card.");
            }
        }

        GameObject SpawnTool(MarkerToolDefinition definition, Transform markerTransform)
        {
            GameObject tool;
            if (definition.ToolPrefab != null)
                tool = Instantiate(definition.ToolPrefab);
            else
                tool = CreateFallbackTool(definition);

            tool.name = $"{definition.DisplayName} Tool";
            AttachToMarker(tool.transform, markerTransform);
            return tool;
        }

        static void AttachToMarker(Transform toolTransform, Transform markerTransform)
        {
            toolTransform.SetParent(markerTransform, false);
            toolTransform.localPosition = Vector3.zero;
            toolTransform.localRotation = Quaternion.identity;
        }

        void RemoveTool(string markerName)
        {
            if (string.IsNullOrEmpty(markerName))
                return;

            if (!m_SpawnedToolsByMarkerName.TryGetValue(markerName, out var tool))
                return;

            m_SpawnedToolsByMarkerName.Remove(markerName);
            if (tool != null)
                Destroy(tool);
        }

        static GameObject CreateFallbackTool(MarkerToolDefinition definition)
        {
            var root = new GameObject(definition.DisplayName);

            switch (definition.ToolType)
            {
                case MarkerToolType.Thermometer:
                    CreateThermometerVisual(root.transform);
                    break;
                case MarkerToolType.SpiritResponse:
                    CreateSpiritResponseVisual(root.transform);
                    root.AddComponent<SpiritResponseTool>();
                    break;
                default:
                    CreateEMFVisual(root.transform);
                    break;
            }

            return root;
        }

        static void CreateEMFVisual(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "EMF Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            body.transform.localScale = new Vector3(0.08f, 0.02f, 0.13f);
            SetColor(body, new Color(0.08f, 0.08f, 0.09f));

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "EMF Screen";
            screen.transform.SetParent(parent, false);
            screen.transform.localPosition = new Vector3(0f, 0.051f, 0.02f);
            screen.transform.localScale = new Vector3(0.055f, 0.006f, 0.045f);
            SetColor(screen, new Color(0.15f, 0.9f, 0.45f));
        }

        static void CreateThermometerVisual(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Thermometer Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.018f, 0.08f, 0.018f);
            SetColor(body, new Color(0.88f, 0.9f, 0.92f));

            var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = "Thermometer Display";
            display.transform.SetParent(parent, false);
            display.transform.localPosition = new Vector3(0f, 0.08f, 0.03f);
            display.transform.localScale = new Vector3(0.045f, 0.007f, 0.035f);
            SetColor(display, new Color(0.2f, 0.7f, 1f));
        }

        static void CreateSpiritResponseVisual(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Spirit Response Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            body.transform.localScale = new Vector3(0.095f, 0.018f, 0.115f);
            SetColor(body, new Color(0.06f, 0.05f, 0.09f));

            var speaker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            speaker.name = "Spirit Response Speaker";
            speaker.transform.SetParent(parent, false);
            speaker.transform.localPosition = new Vector3(0f, 0.052f, 0.022f);
            speaker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            speaker.transform.localScale = new Vector3(0.026f, 0.006f, 0.026f);
            SetColor(speaker, new Color(0.22f, 0.18f, 0.32f));

            var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = "Spirit Response Display";
            display.transform.SetParent(parent, false);
            display.transform.localPosition = new Vector3(0f, 0.053f, -0.025f);
            display.transform.localScale = new Vector3(0.06f, 0.006f, 0.028f);
            SetColor(display, new Color(0.55f, 0.9f, 1f));
        }

        static void SetColor(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }

        void RebuildDefinitionLookup()
        {
            m_DefinitionsByMarkerName.Clear();
            m_DefinitionsByTextureGuid.Clear();
            if (!HasDefinitions(m_ToolDefinitions))
                m_ToolDefinitions = MarkerToolDefaults.CreateDefinitions();

            foreach (var definition in m_ToolDefinitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.MarkerName))
                    continue;

                m_DefinitionsByMarkerName[definition.MarkerName] = definition;
            }

            if (Guid.TryParse(MarkerToolDefaults.EMFMarkerTextureGuid, out var emfGuid)
                && m_DefinitionsByMarkerName.TryGetValue(MarkerToolDefaults.EMFMarkerName, out var emfDefinition))
            {
                m_DefinitionsByTextureGuid[emfGuid] = emfDefinition;
            }

            if (Guid.TryParse(MarkerToolDefaults.ThermometerMarkerTextureGuid, out var thermometerGuid)
                && m_DefinitionsByMarkerName.TryGetValue(MarkerToolDefaults.ThermometerMarkerName, out var thermometerDefinition))
            {
                m_DefinitionsByTextureGuid[thermometerGuid] = thermometerDefinition;
            }

            if (Guid.TryParse(MarkerToolDefaults.SpiritResponseMarkerTextureGuid, out var spiritResponseGuid)
                && m_DefinitionsByMarkerName.TryGetValue(MarkerToolDefaults.SpiritResponseMarkerName, out var spiritResponseDefinition))
            {
                m_DefinitionsByTextureGuid[spiritResponseGuid] = spiritResponseDefinition;
            }
        }

        bool TryGetDefinition(ARTrackedImage trackedImage, out string markerName, out MarkerToolDefinition definition)
        {
            markerName = trackedImage.referenceImage.name;
            if (!string.IsNullOrEmpty(markerName)
                && m_DefinitionsByMarkerName.TryGetValue(markerName, out definition))
            {
                return true;
            }

            var textureGuid = trackedImage.referenceImage.textureGuid;
            if (textureGuid != Guid.Empty && m_DefinitionsByTextureGuid.TryGetValue(textureGuid, out definition))
            {
                markerName = definition.MarkerName;
                return true;
            }

            definition = null;
            return false;
        }

        void SetStatus(string message)
        {
            if (m_StatusText != null)
                m_StatusText.text = message;
        }

        static bool HasDefinitions(MarkerToolDefinition[] definitions)
        {
            return definitions != null && definitions.Length > 0;
        }
    }
}
