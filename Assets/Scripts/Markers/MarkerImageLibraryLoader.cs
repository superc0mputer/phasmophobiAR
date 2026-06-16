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
            m_ToolDefinitions = toolDefinitions;
        }

        void Start()
        {
            StartCoroutine(LoadWhenARIsReady());
        }

        IEnumerator LoadWhenARIsReady()
        {
            if (m_LoadStarted)
                yield break;

            m_LoadStarted = true;

            while (ARSession.state < ARSessionState.Ready)
                yield return null;

            if (m_TrackedImageManager == null)
            {
                Debug.LogWarning("Marker image library loader has no ARTrackedImageManager.");
                yield break;
            }

            var wasEnabled = m_TrackedImageManager.enabled;
            m_TrackedImageManager.enabled = false;

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

            foreach (var definition in m_ToolDefinitions ?? System.Array.Empty<MarkerToolDefinition>())
            {
                if (definition == null)
                    continue;

                var texture = Resources.Load<Texture2D>(definition.TextureResourcePath);
                if (texture == null)
                {
                    Debug.LogWarning($"Marker texture not found at Resources/{definition.TextureResourcePath} for {definition.MarkerName}.");
                    continue;
                }

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
                    Debug.LogWarning($"Failed to schedule marker image '{definition.MarkerName}': {exception.Message}");
                }
            }

            while (!AllJobsComplete())
                yield return null;

            IsLoaded = true;
            m_TrackedImageManager.requestedMaxNumberOfMovingImages = Mathf.Max(1, m_ToolDefinitions?.Length ?? 0);
            m_TrackedImageManager.enabled = wasEnabled || m_AddJobs.Count > 0;
            Debug.Log($"Marker image library ready with {m_AddJobs.Count} scheduled marker images.");
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
    }
}
