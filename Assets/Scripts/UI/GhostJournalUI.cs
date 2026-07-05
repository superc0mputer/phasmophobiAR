using System.Text;
using PhasmophobiAR.Game;
using PhasmophobiAR.Ghosts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhasmophobiAR.UI
{
    public sealed class GhostJournalUI : MonoBehaviour
    {
        enum JournalPage
        {
            Investigation,
            Reference,
            Cases
        }

        [SerializeField]
        EvidenceRegistry m_EvidenceRegistry;

        [SerializeField]
        JournalEvidenceSelection m_JournalEvidenceSelection;

        [SerializeField]
        IdentificationController m_IdentificationController;

        [SerializeField]
        GameStateManager m_GameStateManager;

        [SerializeField]
        GhostRevealCaptureController m_CaptureController;

        [SerializeField]
        JournalCaseRepository m_CaseRepository;

        [SerializeField]
        GameObject m_JournalRoot;

        [SerializeField]
        Button m_OpenButton;

        [SerializeField]
        Button m_CloseButton;

        [SerializeField]
        Button m_RestartButton;

        [SerializeField]
        Button m_InvestigationTabButton;

        [SerializeField]
        Button m_ReferenceTabButton;

        [SerializeField]
        Button m_CasesTabButton;

        [SerializeField]
        GameObject m_InvestigationPage;

        [SerializeField]
        GameObject m_ReferencePage;

        [SerializeField]
        GameObject m_CasesPage;

        [SerializeField]
        TMP_Text m_EvidenceText;

        [SerializeField]
        Button[] m_EvidenceButtons;

        [SerializeField]
        TMP_Text[] m_EvidenceButtonLabels;

        [SerializeField]
        TMP_Text m_PossibleGhostsText;

        [SerializeField]
        TMP_Text m_SelectedGhostText;

        [SerializeField]
        Button[] m_GhostSelectionButtons;

        [SerializeField]
        TMP_Text[] m_GhostSelectionLabels;

        [SerializeField]
        Image[] m_GhostCrossoutImages;

        [SerializeField]
        Button m_SubmitButton;

        [SerializeField]
        TMP_Text m_ReferenceText;

        [SerializeField]
        TMP_Text m_ReferenceTextRight;

        [SerializeField]
        TMP_Text m_CasesText;

        [SerializeField]
        bool m_StartOpen;

        JournalPage m_CurrentPage = JournalPage.Investigation;
        GhostRevealCaptureController m_SubscribedCaptureController;

        void Awake()
        {
            ResolveReferences();
            EnsureRestartButton();
            SetOpen(m_StartOpen);
            BuildStaticLabels();
        }

        void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            EvidenceRegistry evidenceRegistry,
            JournalEvidenceSelection journalEvidenceSelection,
            IdentificationController identificationController,
            JournalCaseRepository caseRepository,
            GameObject journalRoot,
            Button openButton,
            Button closeButton,
            Button investigationTabButton,
            Button referenceTabButton,
            Button casesTabButton,
            GameObject investigationPage,
            GameObject referencePage,
            GameObject casesPage,
            TMP_Text evidenceText,
            Button[] evidenceButtons,
            TMP_Text[] evidenceButtonLabels,
            TMP_Text possibleGhostsText,
            TMP_Text selectedGhostText,
            Button[] ghostSelectionButtons,
            TMP_Text[] ghostSelectionLabels,
            Image[] ghostCrossoutImages,
            Button submitButton,
            TMP_Text referenceText,
            TMP_Text casesText,
            Button restartButton = null)
        {
            Unsubscribe();

            m_EvidenceRegistry = evidenceRegistry ?? m_EvidenceRegistry;
            m_JournalEvidenceSelection = journalEvidenceSelection ?? m_JournalEvidenceSelection;
            m_IdentificationController = identificationController ?? m_IdentificationController;
            m_CaseRepository = caseRepository ?? m_CaseRepository;
            m_JournalRoot = journalRoot ?? m_JournalRoot;
            m_OpenButton = openButton ?? m_OpenButton;
            m_CloseButton = closeButton ?? m_CloseButton;
            m_RestartButton = restartButton ?? m_RestartButton;
            m_InvestigationTabButton = investigationTabButton ?? m_InvestigationTabButton;
            m_ReferenceTabButton = referenceTabButton ?? m_ReferenceTabButton;
            m_CasesTabButton = casesTabButton ?? m_CasesTabButton;
            m_InvestigationPage = investigationPage ?? m_InvestigationPage;
            m_ReferencePage = referencePage ?? m_ReferencePage;
            m_CasesPage = casesPage ?? m_CasesPage;
            m_EvidenceText = evidenceText ?? m_EvidenceText;
            m_EvidenceButtons = evidenceButtons ?? m_EvidenceButtons;
            m_EvidenceButtonLabels = evidenceButtonLabels ?? m_EvidenceButtonLabels;
            m_PossibleGhostsText = possibleGhostsText ?? m_PossibleGhostsText;
            m_SelectedGhostText = selectedGhostText ?? m_SelectedGhostText;
            m_GhostSelectionButtons = ghostSelectionButtons ?? m_GhostSelectionButtons;
            m_GhostSelectionLabels = ghostSelectionLabels ?? m_GhostSelectionLabels;
            m_GhostCrossoutImages = ghostCrossoutImages ?? m_GhostCrossoutImages;
            m_SubmitButton = submitButton ?? m_SubmitButton;
            m_ReferenceText = referenceText ?? m_ReferenceText;
            m_CasesText = casesText ?? m_CasesText;

            BuildStaticLabels();
            EnsureRestartButton();
            Subscribe();
            RefreshAll();
        }

        void Subscribe()
        {
            ResolveReferences();

            if (m_OpenButton != null)
            {
                m_OpenButton.onClick.RemoveListener(Open);
                m_OpenButton.onClick.AddListener(Open);
            }

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.RemoveListener(Close);
                m_CloseButton.onClick.AddListener(Close);
            }

            if (m_RestartButton != null)
            {
                m_RestartButton.onClick.RemoveListener(RestartRound);
                m_RestartButton.onClick.AddListener(RestartRound);
            }

            if (m_InvestigationTabButton != null)
            {
                m_InvestigationTabButton.onClick.RemoveListener(ShowInvestigationPage);
                m_InvestigationTabButton.onClick.AddListener(ShowInvestigationPage);
            }

            if (m_ReferenceTabButton != null)
            {
                m_ReferenceTabButton.onClick.RemoveListener(ShowReferencePage);
                m_ReferenceTabButton.onClick.AddListener(ShowReferencePage);
            }

            if (m_CasesTabButton != null)
            {
                m_CasesTabButton.onClick.RemoveListener(ShowCasesPage);
                m_CasesTabButton.onClick.AddListener(ShowCasesPage);
            }

            if (m_SubmitButton != null)
            {
                m_SubmitButton.onClick.RemoveListener(HandleSubmitButton);
                m_SubmitButton.onClick.AddListener(HandleSubmitButton);
            }

            WireGhostSelectionButtons();
            WireEvidenceButtons();

            if (m_JournalEvidenceSelection != null)
            {
                m_JournalEvidenceSelection.SelectionChanged -= RefreshInvestigationPage;
                m_JournalEvidenceSelection.SelectionChanged += RefreshInvestigationPage;
            }

            if (m_IdentificationController != null)
            {
                m_IdentificationController.SelectionChanged -= RefreshInvestigationPage;
                m_IdentificationController.SelectionChanged += RefreshInvestigationPage;
            }

            if (m_CaseRepository != null)
            {
                m_CaseRepository.EntriesChanged -= RefreshCasesPage;
                m_CaseRepository.EntriesChanged += RefreshCasesPage;
            }

            if (m_GameStateManager != null)
            {
                m_GameStateManager.PhaseChanged -= OnGamePhaseChanged;
                m_GameStateManager.PhaseChanged += OnGamePhaseChanged;
            }
        }

        void Unsubscribe()
        {
            if (m_OpenButton != null)
                m_OpenButton.onClick.RemoveListener(Open);
            if (m_CloseButton != null)
                m_CloseButton.onClick.RemoveListener(Close);
            if (m_RestartButton != null)
                m_RestartButton.onClick.RemoveListener(RestartRound);
            if (m_InvestigationTabButton != null)
                m_InvestigationTabButton.onClick.RemoveListener(ShowInvestigationPage);
            if (m_ReferenceTabButton != null)
                m_ReferenceTabButton.onClick.RemoveListener(ShowReferencePage);
            if (m_CasesTabButton != null)
                m_CasesTabButton.onClick.RemoveListener(ShowCasesPage);
            if (m_SubmitButton != null)
                m_SubmitButton.onClick.RemoveListener(HandleSubmitButton);

            if (m_GhostSelectionButtons != null)
            {
                foreach (var button in m_GhostSelectionButtons)
                {
                    if (button != null)
                        button.onClick.RemoveAllListeners();
                }
            }

            if (m_EvidenceButtons != null)
            {
                foreach (var button in m_EvidenceButtons)
                {
                    if (button != null)
                        button.onClick.RemoveAllListeners();
                }
            }

            if (m_JournalEvidenceSelection != null)
                m_JournalEvidenceSelection.SelectionChanged -= RefreshInvestigationPage;
            if (m_IdentificationController != null)
                m_IdentificationController.SelectionChanged -= RefreshInvestigationPage;
            if (m_CaseRepository != null)
                m_CaseRepository.EntriesChanged -= RefreshCasesPage;
            if (m_GameStateManager != null)
                m_GameStateManager.PhaseChanged -= OnGamePhaseChanged;

            if (m_SubscribedCaptureController != null)
            {
                m_SubscribedCaptureController.CaptureSucceeded -= OnCaptureSucceeded;
                m_SubscribedCaptureController.CaptureInterrupted -= OnCaptureInterrupted;
                m_SubscribedCaptureController = null;
            }
        }

        void WireGhostSelectionButtons()
        {
            if (m_GhostSelectionButtons == null)
                return;

            var profiles = GhostProfileCatalog.GetMvpSelectableProfiles();
            for (var i = 0; i < m_GhostSelectionButtons.Length; i++)
            {
                var button = m_GhostSelectionButtons[i];
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
                if (i >= profiles.Length)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                var ghostType = profiles[i].ghostType;
                button.onClick.AddListener(() => SelectGhost(ghostType));
            }
        }

        void WireEvidenceButtons()
        {
            if (m_EvidenceButtons == null)
                return;

            for (var i = 0; i < m_EvidenceButtons.Length; i++)
            {
                var button = m_EvidenceButtons[i];
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
                if (!TryGetEvidenceByIndex(i, out var evidenceType))
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => ToggleEvidence(evidenceType));
            }
        }

        void BuildStaticLabels()
        {
            if (m_GhostSelectionLabels != null)
            {
                var profiles = GhostProfileCatalog.GetMvpSelectableProfiles();
                for (var i = 0; i < m_GhostSelectionLabels.Length; i++)
                {
                    if (m_GhostSelectionLabels[i] == null)
                        continue;

                    m_GhostSelectionLabels[i].text = i < profiles.Length
                        ? profiles[i].displayName
                        : string.Empty;
                }
            }

            if (m_EvidenceButtonLabels != null)
            {
                for (var i = 0; i < m_EvidenceButtonLabels.Length; i++)
                {
                    if (m_EvidenceButtonLabels[i] == null)
                        continue;

                    m_EvidenceButtonLabels[i].text = TryGetEvidenceByIndex(i, out var evidenceType)
                        ? FormatEvidenceName(evidenceType)
                        : string.Empty;
                }
            }

            RefreshReferencePage();
        }

        public void Open()
        {
            if (!CanShowJournalOpenButton())
                return;

            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        void SetOpen(bool isOpen)
        {
            if (isOpen && !CanShowJournalOpenButton())
                isOpen = false;

            if (m_JournalRoot != null)
                m_JournalRoot.SetActive(isOpen);

            if (isOpen)
                RefreshAll();
        }

        void OnGamePhaseChanged(GamePhase phase)
        {
            RefreshOpenButtonVisibility();
            Close();
        }

        void RestartRound()
        {
            Close();

            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;

            m_GameStateManager?.PlayAgain();
        }

        void ShowInvestigationPage()
        {
            m_CurrentPage = JournalPage.Investigation;
            RefreshPageVisibility();
        }

        void ShowReferencePage()
        {
            m_CurrentPage = JournalPage.Reference;
            RefreshPageVisibility();
        }

        void ShowCasesPage()
        {
            m_CurrentPage = JournalPage.Cases;
            RefreshPageVisibility();
        }

        void SelectGhost(GhostType ghostType)
        {
            if (m_IdentificationController == null)
                m_IdentificationController = IdentificationController.Instance;

            m_IdentificationController?.SelectGhost(ghostType);
            RefreshInvestigationPage();
        }

        void ToggleEvidence(EvidenceType evidenceType)
        {
            if (m_JournalEvidenceSelection == null)
                m_JournalEvidenceSelection = JournalEvidenceSelection.Instance;

            m_JournalEvidenceSelection?.Toggle(evidenceType);
            RefreshInvestigationPage();
        }

        void HandleSubmitButton()
        {
            var gameStateManager = GameStateManager.Instance;
            if (gameStateManager == null)
                return;

            if (gameStateManager.CurrentPhase != GamePhase.Investigation)
            {
                Debug.LogWarning($"{nameof(GhostJournalUI)} blocked identification submission outside {nameof(GamePhase.Investigation)} phase.");
                return;
            }

            ResolveReferences();
            if (m_IdentificationController == null || !m_IdentificationController.HasSelection)
            {
                Debug.LogWarning($"{nameof(GhostJournalUI)} blocked capture request before selecting a ghost.");
                return;
            }

            if (m_CaptureController == null)
            {
                Debug.LogWarning($"{nameof(GhostJournalUI)} cannot start capture without a {nameof(GhostRevealCaptureController)}.");
                return;
            }

            if (m_CaptureController.HasCompletedCapture)
            {
                Debug.LogWarning($"{nameof(GhostJournalUI)} blocked duplicate submission after capture result was already recorded.");
                return;
            }

            m_CaptureController.RequestCapture();
            Close();
            RefreshInvestigationPage();
        }

        void RefreshAll()
        {
            RefreshOpenButtonVisibility();
            RefreshPageVisibility();
            RefreshInvestigationPage();
            RefreshReferencePage();
            RefreshCasesPage();
        }

        void RefreshOpenButtonVisibility()
        {
            if (m_OpenButton != null)
                m_OpenButton.gameObject.SetActive(CanShowJournalOpenButton());
        }

        bool CanShowJournalOpenButton()
        {
            ResolveReferences();
            return m_GameStateManager != null && m_GameStateManager.CurrentPhase == GamePhase.Investigation;
        }

        void RefreshPageVisibility()
        {
            if (m_InvestigationPage != null)
                m_InvestigationPage.SetActive(m_CurrentPage == JournalPage.Investigation);
            if (m_ReferencePage != null)
                m_ReferencePage.SetActive(m_CurrentPage == JournalPage.Reference);
            if (m_CasesPage != null)
                m_CasesPage.SetActive(m_CurrentPage == JournalPage.Cases);
        }

        void RefreshInvestigationPage()
        {
            ResolveReferences();
            var evidence = m_JournalEvidenceSelection != null
                ? m_JournalEvidenceSelection.GetSelectedEvidenceSnapshot()
                : System.Array.Empty<EvidenceType>();
            var matchResult = GhostEvidenceMatcher.Match(evidence);

            if (m_EvidenceText != null)
                m_EvidenceText.text = "Evidence";

            RefreshEvidenceButtons();
            RefreshGhostSelectionButtons(matchResult);

            if (m_SelectedGhostText != null)
            {
                if (m_IdentificationController != null && m_IdentificationController.HasSelection)
                    m_SelectedGhostText.text = $"Selected: {GhostProfileCatalog.GetProfile(m_IdentificationController.SelectedGhostType)?.displayName ?? m_IdentificationController.SelectedGhostType.ToString()}";
                else
                    m_SelectedGhostText.text = "Selected: none";
            }

            if (m_SubmitButton != null)
                RefreshSubmitButton();
        }

        void RefreshSubmitButton()
        {
            if (m_SubmitButton == null)
                return;

            ResolveReferences();
            var hasSelection = m_IdentificationController != null && m_IdentificationController.HasSelection;
            var canCapture = hasSelection
                && m_CaptureController != null
                && !m_CaptureController.CaptureRequested
                && !m_CaptureController.HasCompletedCapture;
            var hasCompletedCapture = m_CaptureController != null && m_CaptureController.HasCompletedCapture;

            m_SubmitButton.gameObject.SetActive(hasSelection && !hasCompletedCapture);
            m_SubmitButton.interactable = canCapture;

            var label = m_SubmitButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (m_CaptureController != null && m_CaptureController.HasCompletedCapture)
                    label.text = "Captured";
                else if (m_CaptureController != null && m_CaptureController.CaptureRequested)
                    label.text = "Capture active";
                else
                    label.text = "Start Capture";
            }
        }

        void RefreshReferencePage()
        {
            if (m_ReferenceText == null || m_ReferenceTextRight == null)
                return;

            var leftPage = new StringBuilder();
            var rightPage = new StringBuilder();
            for (var i = 0; i < GhostProfileCatalog.Profiles.Count; i++)
            {
                var profile = GhostProfileCatalog.Profiles[i];
                var page = i < 3 ? leftPage : rightPage;
                AppendFieldGuideEntry(page, profile);
            }

            m_ReferenceText.text = leftPage.ToString().TrimEnd();
            m_ReferenceTextRight.text = rightPage.ToString().TrimEnd();
        }

        static void AppendFieldGuideEntry(StringBuilder builder, GhostProfile profile)
        {
            builder.Append("<b><color=#5CF2C2>");
            builder.Append(profile.displayName.ToUpperInvariant());
            builder.AppendLine("</color></b>");
            builder.AppendLine(profile.description);
            builder.AppendLine(profile.behaviorSummary);
            builder.Append("<color=#9FB8AD>Evidence: ");
            builder.Append(FormatEvidenceInline(profile.requiredEvidence));
            builder.AppendLine("</color>");
            builder.AppendLine();
        }

        void RefreshCasesPage()
        {
            if (m_CasesText == null)
                return;

            ResolveReferences();
            if (m_CaseRepository == null || m_CaseRepository.Entries.Count == 0)
            {
                m_CasesText.text = "No captured cases yet.";
                return;
            }

            var builder = new StringBuilder();
            foreach (var entry in m_CaseRepository.Entries)
            {
                var actual = GhostProfileCatalog.GetProfile(entry.actualGhostType)?.displayName ?? entry.actualGhostType.ToString();
                var selected = entry.hasSelection
                    ? GhostProfileCatalog.GetProfile(entry.selectedGhostType)?.displayName ?? entry.selectedGhostType.ToString()
                    : "None";

                builder.AppendLine($"{actual} - {(entry.isCorrect ? "Correct" : "Incorrect")}");
                builder.AppendLine($"Selected: {selected}");
                builder.AppendLine($"Evidence: {FormatEvidenceInline(entry.recordedEvidence)}");
                builder.AppendLine($"Capture: {FormatCaptureOutcome(entry)}");
                builder.AppendLine();
            }

            m_CasesText.text = builder.ToString().TrimEnd();
        }

        void ResolveReferences()
        {
            if (m_EvidenceRegistry == null)
                m_EvidenceRegistry = EvidenceRegistry.Instance;
            if (m_JournalEvidenceSelection == null)
                m_JournalEvidenceSelection = JournalEvidenceSelection.Instance;
            if (m_IdentificationController == null)
                m_IdentificationController = IdentificationController.Instance;
            if (m_GameStateManager == null)
                m_GameStateManager = GameStateManager.Instance;
            if (m_CaptureController == null)
                m_CaptureController = FindCaptureController();
            if (m_CaseRepository == null)
                m_CaseRepository = JournalCaseRepository.Instance;

            SubscribeCaptureController();
        }

        static GhostRevealCaptureController FindCaptureController()
        {
            var spawnedGhosts = GhostSpawnController.GetSpawnedGhostInfos();
            foreach (var info in spawnedGhosts)
            {
                if (info?.ghostTransform == null)
                    continue;

                var controller = info.ghostTransform.GetComponent<GhostRevealCaptureController>();
                if (controller != null)
                    return controller;
            }

            return FindAnyObjectByType<GhostRevealCaptureController>();
        }

        void SubscribeCaptureController()
        {
            if (m_CaptureController == m_SubscribedCaptureController)
                return;

            if (m_SubscribedCaptureController != null)
            {
                m_SubscribedCaptureController.CaptureSucceeded -= OnCaptureSucceeded;
                m_SubscribedCaptureController.CaptureInterrupted -= OnCaptureInterrupted;
            }

            m_SubscribedCaptureController = m_CaptureController;

            if (m_SubscribedCaptureController != null)
            {
                m_SubscribedCaptureController.CaptureSucceeded -= OnCaptureSucceeded;
                m_SubscribedCaptureController.CaptureSucceeded += OnCaptureSucceeded;
                m_SubscribedCaptureController.CaptureInterrupted -= OnCaptureInterrupted;
                m_SubscribedCaptureController.CaptureInterrupted += OnCaptureInterrupted;
            }
        }

        void OnCaptureSucceeded()
        {
            RefreshAll();
        }

        void OnCaptureInterrupted(string reason)
        {
            RefreshInvestigationPage();
        }

        void EnsureRestartButton()
        {
            if (m_RestartButton == null)
                Debug.LogError("GhostJournalUI requires a scene-authored restart button. Runtime UI creation is disabled.", this);
        }

        void RefreshEvidenceButtons()
        {
            if (m_EvidenceButtonLabels == null)
                return;

            for (var i = 0; i < m_EvidenceButtonLabels.Length; i++)
            {
                var label = m_EvidenceButtonLabels[i];
                if (label == null || !TryGetEvidenceByIndex(i, out var evidenceType))
                    continue;

                var isSelected = m_JournalEvidenceSelection != null && m_JournalEvidenceSelection.IsSelected(evidenceType);
                label.text = isSelected
                    ? $"[X] {FormatEvidenceName(evidenceType)}"
                    : $"[ ] {FormatEvidenceName(evidenceType)}";
            }
        }

        void RefreshGhostSelectionButtons(GhostMatchResult matchResult)
        {
            if (m_GhostSelectionLabels == null)
                return;

            var profiles = GhostProfileCatalog.GetMvpSelectableProfiles();
            for (var i = 0; i < m_GhostSelectionLabels.Length; i++)
            {
                var label = m_GhostSelectionLabels[i];
                if (label == null || i >= profiles.Length)
                    continue;

                var profile = profiles[i];
                var isPossible = IsPossible(matchResult, profile.ghostType);
                label.text = isPossible
                    ? profile.displayName
                    : $"<s>{profile.displayName}</s>";

                if (m_GhostCrossoutImages != null && i < m_GhostCrossoutImages.Length && m_GhostCrossoutImages[i] != null)
                    m_GhostCrossoutImages[i].gameObject.SetActive(!isPossible);
            }
        }

        static string FormatEvidenceInline(EvidenceType[] evidence)
        {
            if (evidence == null || evidence.Length == 0)
                return "None";

            var builder = new StringBuilder();
            for (var i = 0; i < evidence.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(FormatEvidenceName(evidence[i]));
            }

            return builder.ToString();
        }

        static string FormatGhostEliminationList(GhostMatchResult matchResult)
        {
            if (matchResult == null)
                return "Ghosts:\nunknown";

            var builder = new StringBuilder("Ghost Candidates:");
            foreach (var profile in GhostProfileCatalog.GetMvpSelectableProfiles())
            {
                var line = IsPossible(matchResult, profile.ghostType)
                    ? profile.displayName
                    : $"<s>{profile.displayName}</s>";
                builder.AppendLine().Append("- ").Append(line);
            }

            return builder.ToString();
        }

        static bool IsPossible(GhostMatchResult matchResult, GhostType ghostType)
        {
            if (matchResult == null || matchResult.possibleMatches == null)
                return true;

            foreach (var profile in matchResult.possibleMatches)
            {
                if (profile != null && profile.ghostType == ghostType)
                    return true;
            }

            return false;
        }

        static bool TryGetEvidenceByIndex(int index, out EvidenceType evidenceType)
        {
            switch (index)
            {
                case 0:
                    evidenceType = EvidenceType.EMFSpike;
                    return true;
                case 1:
                    evidenceType = EvidenceType.FreezingTemperature;
                    return true;
                case 2:
                    evidenceType = EvidenceType.SpectralTrace;
                    return true;
                case 3:
                    evidenceType = EvidenceType.SpiritResponse;
                    return true;
                default:
                    evidenceType = default;
                    return false;
            }
        }

        static string FormatCaptureOutcome(JournalCaseEntry entry)
        {
            if (entry == null)
                return "Unknown";

            switch (entry.captureOutcome)
            {
                case CaptureOutcome.Success:
                    return $"Success ({entry.captureProgress * 100f:0}% in {entry.captureDurationSeconds:0.0}s)";
                case CaptureOutcome.Interrupted:
                    return $"Interrupted ({entry.captureProgress * 100f:0}% reached)";
                case CaptureOutcome.Failed:
                    return "Failed";
                default:
                    return "Not recorded";
            }
        }

        static string FormatEvidenceName(EvidenceType evidenceType)
        {
            switch (evidenceType)
            {
                case EvidenceType.EMFSpike:
                    return "EMF Spike";
                case EvidenceType.FreezingTemperature:
                    return "Freezing Temperature";
                case EvidenceType.SpectralTrace:
                    return "Spectral Trace";
                case EvidenceType.SpiritResponse:
                    return "Spirit Response";
                default:
                    return evidenceType.ToString();
            }
        }

        static string FormatDifficulty(float difficulty)
        {
            difficulty = Mathf.Clamp01(difficulty);
            if (difficulty < 0.4f)
                return "Low";
            if (difficulty < 0.7f)
                return "Moderate";

            return "High";
        }
    }
}
