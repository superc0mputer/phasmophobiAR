using System;
using System.Collections.Generic;
using System.Linq;
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
        private float m_MarkerLostDelay = 0.35f;

        readonly Dictionary<string, MarkerToolDefinition> m_DefinitionsByMarkerName = new Dictionary<string, MarkerToolDefinition>();
        readonly Dictionary<Guid, MarkerToolDefinition> m_DefinitionsByTextureGuid = new Dictionary<Guid, MarkerToolDefinition>();
        readonly Dictionary<string, GameObject> m_SpawnedToolsByMarkerName = new Dictionary<string, GameObject>();
        readonly Dictionary<TrackableId, string> m_MarkerNamesByTrackableId = new Dictionary<TrackableId, string>();
        private readonly Dictionary<string, float> m_LastSeenTimeByMarkerName = new();

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

        void Update()
        {
            UpdateMarkerLossTimeouts();
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
                if (!m_MarkerNamesByTrackableId.TryGetValue(removed.Key, out var markerName))
                {
                    // Fallback for cases where the definition might not have been resolved yet
                    markerName = removed.Value != null ? removed.Value.referenceImage.name : null;
                }

                if (!string.IsNullOrEmpty(markerName))
                {
                    Debug.Log($"Tool marker '{markerName}' removed by AR tracking system.");
                    RemoveTool(markerName);
                    m_LastSeenTimeByMarkerName.Remove(markerName);
                }
                
                m_MarkerNamesByTrackableId.Remove(removed.Key);
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

            if (m_GameStateManager != null && !m_GameStateManager.CanPlaceTools)
            {
                Debug.Log($"Tool marker '{markerName}' ignored until investigation starts.");
                SetStatus($"Start the investigation to place {definition.DisplayName}.");
                return;
            }

            switch (trackedImage.trackingState)
            {
                case TrackingState.Tracking:
                    m_LastSeenTimeByMarkerName[markerName] = Time.time;
                    SetStatus($"{definition.DisplayName} card detected.");

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
                        SetStatus($"{definition.DisplayName} tracking.");
                    }
                    else
                    {
                        AttachToMarker(tool.transform, trackedImage.transform);
                        tool.SetActive(true);
                        Debug.Log($"Updated {definition.DisplayName} to follow marker '{markerName}'.");
                        SetStatus($"{definition.DisplayName} following card.");
                    }
                    break;

                case TrackingState.Limited:
                    Debug.Log($"Tool marker '{markerName}' has unstable tracking. Waiting for timeout to handle removal.");
                    break;

                case TrackingState.None:
                    Debug.Log($"Tool marker '{markerName}' is not tracking. Timeout will handle removal.");
                    break;
            }
        }

        private void UpdateMarkerLossTimeouts()
        {
            if (m_LastSeenTimeByMarkerName.Count == 0)
                return;

            // Use ToArray to prevent collection modification issues while iterating
            foreach (var entry in m_LastSeenTimeByMarkerName.ToArray())
            {
                var markerName = entry.Key;
                var lastSeenTime = entry.Value;

                if (Time.time - lastSeenTime > m_MarkerLostDelay)
                {
                    Debug.Log($"Tool marker '{markerName}' lost for longer than {m_MarkerLostDelay}s. Removing tool.");
                    RemoveTool(markerName);
                    m_LastSeenTimeByMarkerName.Remove(markerName);
                    SetStatus("Show the tool card to place it again.");
                }
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
            {
                Debug.LogWarning($"Request to remove tool for marker '{markerName}', but no tool was found in the spawned list.");
                return;
            }

            Debug.Log($"Removing tool for marker '{markerName}'.");
            m_SpawnedToolsByMarkerName.Remove(markerName);
            if (tool != null)
            {
                Destroy(tool);
                Debug.Log($"Destroyed GameObject for tool '{markerName}'.");
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
    }
}
