using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhasmophobiAR.Game
{
    public sealed class JournalCaseRepository : MonoBehaviour
    {
        const string k_PlayerPrefsKey = "PhasmophobiAR.Journal.CaseEntries.v1";

        [Serializable]
        sealed class CaseEntryList
        {
            public List<JournalCaseEntry> entries = new List<JournalCaseEntry>();
        }

        public static JournalCaseRepository Instance { get; private set; }

        [SerializeField]
        GameStateManager m_GameStateManager;

        readonly List<JournalCaseEntry> m_Entries = new List<JournalCaseEntry>();

        public event Action EntriesChanged;

        public IReadOnlyList<JournalCaseEntry> Entries => m_Entries;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {nameof(JournalCaseRepository)} found on {name}; disabling this instance.");
                enabled = false;
                return;
            }

            Instance = this;
            Load();
        }

        void OnEnable()
        {
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            if (m_GameStateManager != null)
                m_GameStateManager.ResultPrepared += SaveResult;
        }

        void OnDisable()
        {
            if (m_GameStateManager != null)
                m_GameStateManager.ResultPrepared -= SaveResult;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Configure(GameStateManager gameStateManager)
        {
            m_GameStateManager = gameStateManager ?? m_GameStateManager;
        }

        public void SaveResult(RoundResult result)
        {
            if (result == null)
                return;

            m_Entries.Insert(0, new JournalCaseEntry(result));
            Save();
            EntriesChanged?.Invoke();
        }

        public void Clear()
        {
            m_Entries.Clear();
            PlayerPrefs.DeleteKey(k_PlayerPrefsKey);
            EntriesChanged?.Invoke();
        }

        void Load()
        {
            m_Entries.Clear();
            var json = PlayerPrefs.GetString(k_PlayerPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var list = JsonUtility.FromJson<CaseEntryList>(json);
                if (list?.entries != null)
                    m_Entries.AddRange(list.entries);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load journal case entries: {exception.Message}");
            }
        }

        void Save()
        {
            var list = new CaseEntryList();
            list.entries.AddRange(m_Entries);
            PlayerPrefs.SetString(k_PlayerPrefsKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }
    }
}
