using PhasmophobiAR.Game;
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
            if (m_HasSpawned)
                return;

            m_HasSpawned = true;

            if (m_GhostPrefab == null || m_ARCamera == null)
            {
                Debug.Log("Room scan completed. Ghost spawn hook fired; assign a ghost prefab to spawn a visible ghost.");
                return;
            }

            var forward = Vector3.ProjectOnPlane(m_ARCamera.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = m_ARCamera.forward;

            var spawnPosition = m_ARCamera.position + forward.normalized * m_SpawnDistanceMeters;
            spawnPosition.y += m_SpawnHeightOffsetMeters;
            Instantiate(m_GhostPrefab, spawnPosition, Quaternion.LookRotation(-forward.normalized, Vector3.up));
        }
    }
}
