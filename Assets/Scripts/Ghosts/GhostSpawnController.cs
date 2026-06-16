using PhasmophobiAR.Game;
using PhasmophobiAR.Scanning;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    public sealed class GhostSpawnController : MonoBehaviour
    {
        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GameObject m_GhostPrefab;

        [SerializeField]
        Transform m_ARCamera;

        [SerializeField]
        float m_SpawnDistanceMeters = 2.5f;

        [SerializeField]
        float m_SpawnHeightOffsetMeters = -0.25f;

        bool m_HasSpawned;

        public bool HasSpawned => m_HasSpawned;

        void Awake()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_ARCamera == null && Camera.main != null)
                m_ARCamera = Camera.main.transform;
        }

        void OnEnable()
        {
            if (m_GameStateManager != null)
                m_GameStateManager.ScanCompleted += SpawnGhostOnce;
        }

        void OnDisable()
        {
            if (m_GameStateManager != null)
                m_GameStateManager.ScanCompleted -= SpawnGhostOnce;
        }

        public void SpawnGhostOnce()
        {
            SpawnGhostOnce(m_GameStateManager != null ? m_GameStateManager.LastRoomScanResult : null);
        }

        public void SpawnGhostOnce(RoomScanResult scanResult)
        {
            if (m_HasSpawned)
                return;

            m_HasSpawned = true;

            if (m_GhostPrefab == null || m_ARCamera == null)
            {
                Debug.Log("Room scan completed. Ghost spawn hook fired; assign a ghost prefab to spawn a visible ghost.");
                return;
            }

            if (TryGetScanSpawnPose(scanResult, out var scanPosition, out var scanRotation))
            {
                Instantiate(m_GhostPrefab, scanPosition, scanRotation);
                return;
            }

            var forward = Vector3.ProjectOnPlane(m_ARCamera.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = m_ARCamera.forward;

            var spawnPosition = m_ARCamera.position + forward.normalized * m_SpawnDistanceMeters;
            spawnPosition.y += m_SpawnHeightOffsetMeters;
            Instantiate(m_GhostPrefab, spawnPosition, Quaternion.LookRotation(-forward.normalized, Vector3.up));
        }

        bool TryGetScanSpawnPose(RoomScanResult scanResult, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;

            if (scanResult == null || scanResult.safeSpawnCandidates == null || scanResult.safeSpawnCandidates.Length == 0)
                return false;

            position = scanResult.safeSpawnCandidates[0].position;
            var directionToCamera = m_ARCamera != null ? Vector3.ProjectOnPlane(m_ARCamera.position - position, Vector3.up) : Vector3.forward;
            if (directionToCamera.sqrMagnitude < 0.001f)
                directionToCamera = Vector3.forward;

            rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
            return true;
        }
    }
}
