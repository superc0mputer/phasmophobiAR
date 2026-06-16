using System.Collections.Generic;
using PhasmophobiAR.Game;
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

        readonly Dictionary<string, MarkerToolDefinition> m_DefinitionsByMarkerName = new Dictionary<string, MarkerToolDefinition>();
        readonly Dictionary<string, GameObject> m_SpawnedToolsByMarkerName = new Dictionary<string, GameObject>();

        public void Configure(
            GameStateManager gameStateManager,
            ARTrackedImageManager trackedImageManager,
            MarkerToolDefinition[] toolDefinitions)
        {
            m_GameStateManager = gameStateManager;
            m_TrackedImageManager = trackedImageManager;
            m_ToolDefinitions = toolDefinitions;
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
            if (phase != GamePhase.Investigation || m_TrackedImageManager == null)
                return;

            foreach (var trackedImage in m_TrackedImageManager.trackables)
                HandleTrackedImage(trackedImage, "existing");
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            foreach (var trackedImage in eventArgs.added)
                HandleTrackedImage(trackedImage, "added");

            foreach (var trackedImage in eventArgs.updated)
                HandleTrackedImage(trackedImage, "updated");

            foreach (var removed in eventArgs.removed)
            {
                var markerName = removed.Value != null ? removed.Value.referenceImage.name : removed.Key.ToString();
                Debug.Log($"Tool marker '{markerName}' removed by AR tracking.");
            }
        }

        void HandleTrackedImage(ARTrackedImage trackedImage, string lifecycle)
        {
            if (trackedImage == null)
                return;

            var markerName = trackedImage.referenceImage.name;
            if (string.IsNullOrEmpty(markerName))
                return;

            if (!m_DefinitionsByMarkerName.TryGetValue(markerName, out var definition))
            {
                Debug.Log($"Tracked image '{markerName}' has no tool mapping.");
                return;
            }

            Debug.Log($"Tool marker '{markerName}' {lifecycle}; state={trackedImage.trackingState}.");

            if (m_GameStateManager != null && !m_GameStateManager.CanPlaceTools)
            {
                Debug.Log($"Tool marker '{markerName}' ignored until investigation starts.");
                return;
            }

            if (trackedImage.trackingState != TrackingState.Tracking)
            {
                Debug.Log($"Tool marker '{markerName}' is not currently tracking. Keeping existing tool stable.");
                return;
            }

            if (!m_SpawnedToolsByMarkerName.TryGetValue(markerName, out var tool) || tool == null)
            {
                tool = SpawnTool(definition, trackedImage.transform);
                m_SpawnedToolsByMarkerName[markerName] = tool;
                Debug.Log($"Spawned {definition.DisplayName} for marker '{markerName}'.");
            }
            else
            {
                ApplyPose(tool.transform, trackedImage.transform);
                Debug.Log($"Updated {definition.DisplayName} pose from re-detected marker '{markerName}'.");
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
            ApplyPose(tool.transform, markerTransform);
            return tool;
        }

        static void ApplyPose(Transform toolTransform, Transform markerTransform)
        {
            toolTransform.SetPositionAndRotation(markerTransform.position, markerTransform.rotation);
        }

        static GameObject CreateFallbackTool(MarkerToolDefinition definition)
        {
            var root = new GameObject(definition.DisplayName);

            switch (definition.ToolType)
            {
                case MarkerToolType.Thermometer:
                    CreateThermometerVisual(root.transform);
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

        static void SetColor(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }

        void RebuildDefinitionLookup()
        {
            m_DefinitionsByMarkerName.Clear();
            if (m_ToolDefinitions == null)
                return;

            foreach (var definition in m_ToolDefinitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.MarkerName))
                    continue;

                m_DefinitionsByMarkerName[definition.MarkerName] = definition;
            }
        }
    }
}
