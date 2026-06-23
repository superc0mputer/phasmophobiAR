using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PhasmophobiAR.Markers
{
    public sealed class MarkerImageLibraryLoader : MonoBehaviour
    {
        [SerializeField]
        ARTrackedImageManager m_TrackedImageManager;

        [SerializeField]
        MarkerToolDefinition[] m_ToolDefinitions;

        readonly List<(string markerName, AddReferenceImageJobState state)> m_AddJobs = new List<(string, AddReferenceImageJobState)>();
        readonly HashSet<string> m_ReportedJobStatuses = new HashSet<string>();
        bool m_LoadStarted;

        public bool IsLoaded { get; private set; }

        public void Configure(ARTrackedImageManager trackedImageManager, MarkerToolDefinition[] toolDefinitions)
        {
            m_TrackedImageManager = trackedImageManager;
            m_ToolDefinitions = HasDefinitions(toolDefinitions) ? toolDefinitions : MarkerToolDefaults.CreateDefinitions();
        }

        void Start()
        {
            Debug.Log("Marker image library loader starting.");
            StartCoroutine(LoadWhenARIsReady());
        }

        IEnumerator LoadWhenARIsReady()
        {
            if (m_LoadStarted)
                yield break;

            m_LoadStarted = true;
            if (!HasDefinitions(m_ToolDefinitions))
                m_ToolDefinitions = MarkerToolDefaults.CreateDefinitions();

            Debug.Log($"Marker image library loader has {m_ToolDefinitions.Length} marker definition(s).");

            Debug.Log($"Marker image library loader waiting for ARSession readiness. Current state: {ARSession.state}.");

            while (ARSession.state < ARSessionState.Ready)
                yield return null;

            Debug.Log($"Marker image library loader continuing. ARSession state: {ARSession.state}.");

            if (m_TrackedImageManager == null)
            {
                Debug.LogWarning("Marker image library loader has no ARTrackedImageManager.");
                yield break;
            }

            var wasEnabled = m_TrackedImageManager.enabled;
            m_TrackedImageManager.enabled = false;
            Debug.Log($"Preparing runtime marker library. Image manager was enabled: {wasEnabled}.");

            RuntimeReferenceImageLibrary runtimeLibrary;
            try
            {
                runtimeLibrary = m_TrackedImageManager.CreateRuntimeLibrary();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Image tracking is unavailable on this device/provider: {exception.Message}");
                yield break;
            }

            var mutableLibrary = runtimeLibrary as MutableRuntimeReferenceImageLibrary;
            if (mutableLibrary == null)
            {
                Debug.LogWarning("Runtime image library is not mutable. Tool marker images could not be added at runtime.");
                yield break;
            }

            m_TrackedImageManager.referenceLibrary = mutableLibrary;

            foreach (var definition in m_ToolDefinitions)
            {
                if (definition == null)
                    continue;

                Debug.Log($"Loading marker texture Resources/{definition.TextureResourcePath} for '{definition.MarkerName}'.");
                var texture = Resources.Load<Texture2D>(definition.TextureResourcePath);
                if (texture == null)
                {
                    Debug.LogWarning($"Marker texture not found at Resources/{definition.TextureResourcePath} for {definition.MarkerName}.");
                    continue;
                }

                Debug.Log($"Marker texture '{definition.MarkerName}' loaded. Size={texture.width}x{texture.height}, format={texture.format}, readable={texture.isReadable}.");
                try
                {
                    var jobState = mutableLibrary.ScheduleAddImageWithValidationJob(
                        texture,
                        definition.MarkerName,
                        definition.PhysicalWidthMeters);
                    m_AddJobs.Add((definition.MarkerName, jobState));
                    Debug.Log($"Scheduled marker image '{definition.MarkerName}' for runtime tracking.");
                }
                catch (System.Exception exception)
                {
                    Debug.LogError($"Failed to schedule marker image '{definition.MarkerName}': {exception.GetType().Name}: {exception.Message}");
                }
            }

            while (!AllJobsComplete())
                yield return null;

            IsLoaded = true;
            m_TrackedImageManager.requestedMaxNumberOfMovingImages = Mathf.Max(1, m_ToolDefinitions?.Length ?? 0);
            m_TrackedImageManager.enabled = wasEnabled || HasDefinitions(m_ToolDefinitions);

            if (m_AddJobs.Count == 0 && HasDefinitions(m_ToolDefinitions))
                Debug.LogWarning("No marker images were added to the runtime library. Image tracking is still enabled so XR Simulation can report simulated tracked images.");

            Debug.Log($"Marker image library ready with {m_AddJobs.Count} scheduled marker images. Image manager enabled: {m_TrackedImageManager.enabled}.");
        }

        bool AllJobsComplete()
        {
            var allComplete = true;

            foreach (var addJob in m_AddJobs)
            {
                var status = addJob.state.status;
                if (status.IsPending())
                {
                    allComplete = false;
                    continue;
                }

                if (!m_ReportedJobStatuses.Add(addJob.markerName))
                    continue;

                if (status.IsSuccess())
                    Debug.Log($"Marker image '{addJob.markerName}' added to runtime library.");
                else if (status.IsError())
                    Debug.LogWarning($"Marker image '{addJob.markerName}' failed to add: {status}");
            }

            return allComplete;
        }

        static bool HasDefinitions(MarkerToolDefinition[] definitions)
        {
            return definitions != null && definitions.Length > 0;
        }
    }
}
