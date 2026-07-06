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

        [SerializeField]
        Camera m_ARCamera;

        [SerializeField, Min(0.1f)]
        float m_MarkerVisibilityGraceSeconds = 0.75f;

        [SerializeField, Min(0.1f)]
        float m_PartialTrackingGraceSeconds = 1.5f;

        [SerializeField, Range(0f, 0.5f)]
        float m_MarkerViewportMargin = 0.12f;

        readonly Dictionary<string, MarkerToolDefinition> m_DefinitionsByMarkerName = new Dictionary<string, MarkerToolDefinition>();
        readonly Dictionary<Guid, MarkerToolDefinition> m_DefinitionsByTextureGuid = new Dictionary<Guid, MarkerToolDefinition>();
        readonly Dictionary<string, GameObject> m_SpawnedToolsByMarkerName = new Dictionary<string, GameObject>();
        readonly Dictionary<TrackableId, string> m_MarkerNamesByTrackableId = new Dictionary<TrackableId, string>();
        readonly Dictionary<string, float> m_LastVisibleTimeByMarkerName = new Dictionary<string, float>();
        readonly Dictionary<string, float> m_PartialTrackingStartTimeByMarkerName = new Dictionary<string, float>();
        readonly List<string> m_ExpiredMarkerNames = new List<string>();

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

            if (m_ARCamera == null)
                m_ARCamera = Camera.main;

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

        void Update()
        {
            var now = Time.realtimeSinceStartup;
            RefreshVisibleTrackedImages(now);
            RemoveExpiredTools(now);
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
                var markerName = removed.Value != null ? removed.Value.referenceImage.name : null;
                if (m_MarkerNamesByTrackableId.TryGetValue(removed.Key, out var trackedMarkerName))
                    markerName = trackedMarkerName;

                m_MarkerNamesByTrackableId.Remove(removed.Key);
                Debug.Log($"Tool marker '{markerName}' removed by AR tracking.");
                RemoveTool(markerName);
                if (!string.IsNullOrEmpty(markerName))
                {
                    m_LastVisibleTimeByMarkerName.Remove(markerName);
                    m_PartialTrackingStartTimeByMarkerName.Remove(markerName);
                }
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

            m_MarkerNamesByTrackableId[trackedImage.trackableId] = markerName;

            Debug.Log($"Tool marker '{markerName}' {lifecycle}; state={trackedImage.trackingState}.");
            SetStatus($"{definition.DisplayName} card detected.");

            if (m_GameStateManager != null && !m_GameStateManager.CanPlaceTools)
            {
                Debug.Log($"Tool marker '{markerName}' ignored until investigation starts.");
                SetStatus($"Start the investigation to place {definition.DisplayName}.");
                return;
            }

            var now = Time.realtimeSinceStartup;
            var visibility = GetMarkerVisibility(trackedImage);
            if (visibility == MarkerVisibility.NotVisible)
            {
                Debug.Log($"Tool marker '{markerName}' is not currently visible. Waiting up to {m_MarkerVisibilityGraceSeconds:0.00}s before removing its tool.");
                RemoveToolIfExpired(markerName, definition, now);
                return;
            }

            if (visibility == MarkerVisibility.Partial)
            {
                if (!m_PartialTrackingStartTimeByMarkerName.TryGetValue(markerName, out var partialStartTime))
                {
                    partialStartTime = now;
                    m_PartialTrackingStartTimeByMarkerName[markerName] = partialStartTime;
                }

                if (HasVisibilityTimedOut(now, partialStartTime, m_PartialTrackingGraceSeconds))
                {
                    Debug.Log($"Tool marker '{markerName}' stayed in partial tracking for more than {m_PartialTrackingGraceSeconds:0.00}s. Removing stale tool until full tracking returns.");
                    RemoveTool(markerName);
                    m_LastVisibleTimeByMarkerName.Remove(markerName);
                    SetStatus($"Show the full {definition.DisplayName} card to place it.");
                    return;
                }

                SetStatus($"{definition.DisplayName} partially tracking.");
            }
            else
            {
                m_PartialTrackingStartTimeByMarkerName.Remove(markerName);
            }

            m_LastVisibleTimeByMarkerName[markerName] = now;

            if (!m_SpawnedToolsByMarkerName.TryGetValue(markerName, out var tool) || tool == null)
            {
                tool = SpawnTool(definition, trackedImage.transform);
                if (tool == null)
                {
                    Debug.LogError($"No prefab is assigned for {definition.DisplayName}.", this);
                    SetStatus($"{definition.DisplayName} prefab is missing.");
                    return;
                }

                m_SpawnedToolsByMarkerName[markerName] = tool;
                Debug.Log($"Spawned {definition.DisplayName} for marker '{markerName}'.");
                if (visibility == MarkerVisibility.Confirmed)
                    SetStatus($"{definition.DisplayName} tracking.");
            }
            else
            {
                AttachToMarker(tool.transform, trackedImage.transform);
                tool.SetActive(true);
                Debug.Log($"Updated {definition.DisplayName} to follow marker '{markerName}'.");
                if (visibility == MarkerVisibility.Confirmed)
                    SetStatus($"{definition.DisplayName} following card.");
            }
        }

        static GameObject SpawnTool(MarkerToolDefinition definition, Transform markerTransform)
        {
            if (definition.ToolPrefab == null)
                return null;

            var tool = Instantiate(definition.ToolPrefab, markerTransform);
            tool.name = $"{definition.DisplayName} Tool";
            tool.transform.localPosition = Vector3.zero;
            tool.transform.localRotation = Quaternion.identity;
            tool.SetActive(true);
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

        void RefreshVisibleTrackedImages(float now)
        {
            if (m_TrackedImageManager == null || m_SpawnedToolsByMarkerName.Count == 0)
                return;

            foreach (var trackedImage in m_TrackedImageManager.trackables)
            {
                if (trackedImage == null || !TryGetDefinition(trackedImage, out var markerName, out _))
                    continue;

                if (!m_SpawnedToolsByMarkerName.ContainsKey(markerName))
                    continue;

                var visibility = GetMarkerVisibility(trackedImage);
                if (visibility == MarkerVisibility.NotVisible)
                    continue;

                m_LastVisibleTimeByMarkerName[markerName] = now;
                if (visibility == MarkerVisibility.Confirmed)
                {
                    m_PartialTrackingStartTimeByMarkerName.Remove(markerName);
                    continue;
                }

                if (!m_PartialTrackingStartTimeByMarkerName.ContainsKey(markerName))
                    m_PartialTrackingStartTimeByMarkerName[markerName] = now;
            }
        }

        void RemoveExpiredTools(float now)
        {
            if (m_SpawnedToolsByMarkerName.Count == 0)
                return;

            m_ExpiredMarkerNames.Clear();
            foreach (var spawnedTool in m_SpawnedToolsByMarkerName)
            {
                if (spawnedTool.Value == null)
                {
                    m_ExpiredMarkerNames.Add(spawnedTool.Key);
                    continue;
                }

                if (!m_LastVisibleTimeByMarkerName.TryGetValue(spawnedTool.Key, out var lastVisibleTime)
                    || HasVisibilityTimedOut(now, lastVisibleTime, m_MarkerVisibilityGraceSeconds))
                {
                    m_ExpiredMarkerNames.Add(spawnedTool.Key);
                    continue;
                }

                if (m_PartialTrackingStartTimeByMarkerName.TryGetValue(spawnedTool.Key, out var partialStartTime)
                    && HasVisibilityTimedOut(now, partialStartTime, m_PartialTrackingGraceSeconds))
                {
                    m_ExpiredMarkerNames.Add(spawnedTool.Key);
                }
            }

            foreach (var markerName in m_ExpiredMarkerNames)
            {
                if (m_DefinitionsByMarkerName.TryGetValue(markerName, out var definition))
                    Debug.Log($"Tool marker '{markerName}' visibility timed out. Removing {definition.DisplayName} until the card is visible again.");
                else
                    Debug.Log($"Tool marker '{markerName}' visibility timed out. Removing its tool until the card is visible again.");

                RemoveTool(markerName);
                m_LastVisibleTimeByMarkerName.Remove(markerName);
                m_PartialTrackingStartTimeByMarkerName.Remove(markerName);
            }
        }

        void RemoveToolIfExpired(string markerName, MarkerToolDefinition definition, float now)
        {
            if (!m_LastVisibleTimeByMarkerName.TryGetValue(markerName, out var lastVisibleTime)
                || HasVisibilityTimedOut(now, lastVisibleTime, m_MarkerVisibilityGraceSeconds))
            {
                RemoveTool(markerName);
                m_LastVisibleTimeByMarkerName.Remove(markerName);
                m_PartialTrackingStartTimeByMarkerName.Remove(markerName);
                SetStatus($"Show the {definition.DisplayName} card to place it.");
            }
            else
            {
                SetStatus($"{definition.DisplayName} reacquiring...");
            }
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

        MarkerVisibility GetMarkerVisibility(ARTrackedImage trackedImage)
        {
            if (!IsVisibleTrackingState(trackedImage.trackingState))
                return MarkerVisibility.NotVisible;

            if (m_ARCamera == null)
                m_ARCamera = Camera.main;

            var isInsideViewport = m_ARCamera == null
                || IsMarkerInsideViewport(
                    m_ARCamera,
                    trackedImage.transform,
                    trackedImage.size,
                    m_MarkerViewportMargin);

            if (!isInsideViewport)
                return MarkerVisibility.NotVisible;

            return trackedImage.trackingState == TrackingState.Tracking
                ? MarkerVisibility.Confirmed
                : MarkerVisibility.Partial;
        }

        public static bool IsMarkerInsideViewport(Camera camera, Transform markerTransform, Vector2 markerSize, float viewportMargin)
        {
            if (camera == null || markerTransform == null)
                return false;

            var halfWidth = Mathf.Max(0.01f, markerSize.x) * 0.5f;
            var halfHeight = Mathf.Max(0.01f, markerSize.y) * 0.5f;
            var margin = Mathf.Max(0f, viewportMargin);

            return IsWorldPointInsideViewport(camera, markerTransform.position, margin)
                || IsWorldPointInsideViewport(camera, markerTransform.TransformPoint(new Vector3(-halfWidth, 0f, -halfHeight)), margin)
                || IsWorldPointInsideViewport(camera, markerTransform.TransformPoint(new Vector3(-halfWidth, 0f, halfHeight)), margin)
                || IsWorldPointInsideViewport(camera, markerTransform.TransformPoint(new Vector3(halfWidth, 0f, -halfHeight)), margin)
                || IsWorldPointInsideViewport(camera, markerTransform.TransformPoint(new Vector3(halfWidth, 0f, halfHeight)), margin);
        }

        public static bool IsWorldPointInsideViewport(Camera camera, Vector3 worldPoint, float viewportMargin)
        {
            if (camera == null)
                return false;

            var viewportPoint = camera.WorldToViewportPoint(worldPoint);
            var margin = Mathf.Max(0f, viewportMargin);
            return viewportPoint.z > 0f
                && viewportPoint.x >= -margin
                && viewportPoint.x <= 1f + margin
                && viewportPoint.y >= -margin
                && viewportPoint.y <= 1f + margin;
        }

        public static bool IsVisibleTrackingState(TrackingState trackingState)
        {
            return trackingState == TrackingState.Tracking || trackingState == TrackingState.Limited;
        }

        public static bool HasVisibilityTimedOut(float now, float lastVisibleTime, float timeoutSeconds)
        {
            return now - lastVisibleTime >= Mathf.Max(0f, timeoutSeconds);
        }

        enum MarkerVisibility
        {
            NotVisible,
            Partial,
            Confirmed
        }
    }
}
