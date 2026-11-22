using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TeksMaster.Audio;
using TeksMaster.Characters;
using TeksMaster.Core;
using TeksMaster.Gameplay;
using TeksMaster.Shop;
using TeksMaster.Visuals;
using TeksMaster.World;

namespace TeksMaster.UI
{
    public partial class TeksUIManager
    {
        // In-Game HUD Current Mission Tracker
        private GameObject _missionTrackerPanel;
        private Text _missionTrackerHeaderText;
        private Text _missionTrackerBodyText;
        private Image _missionTrackerFillBar;

        // Navigation State
        private bool _openedSubModalFromStartScreen;
        private bool _openedSettingsFromPause;

        // Pause Menu ([Esc])
        private GameObject _pauseModal;
        private Text _pauseStatusText;

        // Settings Modal
        private GameObject _settingsModal;
        private Text _settingsMasterValText;
        private Image _settingsMasterFill;
        private Text _settingsBgmValText;
        private Image _settingsBgmFill;
        private Text _settingsSfxValText;
        private Image _settingsSfxFill;
        private Text _settingsMuteBtnText;
        private Image _settingsMuteBtnImg;
        private Text _settingsFullscreenBtnText;
        private Image _settingsFullscreenBtnImg;

        // Categorized Missions & Achievements Modal ([Q])
        private int _activeMissionTabIndex;
        private readonly Image[] _missionTabImages = new Image[6];
        private readonly Text[] _missionTabTexts = new Text[6];
        private Text _questsHeaderCurrencyText;

        // Game Modes Modal ([G])
        private GameObject _modesModal;
        private int _activeModesTabIndex;
        private readonly Image[] _modesTabImages = new Image[3];
        private Transform _modesListContent;
        private Text _modesActiveStatusText;
        private Button _modesExitActiveBtn;

        // Dynamic 5-Area Barangay Map Modal ([M])
        private Transform _mapListContent;
        private Button _mapFreePlayToggleBtn;
        private Text _mapFreePlayToggleText;
        private Text _mapHeaderSummaryText;

        // Hidden Card Reveal Celebration Modal
        private GameObject _hiddenCardRevealModal;
        private Image _hiddenRevealCardFrame;
        private Image _hiddenRevealCardArt;
        private Text _hiddenRevealSpotText;
        private Text _hiddenRevealCardNameText;
        private Text _hiddenRevealMetaText;
        private Text _hiddenRevealBonusText;

        private void BuildExtendedUI()
        {
            CreateCurrentMissionTrackerHud();
            CreateModesModal();
            CreatePauseModal();
            CreateSettingsModal();
            CreateHiddenCardRevealModal();
        }

        private void HookExtendedEvents()
        {
            HiddenCardCollectible.OnHiddenCardCollected -= HandleHiddenCardCollected;
            HiddenCardCollectible.OnHiddenCardCollected += HandleHiddenCardCollected;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ExternalInteractHandlerActive = true;
                PlayerController.Instance.OnLockedGateInteracted -= HandleLockedGateInteracted;
                PlayerController.Instance.OnLockedGateInteracted += HandleLockedGateInteracted;
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnAreaUnlocked -= HandleAreaUnlocked;
                PlayerInventory.Instance.OnAreaUnlocked += HandleAreaUnlocked;
                PlayerInventory.Instance.OnAreasChanged -= HandleAreasChanged;
                PlayerInventory.Instance.OnAreasChanged += HandleAreasChanged;
                PlayerInventory.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
                PlayerInventory.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;
            }

            if (TeksGameModeManager.Instance != null)
            {
                TeksGameModeManager.Instance.OnModeStateChanged -= HandleGameModeStateChanged;
                TeksGameModeManager.Instance.OnModeStateChanged += HandleGameModeStateChanged;
                TeksGameModeManager.Instance.OnModeCompleted -= HandleGameModeCompleted;
                TeksGameModeManager.Instance.OnModeCompleted += HandleGameModeCompleted;
            }
        }

        private void UnhookExtendedEvents()
        {
            HiddenCardCollectible.OnHiddenCardCollected -= HandleHiddenCardCollected;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnLockedGateInteracted -= HandleLockedGateInteracted;
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnAreaUnlocked -= HandleAreaUnlocked;
                PlayerInventory.Instance.OnAreasChanged -= HandleAreasChanged;
                PlayerInventory.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
            }

            if (TeksGameModeManager.Instance != null)
            {
                TeksGameModeManager.Instance.OnModeStateChanged -= HandleGameModeStateChanged;
                TeksGameModeManager.Instance.OnModeCompleted -= HandleGameModeCompleted;
            }
        }

        private void RefreshExtendedUI()
        {
            RefreshMissionTrackerHud();

            if (_questsModal != null && _questsModal.activeSelf) PopulateQuestsModal();
            if (_mapModal != null && _mapModal.activeSelf) PopulateMapModal();
            if (_modesModal != null && _modesModal.activeSelf) PopulateModesModal();
            if (_settingsModal != null && _settingsModal.activeSelf) RefreshSettingsModal();
        }

        // =====================================================================
        // 1. EXPANDED MAIN MENU (Play, Collection, Shop, Missions, Modes, Settings, Exit)
        // =====================================================================

        private void CreateStartScreen()
        {
            _startScreenRoot = CreateModalBackdrop("StartGameScreen", new Vector2(780f, 620f), out RectTransform window);
            _startScreenRoot.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.10f, 0.62f);

            var title = CreateText("GameTitle", window, "3D TEKS MASTER", 42, FontStyle.Bold, new Color(1.0f, 0.85f, 0.22f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(20f, -66f);
            title.rectTransform.offsetMax = new Vector2(-20f, -12f);

            var sub = CreateText("GameSub", window, "BARANGAY SHOWDOWN  •  5 Areas • 41 Cards • Flick, Fuse & Rule!", 16, FontStyle.Bold, new Color(0.76f, 0.90f, 1.0f), TextAnchor.MiddleCenter);
            sub.rectTransform.anchorMin = new Vector2(0f, 1f);
            sub.rectTransform.anchorMax = new Vector2(1f, 1f);
            sub.rectTransform.offsetMin = new Vector2(20f, -94f);
            sub.rectTransform.offsetMax = new Vector2(-20f, -66f);

            // Avatar Preview Box
            var previewBox = CreateRoundedPanel("StartAvatarBox", window, new Color(0.14f, 0.20f, 0.30f), new Vector2(132f, 132f));
            previewBox.anchoredPosition = new Vector2(0f, 128f);
            _startPreviewPortrait = CreateImage("StartAvatarImg", previewBox, CharacterResourceGenerator.GetAvatarPortraitSprite(PlayerGender.Male));
            StretchFull(_startPreviewPortrait.rectTransform, 6f);

            // Nickname Label + InputField
            var nickLbl = CreateText("NickLbl", window, "SET YOUR NICKNAME:", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            nickLbl.rectTransform.anchoredPosition = new Vector2(0f, 42f);
            nickLbl.rectTransform.sizeDelta = new Vector2(360f, 24f);

            var inputBg = CreateRoundedPanel("NicknameInputBg", window, new Color(0.06f, 0.09f, 0.16f, 1f), new Vector2(360f, 40f));
            inputBg.anchoredPosition = new Vector2(0f, 8f);
            AddOutline(inputBg.gameObject, new Color(0.42f, 0.62f, 0.92f), 2f);

            _nicknameInput = inputBg.gameObject.AddComponent<InputField>();
            _nicknameInput.characterLimit = 18;
            var placeholderTxt = CreateText("Placeholder", inputBg, "Enter nickname...", 17, FontStyle.Italic, new Color(0.55f, 0.60f, 0.70f), TextAnchor.MiddleCenter);
            StretchFull(placeholderTxt.rectTransform, 6f);
            var inputText = CreateText("Text", inputBg, "", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(inputText.rectTransform, 6f);
            _nicknameInput.textComponent = inputText;
            _nicknameInput.placeholder = placeholderTxt;
            _nicknameInput.text = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerNickname : "TeksMaster";

            // Gender Picker Buttons
            var genLbl = CreateText("GenderLbl", window, "CHOOSE YOUR AVATAR:", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            genLbl.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            genLbl.rectTransform.sizeDelta = new Vector2(360f, 22f);

            var maleBtn = CreateButton("StartMaleBtn", window, "MALE (TOTOY)", new Color(0.18f, 0.50f, 0.88f), 15, () => SelectStartGender(PlayerGender.Male));
            var mRt = maleBtn.GetComponent<RectTransform>();
            mRt.anchoredPosition = new Vector2(-106f, -64f);
            mRt.sizeDelta = new Vector2(196f, 38f);
            _startMaleBtnImg = maleBtn.GetComponent<Image>();

            var femaleBtn = CreateButton("StartFemaleBtn", window, "FEMALE (NENE)", new Color(0.32f, 0.22f, 0.36f), 15, () => SelectStartGender(PlayerGender.Female));
            var fRt = femaleBtn.GetComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(106f, -64f);
            fRt.sizeDelta = new Vector2(196f, 38f);
            _startFemaleBtnImg = femaleBtn.GetComponent<Image>();

            // Primary Play Button
            var playBtn = CreateButton("StartPlayBtn", window, "PLAY BARANGAY SHOWDOWN!", new Color(0.18f, 0.76f, 0.34f), 22, OnStartPlayClicked);
            var pRt = playBtn.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0f, -124f);
            pRt.sizeDelta = new Vector2(460f, 52f);
            AddOutline(playBtn.gameObject, new Color(0.75f, 1.0f, 0.65f), 2f);

            // Secondary Main Menu Navigation Grid (Collection, Shop, Missions, Modes, Settings, Exit)
            var colBtn = CreateButton("MenuCollectionBtn", window, "CARD COLLECTION", new Color(0.20f, 0.48f, 0.82f), 14, () =>
            {
                OpenSubModalFromMainMenu(() =>
                {
                    _inventoryModal.SetActive(true);
                    SwitchInventoryTab(false);
                });
            });
            var colRt = colBtn.GetComponent<RectTransform>();
            colRt.anchoredPosition = new Vector2(-232f, -190f);
            colRt.sizeDelta = new Vector2(216f, 40f);

            var shopMenuBtn = CreateButton("MenuShopBtn", window, "SARI-SARI SHOP", new Color(0.76f, 0.46f, 0.12f), 14, () =>
            {
                OpenSubModalFromMainMenu(() =>
                {
                    PopulateShopModal();
                    _shopModal.SetActive(true);
                });
            });
            var shRt = shopMenuBtn.GetComponent<RectTransform>();
            shRt.anchoredPosition = new Vector2(0f, -190f);
            shRt.sizeDelta = new Vector2(216f, 40f);

            var missionsMenuBtn = CreateButton("MenuMissionsBtn", window, "MISSIONS & BADGES", new Color(0.58f, 0.26f, 0.82f), 14, () =>
            {
                OpenSubModalFromMainMenu(() =>
                {
                    PopulateQuestsModal();
                    _questsModal.SetActive(true);
                });
            });
            var miRt = missionsMenuBtn.GetComponent<RectTransform>();
            miRt.anchoredPosition = new Vector2(232f, -190f);
            miRt.sizeDelta = new Vector2(216f, 40f);

            var modesMenuBtn = CreateButton("MenuModesBtn", window, "GAME MODES", new Color(0.16f, 0.58f, 0.72f), 14, () =>
            {
                OpenSubModalFromMainMenu(() =>
                {
                    PopulateModesModal();
                    _modesModal.SetActive(true);
                });
            });
            var moRt = modesMenuBtn.GetComponent<RectTransform>();
            moRt.anchoredPosition = new Vector2(-232f, -242f);
            moRt.sizeDelta = new Vector2(216f, 40f);

            var settingsMenuBtn = CreateButton("MenuSettingsBtn", window, "SETTINGS", new Color(0.28f, 0.36f, 0.50f), 14, () =>
            {
                OpenSubModalFromMainMenu(() =>
                {
                    _openedSettingsFromPause = false;
                    RefreshSettingsModal();
                    _settingsModal.SetActive(true);
                });
            });
            var seRt = settingsMenuBtn.GetComponent<RectTransform>();
            seRt.anchoredPosition = new Vector2(0f, -242f);
            seRt.sizeDelta = new Vector2(216f, 40f);

            var exitMenuBtn = CreateButton("MenuExitBtn", window, "EXIT GAME", new Color(0.72f, 0.20f, 0.22f), 14, OnExitGameClicked);
            var exRt = exitMenuBtn.GetComponent<RectTransform>();
            exRt.anchoredPosition = new Vector2(232f, -242f);
            exRt.sizeDelta = new Vector2(216f, 40f);

            var initialGender = PlayerInventory.Instance != null ? PlayerInventory.Instance.SelectedGender : PlayerGender.Male;
            SelectStartGender(initialGender);

            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            _openedSubModalFromStartScreen = false;
            _openedSettingsFromPause = false;

            if (_inventoryModal != null) _inventoryModal.SetActive(false);
            if (_shopModal != null) _shopModal.SetActive(false);
            if (_questsModal != null) _questsModal.SetActive(false);
            if (_mapModal != null) _mapModal.SetActive(false);
            if (_modesModal != null) _modesModal.SetActive(false);
            if (_pauseModal != null) _pauseModal.SetActive(false);
            if (_settingsModal != null) _settingsModal.SetActive(false);
            if (_hiddenCardRevealModal != null) _hiddenCardRevealModal.SetActive(false);

            if (PlayerInventory.Instance != null)
            {
                if (_nicknameInput != null) _nicknameInput.text = PlayerInventory.Instance.PlayerNickname;
                SelectStartGender(PlayerInventory.Instance.SelectedGender);
            }

            if (_startScreenRoot != null) _startScreenRoot.SetActive(true);
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow != null) camFollow.IsTitleCameraActive = true;
            }
            SyncPlayerMovementLock();
        }

        private void OpenSubModalFromMainMenu(Action openAction)
        {
            string nick = _nicknameInput != null ? _nicknameInput.text : "TeksMaster";
            PlayerInventory.Instance?.StartGameWithProfile(nick, _pendingStartGender);

            _openedSubModalFromStartScreen = true;
            if (_startScreenRoot != null) _startScreenRoot.SetActive(false);
            openAction?.Invoke();
            SyncPlayerMovementLock();
        }

        private void StartGameFromMenuIfNeeded()
        {
            if (_openedSubModalFromStartScreen || (_startScreenRoot != null && _startScreenRoot.activeSelf))
            {
                string nick = _nicknameInput != null ? _nicknameInput.text : "TeksMaster";
                PlayerInventory.Instance?.StartGameWithProfile(nick, _pendingStartGender);
                _openedSubModalFromStartScreen = false;
                if (_startScreenRoot != null) _startScreenRoot.SetActive(false);
                if (Camera.main != null)
                {
                    var camFollow = Camera.main.GetComponent<CameraFollowController>();
                    if (camFollow != null) camFollow.IsTitleCameraActive = false;
                }
            }
        }

        private void SelectStartGender(PlayerGender gender)
        {
            _pendingStartGender = gender;
            if (_startPreviewPortrait != null) _startPreviewPortrait.sprite = CharacterResourceGenerator.GetAvatarPortraitSprite(gender);
            if (_startMaleBtnImg != null) _startMaleBtnImg.color = gender == PlayerGender.Male ? new Color(0.18f, 0.56f, 0.94f) : new Color(0.20f, 0.24f, 0.32f);
            if (_startFemaleBtnImg != null) _startFemaleBtnImg.color = gender == PlayerGender.Female ? new Color(0.88f, 0.30f, 0.58f) : new Color(0.20f, 0.24f, 0.32f);
            PlayerInventory.Instance?.SetGender(gender);
        }

        private void OnStartPlayClicked()
        {
            string nick = _nicknameInput != null ? _nicknameInput.text : "TeksMaster";
            PlayerInventory.Instance?.StartGameWithProfile(nick, _pendingStartGender);

            _openedSubModalFromStartScreen = false;
            if (_startScreenRoot != null) _startScreenRoot.SetActive(false);
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow != null) camFollow.IsTitleCameraActive = false;
            }
            SyncPlayerMovementLock();

            string finalNick = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerNickname : "TeksMaster";
            ShowToast($"Maligayang pagdating, {finalNick}! Challenge Dodong & Mark or explore for Hidden Cards!");
            RefreshAllUI();
        }

        private void OnExitGameClicked()
        {
            PlayerInventory.Instance?.SaveToDisk();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // =====================================================================
        // 2. IN-GAME HUD CURRENT MISSION TRACKER
        // =====================================================================

        private void CreateCurrentMissionTrackerHud()
        {
            var panel = CreateRoundedPanel("CurrentMissionTrackerHUD", _canvas.transform, new Color(0.09f, 0.14f, 0.22f, 0.92f), new Vector2(455f, 70f));
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(22f, -190f);
            AddOutline(panel.gameObject, new Color(0.92f, 0.72f, 0.20f), 1.5f);
            _missionTrackerPanel = panel.gameObject;

            var btn = panel.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(ToggleQuestsModal);

            _missionTrackerHeaderText = CreateText("TrackerHeader", panel, "ACTIVE MISSION [Q]: First Barangay Win", 14, FontStyle.Bold, new Color(1.0f, 0.88f, 0.32f), TextAnchor.MiddleLeft);
            _missionTrackerHeaderText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _missionTrackerHeaderText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _missionTrackerHeaderText.rectTransform.offsetMin = new Vector2(12f, -26f);
            _missionTrackerHeaderText.rectTransform.offsetMax = new Vector2(-12f, -4f);

            _missionTrackerBodyText = CreateText("TrackerBody", panel, "Defeat Dodong in Player's Neighborhood  |  0/1  (+120c +5 Gems)", 12, FontStyle.Normal, new Color(0.84f, 0.92f, 1.0f), TextAnchor.MiddleLeft);
            _missionTrackerBodyText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _missionTrackerBodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _missionTrackerBodyText.rectTransform.offsetMin = new Vector2(12f, -48f);
            _missionTrackerBodyText.rectTransform.offsetMax = new Vector2(-12f, -26f);

            var barBg = CreateRoundedPanel("TrackerBarBg", panel, new Color(0.05f, 0.08f, 0.14f, 0.96f), Vector2.zero);
            barBg.anchorMin = new Vector2(0f, 0f);
            barBg.anchorMax = new Vector2(1f, 0f);
            barBg.offsetMin = new Vector2(12f, 7f);
            barBg.offsetMax = new Vector2(-12f, 17f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barBg, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0.5f, 1f);
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            _missionTrackerFillBar = fillGo.GetComponent<Image>();
            _missionTrackerFillBar.color = new Color(0.24f, 0.86f, 0.44f);
        }

        private void RefreshMissionTrackerHud()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _missionTrackerHeaderText == null || _missionTrackerBodyText == null) return;

            var q = inv.GetActiveTrackerMission();
            if (q == null)
            {
                _missionTrackerHeaderText.text = "ALL MISSIONS COMPLETE! [Q]";
                _missionTrackerHeaderText.color = new Color(0.38f, 0.98f, 0.54f);
                _missionTrackerBodyText.text = "You have completed every Barangay mission and achievement!";
                if (_missionTrackerFillBar != null)
                {
                    _missionTrackerFillBar.rectTransform.anchorMax = new Vector2(1f, 1f);
                    _missionTrackerFillBar.color = new Color(0.32f, 0.94f, 0.48f);
                }
                return;
            }

            string catName = GetMissionCategoryShortName(q.Category);
            bool readyToClaim = q.IsCompleted && !q.IsClaimed;
            int claimableTotal = inv.GetUnclaimedCompletedQuestCount();

            if (readyToClaim)
            {
                _missionTrackerHeaderText.text = $"[{catName}] {q.Title}  •  CLAIM READY! ({claimableTotal}!)";
                _missionTrackerHeaderText.color = new Color(0.38f, 1.0f, 0.52f);
            }
            else
            {
                _missionTrackerHeaderText.text = $"[{catName}] {q.Title}  ({q.CurrentProgress}/{q.TargetCount})";
                _missionTrackerHeaderText.color = new Color(1.0f, 0.88f, 0.32f);
            }

            _missionTrackerBodyText.text = $"{q.Description}  •  {FormatQuestRewardSummary(q)}";

            if (_missionTrackerFillBar != null)
            {
                float ratio = Mathf.Clamp01((float)q.CurrentProgress / Mathf.Max(1, q.TargetCount));
                _missionTrackerFillBar.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, ratio), 1f);
                _missionTrackerFillBar.color = readyToClaim ? new Color(1.0f, 0.84f, 0.22f) : new Color(0.24f, 0.84f, 0.46f);
            }
        }

        // =====================================================================
        // 3. CATEGORIZED MISSIONS & ACHIEVEMENTS MODAL ([Q])
        // =====================================================================

        private void CreateQuestsModal()
        {
            _questsModal = CreateModalBackdrop("QuestsModal", new Vector2(940f, 640f), out RectTransform window);

            var header = CreateText("QuestsHeader", window, "BARANGAY MISSIONS & ACHIEVEMENTS [Q]", 25, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            header.rectTransform.offsetMin = new Vector2(26f, -54f);
            header.rectTransform.offsetMax = new Vector2(0f, -12f);

            _questsHeaderCurrencyText = CreateText("QuestsCurrency", window, "2,450 COINS  |  25 GEMS", 18, FontStyle.Bold, new Color(0.84f, 0.94f, 1.0f), TextAnchor.MiddleRight);
            _questsHeaderCurrencyText.rectTransform.anchorMin = new Vector2(0.58f, 1f);
            _questsHeaderCurrencyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _questsHeaderCurrencyText.rectTransform.offsetMin = new Vector2(0f, -54f);
            _questsHeaderCurrencyText.rectTransform.offsetMax = new Vector2(-82f, -12f);

            var closeBtn = CreateButton("CloseQuestsBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            // 6 Category Filter Tabs: ALL, STORY, DAILY, COLLECTION, EXPLORATION, ACHIEVEMENTS
            string[] tabLabels = { "ALL", "MAIN STORY", "DAILY", "COLLECTION", "EXPLORATION", "ACHIEVEMENTS" };
            for (int i = 0; i < 6; i++)
            {
                int capturedTab = i;
                var tabBtn = CreateButton("MissionTab_" + i, window, tabLabels[i], new Color(0.18f, 0.26f, 0.38f), 13, () =>
                {
                    _activeMissionTabIndex = capturedTab;
                    PopulateQuestsModal();
                });
                var tRt = tabBtn.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 1f);
                tRt.anchorMax = new Vector2(0f, 1f);
                tRt.anchoredPosition = new Vector2(96f + i * 149f, -82f);
                tRt.sizeDelta = new Vector2(141f, 34f);
                _missionTabImages[i] = tabBtn.GetComponent<Image>();
                _missionTabTexts[i] = tabBtn.GetComponentInChildren<Text>();
            }

            var scroll = CreateScrollView("QuestsScrollView", window, new Vector2(24f, 22f), new Vector2(-24f, -110f), out _questsListContent);
            var vlg = _questsListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = _questsListContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _questsModal.SetActive(false);
        }

        private void ToggleQuestsModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = _questsModal != null && !_questsModal.activeSelf;
            CloseAllModals();
            if (next)
            {
                PopulateQuestsModal();
                _questsModal.SetActive(true);
                SyncPlayerMovementLock();
            }
        }

        private void PopulateQuestsModal()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _questsListContent == null) return;

            if (_questsHeaderCurrencyText != null)
            {
                _questsHeaderCurrencyText.text = $"{inv.Coins:N0} COINS  |  {inv.Gems:N0} GEMS";
            }

            string[] baseLabels = { "ALL", "MAIN STORY", "DAILY", "COLLECTION", "EXPLORATION", "ACHIEVEMENTS" };
            MissionCategory[] tabCats =
            {
                MissionCategory.MainStory,
                MissionCategory.MainStory,
                MissionCategory.Daily,
                MissionCategory.Collection,
                MissionCategory.Exploration,
                MissionCategory.Achievement
            };

            for (int i = 0; i < 6; i++)
            {
                int claimCount = i == 0 ? inv.GetUnclaimedCompletedQuestCount() : inv.GetUnclaimedCompletedQuestCount(tabCats[i]);
                if (_missionTabTexts[i] != null)
                {
                    _missionTabTexts[i].text = claimCount > 0 ? $"{baseLabels[i]} ({claimCount}!)" : baseLabels[i];
                }
                if (_missionTabImages[i] != null)
                {
                    bool active = _activeMissionTabIndex == i;
                    _missionTabImages[i].color = active ? new Color(0.86f, 0.52f, 0.12f) : new Color(0.16f, 0.22f, 0.34f);
                }
            }

            for (int i = _questsListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_questsListContent.GetChild(i).gameObject);
            }

            IReadOnlyList<BarangayQuest> quests = _activeMissionTabIndex == 0
                ? (IReadOnlyList<BarangayQuest>)inv.Quests
                : inv.GetQuestsByCategory(tabCats[_activeMissionTabIndex]);

            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                if (q == null) continue;

                bool canClaim = q.IsCompleted && !q.IsClaimed;
                Color rowBg = q.IsClaimed
                    ? new Color(0.10f, 0.14f, 0.21f, 0.92f)
                    : (canClaim ? new Color(0.15f, 0.26f, 0.20f, 1f) : new Color(0.13f, 0.18f, 0.28f, 1f));
                Color borderCol = canClaim
                    ? new Color(1.0f, 0.84f, 0.22f)
                    : (q.IsClaimed ? new Color(0.24f, 0.30f, 0.40f) : GetMissionCategoryColor(q.Category));

                var row = CreateRoundedPanel("QuestRow_" + q.Id, _questsListContent, rowBg, new Vector2(860f, 98f));
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 98f;
                AddOutline(row.gameObject, borderCol, canClaim ? 2.5f : 1.5f);

                string catTag = GetMissionCategoryShortName(q.Category);
                string dailyTag = q.IsDaily ? " • DAILY RESET" : string.Empty;
                var titleTxt = CreateText(
                    "Title",
                    row,
                    $"[{catTag}{dailyTag}]  {q.Title}",
                    16,
                    FontStyle.Bold,
                    canClaim ? new Color(1.0f, 0.92f, 0.38f) : Color.white,
                    TextAnchor.MiddleLeft
                );
                titleTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                titleTxt.rectTransform.anchorMax = new Vector2(0.75f, 1f);
                titleTxt.rectTransform.offsetMin = new Vector2(18f, -32f);
                titleTxt.rectTransform.offsetMax = new Vector2(-8f, -6f);

                var descTxt = CreateText(
                    "Desc",
                    row,
                    $"{q.Description}\nProgress: {q.CurrentProgress} / {q.TargetCount}   |   Rewards: {FormatQuestRewardSummary(q)}",
                    13,
                    FontStyle.Normal,
                    new Color(0.84f, 0.92f, 1.0f),
                    TextAnchor.MiddleLeft
                );
                descTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                descTxt.rectTransform.anchorMax = new Vector2(0.75f, 1f);
                descTxt.rectTransform.offsetMin = new Vector2(18f, 8f);
                descTxt.rectTransform.offsetMax = new Vector2(-8f, -32f);

                string capturedId = q.Id;
                string btnLbl = q.IsClaimed ? "CLAIMED ✓" : (canClaim ? "CLAIM REWARD!" : $"{q.CurrentProgress} / {q.TargetCount}");
                Color btnCol = q.IsClaimed
                    ? new Color(0.22f, 0.26f, 0.32f)
                    : (canClaim ? new Color(0.18f, 0.76f, 0.34f) : new Color(0.26f, 0.32f, 0.44f));

                var claimBtn = CreateButton("ClaimBtn", row, btnLbl, btnCol, 14, () =>
                {
                    if (inv.TryClaimQuestById(capturedId, out string msg, out List<TeksCardDefinition> grantedCards))
                    {
                        TeksAudioManager.Instance?.PlayCoinClink();
                        if (grantedCards != null && grantedCards.Count > 0)
                        {
                            TeksAudioManager.Instance?.PlayPackOpen();
                        }
                        ShowToast(msg);
                        PopulateQuestsModal();
                        RefreshMissionTrackerHud();
                    }
                    else
                    {
                        ShowToast(msg);
                    }
                });
                var bRt = claimBtn.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0.5f);
                bRt.anchorMax = new Vector2(1f, 0.5f);
                bRt.anchoredPosition = new Vector2(-102f, 0f);
                bRt.sizeDelta = new Vector2(174f, 46f);
                claimBtn.interactable = canClaim;
            }
        }

        private static string GetMissionCategoryShortName(MissionCategory cat)
        {
            switch (cat)
            {
                case MissionCategory.MainStory: return "MAIN STORY";
                case MissionCategory.Daily: return "DAILY";
                case MissionCategory.Collection: return "COLLECTION";
                case MissionCategory.Exploration: return "EXPLORATION";
                case MissionCategory.Achievement: return "ACHIEVEMENT";
                default: return "MISSION";
            }
        }

        private static Color GetMissionCategoryColor(MissionCategory cat)
        {
            switch (cat)
            {
                case MissionCategory.MainStory: return new Color(0.96f, 0.68f, 0.18f);
                case MissionCategory.Daily: return new Color(0.28f, 0.78f, 0.96f);
                case MissionCategory.Collection: return new Color(0.68f, 0.36f, 0.94f);
                case MissionCategory.Exploration: return new Color(0.28f, 0.88f, 0.52f);
                case MissionCategory.Achievement: return new Color(0.96f, 0.38f, 0.42f);
                default: return new Color(0.36f, 0.48f, 0.64f);
            }
        }

        private static string FormatQuestRewardSummary(BarangayQuest q)
        {
            if (q == null) return string.Empty;
            var parts = new List<string>();
            if (q.CoinReward > 0) parts.Add($"+{q.CoinReward} Coins");
            if (q.GemReward > 0) parts.Add($"+{q.GemReward} Gems");
            if (q.HasPackReward) parts.Add($"+{q.PackRewardType} Pack");
            if (!string.IsNullOrEmpty(q.SpecialCardRewardId))
            {
                var cDef = TeksDatabase.GetCard(q.SpecialCardRewardId);
                parts.Add(cDef != null ? $"+{cDef.Name} Card" : "+Special Card");
            }
            return parts.Count > 0 ? string.Join("  ", parts) : "+Reward";
        }

        // =====================================================================
        // 4. GAME MODES MODAL ([G] — Quick Battle, 4-Round Tournament, 3 Challenges)
        // =====================================================================

        private void CreateModesModal()
        {
            _modesModal = CreateModalBackdrop("GameModesModal", new Vector2(940f, 640f), out RectTransform window);

            var header = CreateText("ModesHeader", window, "BARANGAY GAME MODES [G]", 25, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.55f, 1f);
            header.rectTransform.offsetMin = new Vector2(26f, -54f);
            header.rectTransform.offsetMax = new Vector2(0f, -12f);

            _modesActiveStatusText = CreateText("ModesActiveStatus", window, "ACTIVE MODE: STORY EXPLORATION", 15, FontStyle.Bold, new Color(0.68f, 0.94f, 0.76f), TextAnchor.MiddleRight);
            _modesActiveStatusText.rectTransform.anchorMin = new Vector2(0.45f, 1f);
            _modesActiveStatusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _modesActiveStatusText.rectTransform.offsetMin = new Vector2(0f, -54f);
            _modesActiveStatusText.rectTransform.offsetMax = new Vector2(-215f, -12f);

            _modesExitActiveBtn = CreateButton("ExitActiveModeBtn", window, "EXIT ACTIVE MODE", new Color(0.76f, 0.28f, 0.24f), 12, () =>
            {
                TeksGameModeManager.Instance?.ExitActiveMode(true);
                ShowToast("Returned to Story Mode exploration.");
                PopulateModesModal();
            });
            var exRt = _modesExitActiveBtn.GetComponent<RectTransform>();
            exRt.anchorMin = new Vector2(1f, 1f);
            exRt.anchorMax = new Vector2(1f, 1f);
            exRt.anchoredPosition = new Vector2(-132f, -34f);
            exRt.sizeDelta = new Vector2(138f, 34f);

            var closeBtn = CreateButton("CloseModesBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            string[] modeTabs =
            {
                "QUICK BATTLE (FREE PLAY)",
                "TOURNAMENT (4 ROUNDS)",
                "CHALLENGE MODE (3 RULES)"
            };

            for (int i = 0; i < 3; i++)
            {
                int capturedIdx = i;
                var btn = CreateButton("ModeTab_" + i, window, modeTabs[i], new Color(0.18f, 0.28f, 0.42f), 14, () =>
                {
                    _activeModesTabIndex = capturedIdx;
                    PopulateModesModal();
                });
                var bRt = btn.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 1f);
                bRt.anchorMax = new Vector2(0f, 1f);
                bRt.anchoredPosition = new Vector2(174f + i * 296f, -82f);
                bRt.sizeDelta = new Vector2(282f, 36f);
                _modesTabImages[i] = btn.GetComponent<Image>();
            }

            CreateScrollView("ModesScrollView", window, new Vector2(24f, 22f), new Vector2(-24f, -112f), out _modesListContent);
            var vlg = _modesListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = _modesListContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _modesModal.SetActive(false);
        }

        public void ToggleModesModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = _modesModal != null && !_modesModal.activeSelf;
            CloseAllModals();
            if (next)
            {
                PopulateModesModal();
                _modesModal.SetActive(true);
                SyncPlayerMovementLock();
            }
        }

        private void PopulateModesModal()
        {
            if (_modesListContent == null) return;
            var gm = TeksGameModeManager.Instance;
            var inv = PlayerInventory.Instance;

            for (int i = 0; i < 3; i++)
            {
                if (_modesTabImages[i] != null)
                {
                    _modesTabImages[i].color = (_activeModesTabIndex == i)
                        ? new Color(0.18f, 0.58f, 0.86f)
                        : new Color(0.16f, 0.22f, 0.34f);
                }
            }

            bool specialActive = gm != null && gm.IsSpecialModeActive;
            if (_modesActiveStatusText != null)
            {
                _modesActiveStatusText.text = specialActive ? gm.GetModeStatusBanner() : "ACTIVE MODE: STORY EXPLORATION";
            }
            if (_modesExitActiveBtn != null)
            {
                _modesExitActiveBtn.gameObject.SetActive(specialActive);
            }

            for (int i = _modesListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_modesListContent.GetChild(i).gameObject);
            }

            if (_activeModesTabIndex == 0)
            {
                PopulateQuickBattleTab(gm, inv);
            }
            else if (_activeModesTabIndex == 1)
            {
                PopulateTournamentTab(gm, inv);
            }
            else
            {
                PopulateChallengeTab(gm, inv);
            }
        }

        private void PopulateQuickBattleTab(TeksGameModeManager gm, PlayerInventory inv)
        {
            var infoRow = CreateRoundedPanel("QuickBattleInfo", _modesListContent, new Color(0.11f, 0.19f, 0.29f, 1f), new Vector2(860f, 64f));
            infoRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            var infoTxt = CreateText(
                "InfoTxt",
                infoRow,
                "QUICK BATTLE / FREE PLAY ROSTER: Instant same-area 2-card Teks match against any unlocked Barangay opponent!\nEnable 'Free Play: Unlock All Areas' in the Barangay Map [M] to duel every opponent immediately.",
                13,
                FontStyle.Bold,
                new Color(0.82f, 0.94f, 1.0f),
                TextAnchor.MiddleLeft
            );
            StretchFull(infoTxt.rectTransform, 12f);

            List<NpcOpponent> opponents = gm != null
                ? gm.GetAllOpponents()
                : (BarangayCourtBuilder.Instance != null ? BarangayCourtBuilder.Instance.SpawnedOpponents : new List<NpcOpponent>());

            for (int i = 0; i < opponents.Count; i++)
            {
                var npc = opponents[i];
                if (npc == null) continue;

                bool unlocked = gm != null ? gm.IsOpponentUnlocked(npc) : (inv == null || inv.IsAreaUnlocked(npc.AreaId));
                bool defeated = inv != null && inv.HasDefeatedNpc(npc.GetEffectiveNpcId());
                int baseReward = TeksMatchManager.GetBaseCoinReward(npc);
                var favCard = TeksDatabase.GetCard(npc.FavoriteCardId);
                string favCardLabel = favCard != null ? $"{favCard.Name} ({TeksDatabase.GetRarityName(favCard.Rarity)})" : npc.FavoriteCardId;
                string areaName = BarangayCourtBuilder.GetAreaDisplayName(npc.AreaId);

                var row = CreateRoundedPanel("QB_" + npc.GetEffectiveNpcId(), _modesListContent, unlocked ? new Color(0.13f, 0.18f, 0.28f, 1f) : new Color(0.10f, 0.12f, 0.18f, 0.9f), new Vector2(860f, 84f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;
                AddOutline(row.gameObject, unlocked ? npc.GetDifficultyColor() : new Color(0.28f, 0.32f, 0.40f), 2f);

                string storyBadge = npc.IsChampion ? " [CHAMPION]" : (npc.IsStoryOpponent ? " [STORY]" : string.Empty);
                string defeatedBadge = defeated ? "  •  ✓ DEFEATED" : string.Empty;

                var titleTxt = CreateText(
                    "Title",
                    row,
                    $"{npc.GetDisplayTag().ToUpper()}{storyBadge}   |   Area: {areaName}{defeatedBadge}",
                    15,
                    FontStyle.Bold,
                    unlocked ? Color.white : new Color(0.62f, 0.68f, 0.76f),
                    TextAnchor.MiddleLeft
                );
                titleTxt.rectTransform.anchorMin = new Vector2(0f, 0.50f);
                titleTxt.rectTransform.anchorMax = new Vector2(0.74f, 1f);
                titleTxt.rectTransform.offsetMin = new Vector2(18f, 0f);
                titleTxt.rectTransform.offsetMax = new Vector2(-8f, -6f);

                string subLine = unlocked
                    ? $"Pamato: {favCardLabel}  |  AI Front Odds: {Mathf.RoundToInt(npc.GetAiChubProbability() * 100f)}%  |  Win Reward: +{baseReward} Coins"
                    : $"LOCKED — {inv?.GetAreaUnlockRequirementText(npc.AreaId)}";

                var subTxt = CreateText("Sub", row, subLine, 13, FontStyle.Normal, unlocked ? new Color(0.80f, 0.90f, 1.0f) : new Color(0.94f, 0.62f, 0.42f), TextAnchor.MiddleLeft);
                subTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                subTxt.rectTransform.anchorMax = new Vector2(0.74f, 0.52f);
                subTxt.rectTransform.offsetMin = new Vector2(18f, 6f);
                subTxt.rectTransform.offsetMax = new Vector2(-8f, 0f);

                var capturedNpc = npc;
                var challengeBtn = CreateButton(
                    "QuickDuelBtn",
                    row,
                    unlocked ? "CHALLENGE NOW" : "AREA LOCKED",
                    unlocked ? new Color(0.18f, 0.72f, 0.34f) : new Color(0.25f, 0.28f, 0.34f),
                    13,
                    () =>
                    {
                        if (!unlocked)
                        {
                            ShowToast(inv != null ? inv.GetAreaUnlockRequirementText(capturedNpc.AreaId) : "Unlock this area first!");
                            return;
                        }
                        StartGameFromMenuIfNeeded();
                        CloseAllModals();
                        if (gm != null && gm.StartQuickBattle(capturedNpc, out string msg))
                        {
                            ShowToast(msg);
                        }
                        else if (TeksMatchManager.Instance != null)
                        {
                            TeksMatchManager.Instance.StartChallenge(capturedNpc);
                        }
                    }
                );
                var cRt = challengeBtn.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(1f, 0.5f);
                cRt.anchorMax = new Vector2(1f, 0.5f);
                cRt.anchoredPosition = new Vector2(-102f, 0f);
                cRt.sizeDelta = new Vector2(174f, 44f);
                challengeBtn.interactable = unlocked;
            }
        }

        private void PopulateTournamentTab(TeksGameModeManager gm, PlayerInventory inv)
        {
            int tourneysWon = inv != null ? inv.TournamentsWon : 0;
            var banner = CreateRoundedPanel("TourneyBanner", _modesListContent, new Color(0.18f, 0.14f, 0.08f, 1f), new Vector2(860f, 94f));
            banner.gameObject.AddComponent<LayoutElement>().preferredHeight = 94f;
            AddOutline(banner.gameObject, new Color(1.0f, 0.82f, 0.22f), 2f);

            var bTxt = CreateText(
                "TourneyBannerTxt",
                banner,
                $"BARANGAY 4-ROUND CHAMPIONSHIP TOURNAMENT   (Trophies Won: {tourneysWon})\nWin 4 consecutive rounds (Easy -> Medium -> Hard -> Extreme Champion)!\nChampion Trophy Prize: +{TeksGameModeManager.TournamentTrophyCoins} Barya Coins  &  +{TeksGameModeManager.TournamentTrophyGems} Gems!",
                15,
                FontStyle.Bold,
                new Color(1.0f, 0.92f, 0.52f),
                TextAnchor.MiddleLeft
            );
            bTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            bTxt.rectTransform.anchorMax = new Vector2(0.72f, 1f);
            bTxt.rectTransform.offsetMin = new Vector2(18f, 8f);
            bTxt.rectTransform.offsetMax = new Vector2(-8f, -8f);

            bool hasWaitingRound = gm != null && gm.ActiveMode == GameModeType.Tournament && gm.RunState == ModeRunState.AwaitingNextRound;
            string startLabel = hasWaitingRound ? $"PLAY ROUND {gm.TournamentCurrentRoundIndex + 1} / 4" : "START NEW TOURNAMENT!";

            var startBtn = CreateButton("StartTourneyBtn", banner, startLabel, new Color(0.88f, 0.56f, 0.10f), 15, () =>
            {
                if (gm == null) return;
                StartGameFromMenuIfNeeded();
                CloseAllModals();
                if (hasWaitingRound)
                {
                    gm.StartNextTournamentRound();
                }
                else if (gm.StartTournament(out string msg))
                {
                    ShowToast(msg);
                }
                else
                {
                    ShowToast(msg);
                }
            });
            var sRt = startBtn.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 0.5f);
            sRt.anchorMax = new Vector2(1f, 0.5f);
            sRt.anchoredPosition = new Vector2(-116f, 0f);
            sRt.sizeDelta = new Vector2(204f, 50f);

            IReadOnlyList<NpcOpponent> bracket = (gm != null && gm.TournamentBracket != null && gm.TournamentBracket.Count > 0)
                ? gm.TournamentBracket
                : (gm != null ? gm.PreviewTournamentBracket() : new List<NpcOpponent>());

            string[] roundTitles =
            {
                "ROUND 1 — QUARTERFINAL (EASY)",
                "ROUND 2 — SEMIFINAL (MEDIUM)",
                "ROUND 3 — UPPER FINAL (HARD)",
                "ROUND 4 — GRAND FINALS (EXTREME CHAMPION)"
            };

            bool tourneyActive = gm != null && gm.ActiveMode == GameModeType.Tournament && gm.RunState != ModeRunState.Inactive;
            int currentRound = tourneyActive ? gm.TournamentCurrentRoundIndex : 0;
            int winsCount = tourneyActive ? gm.TournamentWinsCount : 0;

            for (int i = 0; i < bracket.Count; i++)
            {
                var opp = bracket[i];
                if (opp == null) continue;

                string rTitle = i < roundTitles.Length ? roundTitles[i] : $"ROUND {i + 1}";
                bool isCompleted = tourneyActive && i < winsCount;
                bool isCurrent = tourneyActive && i == currentRound && gm.RunState != ModeRunState.Victory;

                Color rowCol = isCompleted
                    ? new Color(0.12f, 0.24f, 0.16f, 1f)
                    : (isCurrent ? new Color(0.20f, 0.22f, 0.12f, 1f) : new Color(0.12f, 0.17f, 0.26f, 1f));

                var row = CreateRoundedPanel("TourneyRound_" + i, _modesListContent, rowCol, new Vector2(860f, 82f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;
                AddOutline(row.gameObject, isCurrent ? new Color(1.0f, 0.85f, 0.22f) : opp.GetDifficultyColor(), isCurrent ? 2.5f : 1.5f);

                string statusTag = isCompleted ? "COMPLETED ✓" : (isCurrent ? "UP NEXT" : "BRACKET SEED");
                var cardDef = TeksDatabase.GetCard(opp.FavoriteCardId);
                string cardName = cardDef != null ? cardDef.Name : opp.FavoriteCardId;

                var txt = CreateText(
                    "RoundTxt",
                    row,
                    $"{rTitle}   •   [{statusTag}]\nOpponent: {opp.GetDisplayTag()}  |  Pamato: {cardName}  |  Match Reward: +{TeksMatchManager.GetBaseCoinReward(opp)} Coins",
                    14,
                    FontStyle.Bold,
                    Color.white,
                    TextAnchor.MiddleLeft
                );
                StretchFull(txt.rectTransform, 16f);
            }
        }

        private void PopulateChallengeTab(TeksGameModeManager gm, PlayerInventory inv)
        {
            int challengesDone = inv != null ? inv.ChallengesCompleted : 0;
            var headerBox = CreateRoundedPanel("ChallengeBanner", _modesListContent, new Color(0.16f, 0.12f, 0.26f, 1f), new Vector2(860f, 62f));
            headerBox.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
            var hTxt = CreateText(
                "ChallengeHdrTxt",
                headerBox,
                $"SPECIAL RULE CHALLENGES   (Challenges Cleared: {challengesDone})\nTest your Teks mastery under special rules to earn bonus Barya Coins & Gems!",
                14,
                FontStyle.Bold,
                new Color(0.92f, 0.84f, 1.0f),
                TextAnchor.MiddleLeft
            );
            StretchFull(hTxt.rectTransform, 12f);

            if (gm == null) return;
            var infos = TeksGameModeManager.GetAllChallengeInfos();
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                bool canStart = gm.CanStartChallenge(info.RuleType, out string blockReason);

                var row = CreateRoundedPanel("Challenge_" + info.RuleType, _modesListContent, new Color(0.13f, 0.18f, 0.28f, 1f), new Vector2(860f, 108f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 108f;
                AddOutline(row.gameObject, new Color(0.72f, 0.42f, 0.98f), 2f);

                var titleTxt = CreateText(
                    "Title",
                    row,
                    $"{info.Title.ToUpper()}   •   [{info.RuleSummary}]",
                    16,
                    FontStyle.Bold,
                    new Color(1.0f, 0.88f, 0.34f),
                    TextAnchor.MiddleLeft
                );
                titleTxt.rectTransform.anchorMin = new Vector2(0f, 0.58f);
                titleTxt.rectTransform.anchorMax = new Vector2(0.74f, 1f);
                titleTxt.rectTransform.offsetMin = new Vector2(18f, 0f);
                titleTxt.rectTransform.offsetMax = new Vector2(-8f, -6f);

                var descTxt = CreateText(
                    "Desc",
                    row,
                    $"{info.Description}\nGoal: {info.TargetWins} Consecutive Wins   |   Completion Bonus: +{info.CoinReward} Barya Coins  &  +{info.GemReward} Gems",
                    13,
                    FontStyle.Normal,
                    new Color(0.84f, 0.92f, 1.0f),
                    TextAnchor.MiddleLeft
                );
                descTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                descTxt.rectTransform.anchorMax = new Vector2(0.74f, 0.60f);
                descTxt.rectTransform.offsetMin = new Vector2(18f, 8f);
                descTxt.rectTransform.offsetMax = new Vector2(-8f, 0f);

                var capturedRule = info.RuleType;
                var startBtn = CreateButton(
                    "StartChallengeBtn",
                    row,
                    canStart ? "START CHALLENGE" : "NEED COMMON CARD",
                    canStart ? new Color(0.58f, 0.24f, 0.88f) : new Color(0.28f, 0.28f, 0.34f),
                    13,
                    () =>
                    {
                        if (!gm.CanStartChallenge(capturedRule, out string reason))
                        {
                            ShowToast(reason);
                            return;
                        }
                        StartGameFromMenuIfNeeded();
                        CloseAllModals();
                        if (gm.StartChallengeMode(capturedRule, out string msg))
                        {
                            ShowToast(msg);
                        }
                        else
                        {
                            ShowToast(msg);
                        }
                    }
                );
                var sRt = startBtn.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(1f, 0.5f);
                sRt.anchorMax = new Vector2(1f, 0.5f);
                sRt.anchoredPosition = new Vector2(-104f, 0f);
                sRt.sizeDelta = new Vector2(180f, 46f);
                startBtn.interactable = canStart;
            }
        }

        // =====================================================================
        // 5. DYNAMIC 5-AREA BARANGAY MAP MODAL ([M])
        // =====================================================================

        private void CreateMapModal()
        {
            _mapModal = CreateModalBackdrop("BarangayMapModal", new Vector2(880f, 630f), out RectTransform window);

            var header = CreateText("MapHeader", window, "BARANGAY 5-AREA MAP & FAST TRAVEL [M]", 24, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.56f, 1f);
            header.rectTransform.offsetMin = new Vector2(26f, -52f);
            header.rectTransform.offsetMax = new Vector2(0f, -10f);

            _mapHeaderSummaryText = CreateText("MapSummary", window, "Hidden Cards Found: 0 / 12", 15, FontStyle.Bold, new Color(0.68f, 0.94f, 1.0f), TextAnchor.MiddleLeft);
            _mapHeaderSummaryText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _mapHeaderSummaryText.rectTransform.anchorMax = new Vector2(0.56f, 1f);
            _mapHeaderSummaryText.rectTransform.offsetMin = new Vector2(26f, -78f);
            _mapHeaderSummaryText.rectTransform.offsetMax = new Vector2(0f, -50f);

            _mapFreePlayToggleBtn = CreateButton("FreePlayToggleBtn", window, "FREE PLAY UNLOCK ALL: OFF", new Color(0.22f, 0.36f, 0.54f), 13, () =>
            {
                var inv = PlayerInventory.Instance;
                if (inv == null) return;
                inv.ToggleFreePlayUnlockAll();
                BarangayCourtBuilder.Instance?.RefreshAreaGates();
                ShowToast(inv.FreePlayUnlockAll
                    ? "Free Play Unlock All ENABLED — All 5 Barangay Areas & Gates opened!"
                    : "Free Play Unlock All DISABLED — Story Progression area locks active.");
                PopulateMapModal();
            });
            var fpRt = _mapFreePlayToggleBtn.GetComponent<RectTransform>();
            fpRt.anchorMin = new Vector2(1f, 1f);
            fpRt.anchorMax = new Vector2(1f, 1f);
            fpRt.anchoredPosition = new Vector2(-196f, -42f);
            fpRt.sizeDelta = new Vector2(246f, 38f);
            _mapFreePlayToggleText = _mapFreePlayToggleBtn.GetComponentInChildren<Text>();

            var closeBtn = CreateButton("CloseMapBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            var listGo = new GameObject("ZonesList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(window, false);
            var lRt = listGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(26f, 22f);
            lRt.offsetMax = new Vector2(-26f, -88f);
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            _mapListContent = listGo.transform;

            PopulateMapModal();
            _mapModal.SetActive(false);
        }

        private void ToggleMapModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = _mapModal != null && !_mapModal.activeSelf;
            CloseAllModals();
            if (next)
            {
                PopulateMapModal();
                _mapModal.SetActive(true);
                SyncPlayerMovementLock();
            }
        }

        private void PopulateMapModal()
        {
            if (_mapListContent == null) return;
            var inv = PlayerInventory.Instance;
            var court = BarangayCourtBuilder.Instance;

            bool freePlay = inv != null && inv.FreePlayUnlockAll;
            if (_mapFreePlayToggleText != null)
            {
                _mapFreePlayToggleText.text = freePlay ? "FREE PLAY UNLOCK ALL: ON ✓" : "FREE PLAY UNLOCK ALL: OFF";
            }
            if (_mapFreePlayToggleBtn != null)
            {
                _mapFreePlayToggleBtn.GetComponent<Image>().color = freePlay
                    ? new Color(0.18f, 0.72f, 0.36f)
                    : new Color(0.24f, 0.34f, 0.50f);
            }

            int totalFound = inv != null ? inv.CountDiscoveredHiddenCards() : 0;
            int totalHidden = court != null ? court.GetTotalHiddenCardCount() : 12;
            if (_mapHeaderSummaryText != null)
            {
                _mapHeaderSummaryText.text = $"Hidden Collectible Cards Found: {totalFound} / {totalHidden}   |   Unlocked Areas: {(inv != null ? GetUnlockedAreaCount(inv) : 5)} / 5";
            }

            for (int i = _mapListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_mapListContent.GetChild(i).gameObject);
            }

            foreach (var zone in BarangayCourtBuilder.WorldZones)
            {
                var captured = zone;
                bool unlocked = inv == null || inv.IsAreaUnlocked(zone.AreaId);
                bool storyUnlocked = inv == null || inv.IsAreaStoryUnlocked(zone.AreaId);

                IReadOnlyList<string> areaHiddenIds = court != null ? court.GetHiddenCardIdsInArea(zone.AreaId) : null;
                int foundInArea = (inv != null && areaHiddenIds != null) ? inv.CountDiscoveredHiddenCards(areaHiddenIds) : 0;
                int totalInArea = areaHiddenIds != null ? areaHiddenIds.Count : 0;

                Color bgCol = unlocked ? new Color(0.13f, 0.19f, 0.30f, 1f) : new Color(0.10f, 0.12f, 0.18f, 0.94f);
                Color borderCol = unlocked
                    ? (storyUnlocked ? new Color(0.32f, 0.76f, 0.48f) : new Color(0.32f, 0.68f, 0.94f))
                    : new Color(0.68f, 0.34f, 0.28f);

                var row = CreateRoundedPanel("ZoneRow_" + zone.AreaId, _mapListContent, bgCol, new Vector2(820f, 92f));
                AddOutline(row.gameObject, borderCol, 2f);

                string statusBadge = storyUnlocked
                    ? "[UNLOCKED]"
                    : (unlocked ? "[UNLOCKED via FREE PLAY]" : "[LOCKED]");
                string reqLine = storyUnlocked
                    ? "Story Progression: Unlocked ✓"
                    : (inv != null ? inv.GetAreaUnlockRequirementText(zone.AreaId) : zone.UnlockHint);

                var txt = CreateText(
                    "ZoneTxt",
                    row,
                    $"{zone.Name}   {statusBadge}   •   Hidden Cards: {foundInArea} / {totalInArea}\n{zone.Subtitle}\n{reqLine}",
                    14,
                    FontStyle.Bold,
                    unlocked ? Color.white : new Color(0.78f, 0.80f, 0.86f),
                    TextAnchor.MiddleLeft
                );
                txt.rectTransform.anchorMin = new Vector2(0f, 0f);
                txt.rectTransform.anchorMax = new Vector2(0.76f, 1f);
                txt.rectTransform.offsetMin = new Vector2(18f, 6f);
                txt.rectTransform.offsetMax = new Vector2(-8f, -6f);

                var goBtn = CreateButton(
                    "GoZoneBtn",
                    row,
                    unlocked ? "TRAVEL HERE" : "LOCKED",
                    unlocked ? new Color(0.18f, 0.72f, 0.36f) : new Color(0.28f, 0.28f, 0.34f),
                    14,
                    () =>
                    {
                        if (!unlocked)
                        {
                            ShowToast(reqLine);
                            return;
                        }
                        StartGameFromMenuIfNeeded();
                        CloseAllModals();
                        if (PlayerController.Instance != null)
                        {
                            PlayerController.Instance.TeleportForMatch(captured.SpawnPosition, Quaternion.Euler(0f, captured.SpawnYaw, 0f));
                            ShowToast($"Traveled to {captured.Name}!");
                        }
                    }
                );
                var gRt = goBtn.GetComponent<RectTransform>();
                gRt.anchorMin = new Vector2(1f, 0.5f);
                gRt.anchorMax = new Vector2(1f, 0.5f);
                gRt.anchoredPosition = new Vector2(-96f, 0f);
                gRt.sizeDelta = new Vector2(160f, 44f);
                goBtn.interactable = unlocked;
            }
        }

        private static int GetUnlockedAreaCount(PlayerInventory inv)
        {
            if (inv == null) return 5;
            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                if (inv.IsAreaUnlocked((BarangayAreaId)i)) count++;
            }
            return count;
        }

        // =====================================================================
        // 6. PAUSE MENU ([Esc]) & SETTINGS MODAL (Master/BGM/SFX/Mute/Fullscreen)
        // =====================================================================

        private void CreatePauseModal()
        {
            _pauseModal = CreateModalBackdrop("PauseMenuModal", new Vector2(540f, 520f), out RectTransform window);

            var title = CreateText("PauseTitle", window, "GAME PAUSED", 30, FontStyle.Bold, new Color(1.0f, 0.85f, 0.26f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(20f, -62f);
            title.rectTransform.offsetMax = new Vector2(-20f, -14f);

            _pauseStatusText = CreateText("PauseStatus", window, "Auto-save active (teks_save.json)", 14, FontStyle.Italic, new Color(0.74f, 0.88f, 1.0f), TextAnchor.MiddleCenter);
            _pauseStatusText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _pauseStatusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _pauseStatusText.rectTransform.offsetMin = new Vector2(20f, -92f);
            _pauseStatusText.rectTransform.offsetMax = new Vector2(-20f, -64f);

            var resumeBtn = CreateButton("PauseResumeBtn", window, "RESUME GAME [ESC]", new Color(0.18f, 0.74f, 0.34f), 18, CloseAllModals);
            var rRt = resumeBtn.GetComponent<RectTransform>();
            rRt.anchoredPosition = new Vector2(0f, 110f);
            rRt.sizeDelta = new Vector2(360f, 50f);

            var settingsBtn = CreateButton("PauseSettingsBtn", window, "SETTINGS (AUDIO & DISPLAY)", new Color(0.20f, 0.48f, 0.82f), 17, () =>
            {
                _openedSettingsFromPause = true;
                _pauseModal.SetActive(false);
                RefreshSettingsModal();
                _settingsModal.SetActive(true);
                SyncPlayerMovementLock();
            });
            var sRt = settingsBtn.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 44f);
            sRt.sizeDelta = new Vector2(360f, 50f);

            var saveBtn = CreateButton("PauseSaveBtn", window, "SAVE GAME TO DISK", new Color(0.84f, 0.54f, 0.12f), 17, () =>
            {
                bool ok = PlayerInventory.Instance != null && PlayerInventory.Instance.SaveToDisk();
                TeksAudioManager.Instance?.PlayCoinClink();
                string msg = ok
                    ? "Game progress saved to teks_save.json!"
                    : "Save complete (synced with local profile).";
                if (_pauseStatusText != null) _pauseStatusText.text = msg;
                ShowToast(msg);
            });
            var svRt = saveBtn.GetComponent<RectTransform>();
            svRt.anchoredPosition = new Vector2(0f, -22f);
            svRt.sizeDelta = new Vector2(360f, 50f);

            var mainMenuBtn = CreateButton("PauseMainMenuBtn", window, "RETURN TO MAIN MENU", new Color(0.36f, 0.42f, 0.56f), 17, () =>
            {
                PlayerInventory.Instance?.SaveToDisk();
                TeksGameModeManager.Instance?.ExitActiveMode(true);
                ShowMainMenu();
            });
            var mRt = mainMenuBtn.GetComponent<RectTransform>();
            mRt.anchoredPosition = new Vector2(0f, -88f);
            mRt.sizeDelta = new Vector2(360f, 50f);

            var exitBtn = CreateButton("PauseExitBtn", window, "EXIT GAME", new Color(0.74f, 0.20f, 0.22f), 17, OnExitGameClicked);
            var eRt = exitBtn.GetComponent<RectTransform>();
            eRt.anchoredPosition = new Vector2(0f, -154f);
            eRt.sizeDelta = new Vector2(360f, 50f);

            _pauseModal.SetActive(false);
        }

        public void TogglePauseModal()
        {
            if (_startScreenRoot != null && _startScreenRoot.activeSelf) return;
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;

            bool next = _pauseModal != null && !_pauseModal.activeSelf;
            CloseAllModals();
            if (next)
            {
                if (_pauseStatusText != null && PlayerInventory.Instance != null)
                {
                    _pauseStatusText.text = $"Profile: {PlayerInventory.Instance.PlayerNickname}  •  {PlayerInventory.Instance.Coins:N0} Coins  |  {PlayerInventory.Instance.Gems:N0} Gems";
                }
                _pauseModal.SetActive(true);
                SyncPlayerMovementLock();
            }
        }

        private void CreateSettingsModal()
        {
            _settingsModal = CreateModalBackdrop("SettingsModal", new Vector2(680f, 540f), out RectTransform window);

            var header = CreateText("SettingsHeader", window, "AUDIO & DISPLAY SETTINGS", 26, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.85f, 1f);
            header.rectTransform.offsetMin = new Vector2(28f, -56f);
            header.rectTransform.offsetMax = new Vector2(0f, -12f);

            var closeBtn = CreateButton("CloseSettingsBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseSettingsModal);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            BuildVolumeControlRow(
                window,
                "MASTER VOLUME",
                122f,
                out _settingsMasterValText,
                out _settingsMasterFill,
                delta => AdjustAudioVolume(0, delta)
            );

            BuildVolumeControlRow(
                window,
                "BGM MUSIC VOLUME",
                46f,
                out _settingsBgmValText,
                out _settingsBgmFill,
                delta => AdjustAudioVolume(1, delta)
            );

            BuildVolumeControlRow(
                window,
                "SFX SOUND VOLUME",
                -30f,
                out _settingsSfxValText,
                out _settingsSfxFill,
                delta => AdjustAudioVolume(2, delta)
            );

            var muteBtn = CreateButton("MuteToggleBtn", window, "AUDIO: ON (UNMUTED)", new Color(0.18f, 0.64f, 0.36f), 16, () =>
            {
                var settings = GetWorkingAudioSettings();
                settings.IsMuted = !settings.IsMuted;
                ApplyAndSaveAudioSettings(settings);
            });
            var mRt = muteBtn.GetComponent<RectTransform>();
            mRt.anchoredPosition = new Vector2(-150f, -112f);
            mRt.sizeDelta = new Vector2(276f, 46f);
            _settingsMuteBtnImg = muteBtn.GetComponent<Image>();
            _settingsMuteBtnText = muteBtn.GetComponentInChildren<Text>();

            var fsBtn = CreateButton("FullscreenToggleBtn", window, "DISPLAY: FULLSCREEN", new Color(0.20f, 0.50f, 0.84f), 16, () =>
            {
                var settings = GetWorkingAudioSettings();
                settings.IsFullscreen = !settings.IsFullscreen;
                Screen.fullScreenMode = settings.IsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.fullScreen = settings.IsFullscreen;
                ApplyAndSaveAudioSettings(settings);
            });
            var fRt = fsBtn.GetComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(150f, -112f);
            fRt.sizeDelta = new Vector2(276f, 46f);
            _settingsFullscreenBtnImg = fsBtn.GetComponent<Image>();
            _settingsFullscreenBtnText = fsBtn.GetComponentInChildren<Text>();

            var doneBtn = CreateButton("DoneSettingsBtn", window, "SAVE & DONE", new Color(0.18f, 0.74f, 0.34f), 18, CloseSettingsModal);
            var dRt = doneBtn.GetComponent<RectTransform>();
            dRt.anchoredPosition = new Vector2(0f, -196f);
            dRt.sizeDelta = new Vector2(280f, 50f);

            _settingsModal.SetActive(false);
        }

        private void BuildVolumeControlRow(
            RectTransform parent,
            string label,
            float yOffset,
            out Text valueText,
            out Image fillBar,
            Action<float> onDelta)
        {
            var row = CreateRoundedPanel("VolRow_" + label, parent, new Color(0.12f, 0.17f, 0.27f, 1f), new Vector2(616f, 62f));
            row.anchoredPosition = new Vector2(0f, yOffset);

            var lbl = CreateText("Label", row, label, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            lbl.rectTransform.anchorMin = new Vector2(0f, 0f);
            lbl.rectTransform.anchorMax = new Vector2(0.36f, 1f);
            lbl.rectTransform.offsetMin = new Vector2(18f, 4f);
            lbl.rectTransform.offsetMax = new Vector2(0f, -4f);

            var minusBtn = CreateButton("MinusBtn", row, "- 10%", new Color(0.26f, 0.34f, 0.48f), 14, () => onDelta?.Invoke(-0.10f));
            var minRt = minusBtn.GetComponent<RectTransform>();
            minRt.anchoredPosition = new Vector2(-58f, 0f);
            minRt.sizeDelta = new Vector2(72f, 38f);

            var barBg = CreateRoundedPanel("BarBg", row, new Color(0.05f, 0.08f, 0.14f, 1f), new Vector2(184f, 22f));
            barBg.anchoredPosition = new Vector2(82f, 0f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barBg, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0.85f, 1f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            fillBar = fillGo.GetComponent<Image>();
            fillBar.color = new Color(0.24f, 0.82f, 0.46f);

            var plusBtn = CreateButton("PlusBtn", row, "+ 10%", new Color(0.22f, 0.56f, 0.86f), 14, () => onDelta?.Invoke(0.10f));
            var plRt = plusBtn.GetComponent<RectTransform>();
            plRt.anchoredPosition = new Vector2(222f, 0f);
            plRt.sizeDelta = new Vector2(72f, 38f);

            valueText = CreateText("ValTxt", row, "85%", 15, FontStyle.Bold, new Color(1.0f, 0.88f, 0.34f), TextAnchor.MiddleRight);
            valueText.rectTransform.anchorMin = new Vector2(0.84f, 0f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueText.rectTransform.offsetMin = Vector2.zero;
            valueText.rectTransform.offsetMax = new Vector2(-14f, 0f);
        }

        private AudioSettingsData GetWorkingAudioSettings()
        {
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.AudioSettings != null)
            {
                return PlayerInventory.Instance.AudioSettings.Clone();
            }
            if (TeksAudioManager.Instance != null)
            {
                return TeksAudioManager.Instance.GetCurrentAudioSettings();
            }
            return new AudioSettingsData();
        }

        private void AdjustAudioVolume(int channel, float delta)
        {
            var settings = GetWorkingAudioSettings();
            if (channel == 0) settings.MasterVolume = Mathf.Clamp01(Mathf.Round((settings.MasterVolume + delta) * 10f) / 10f);
            else if (channel == 1) settings.BgmVolume = Mathf.Clamp01(Mathf.Round((settings.BgmVolume + delta) * 10f) / 10f);
            else settings.SfxVolume = Mathf.Clamp01(Mathf.Round((settings.SfxVolume + delta) * 10f) / 10f);

            ApplyAndSaveAudioSettings(settings);
        }

        private void ApplyAndSaveAudioSettings(AudioSettingsData settings)
        {
            if (settings == null) return;
            settings.Clamp();
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.UpdateAudioSettings(settings);
            }
            else if (TeksAudioManager.Instance != null)
            {
                TeksAudioManager.Instance.ApplyAudioSettings(settings);
            }
            RefreshSettingsModal();
        }

        private void RefreshSettingsModal()
        {
            var s = GetWorkingAudioSettings();
            if (_settingsMasterValText != null) _settingsMasterValText.text = $"{Mathf.RoundToInt(s.MasterVolume * 100f)}%";
            if (_settingsMasterFill != null) _settingsMasterFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, s.MasterVolume), 1f);

            if (_settingsBgmValText != null) _settingsBgmValText.text = $"{Mathf.RoundToInt(s.BgmVolume * 100f)}%";
            if (_settingsBgmFill != null) _settingsBgmFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, s.BgmVolume), 1f);

            if (_settingsSfxValText != null) _settingsSfxValText.text = $"{Mathf.RoundToInt(s.SfxVolume * 100f)}%";
            if (_settingsSfxFill != null) _settingsSfxFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, s.SfxVolume), 1f);

            if (_settingsMuteBtnText != null)
            {
                _settingsMuteBtnText.text = s.IsMuted ? "AUDIO: MUTED (CLICK TO UNMUTE)" : "AUDIO: ACTIVE (CLICK TO MUTE)";
            }
            if (_settingsMuteBtnImg != null)
            {
                _settingsMuteBtnImg.color = s.IsMuted ? new Color(0.76f, 0.24f, 0.24f) : new Color(0.18f, 0.64f, 0.36f);
            }

            if (_settingsFullscreenBtnText != null)
            {
                _settingsFullscreenBtnText.text = s.IsFullscreen ? "DISPLAY: FULLSCREEN" : "DISPLAY: WINDOWED";
            }
            if (_settingsFullscreenBtnImg != null)
            {
                _settingsFullscreenBtnImg.color = s.IsFullscreen ? new Color(0.20f, 0.50f, 0.84f) : new Color(0.32f, 0.38f, 0.52f);
            }
        }

        private void CloseSettingsModal()
        {
            if (_settingsModal != null) _settingsModal.SetActive(false);
            if (_openedSettingsFromPause)
            {
                _openedSettingsFromPause = false;
                if (_pauseModal != null) _pauseModal.SetActive(true);
                SyncPlayerMovementLock();
                return;
            }
            CloseAllModals();
        }

        // =====================================================================
        // 7. HIDDEN CARD REVEAL CELEBRATION MODAL & WORLD EVENT HANDLERS
        // =====================================================================

        private void CreateHiddenCardRevealModal()
        {
            _hiddenCardRevealModal = CreateModalBackdrop("HiddenCardRevealModal", new Vector2(580f, 490f), out RectTransform window);

            var title = CreateText("RevealHeader", window, "SECRET TEKS CARD DISCOVERED!", 24, FontStyle.Bold, new Color(1.0f, 0.86f, 0.24f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(18f, -50f);
            title.rectTransform.offsetMax = new Vector2(-18f, -12f);

            _hiddenRevealSpotText = CreateText("RevealSpot", window, "Found at: Behind Player's House", 15, FontStyle.Italic, new Color(0.78f, 0.92f, 1.0f), TextAnchor.MiddleCenter);
            _hiddenRevealSpotText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _hiddenRevealSpotText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _hiddenRevealSpotText.rectTransform.offsetMin = new Vector2(18f, -78f);
            _hiddenRevealSpotText.rectTransform.offsetMax = new Vector2(-18f, -48f);

            var cardFrame = CreateRoundedPanel("RevealCardFrame", window, new Color(1.0f, 0.78f, 0.22f), new Vector2(162f, 224f));
            cardFrame.anchoredPosition = new Vector2(0f, 34f);
            _hiddenRevealCardFrame = cardFrame.GetComponent<Image>();

            var inner = CreateRoundedPanel("Inner", cardFrame, new Color(0.08f, 0.10f, 0.16f), Vector2.zero);
            StretchFull(inner, 5f);

            _hiddenRevealCardArt = CreateImage("Art", inner, null);
            StretchFull(_hiddenRevealCardArt.rectTransform, 4f);

            _hiddenRevealCardNameText = CreateText("RevealCardName", window, "SARIMANOK", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _hiddenRevealCardNameText.rectTransform.anchoredPosition = new Vector2(0f, -106f);
            _hiddenRevealCardNameText.rectTransform.sizeDelta = new Vector2(520f, 30f);

            _hiddenRevealMetaText = CreateText("RevealMeta", window, "Legendary  •  Filipino Mythology", 15, FontStyle.Bold, new Color(1.0f, 0.84f, 0.32f), TextAnchor.MiddleCenter);
            _hiddenRevealMetaText.rectTransform.anchoredPosition = new Vector2(0f, -132f);
            _hiddenRevealMetaText.rectTransform.sizeDelta = new Vector2(520f, 26f);

            _hiddenRevealBonusText = CreateText("RevealBonus", window, "+50 Barya Coins  |  +5 Gems  |  Hidden Cards: 1 / 12", 15, FontStyle.Bold, new Color(0.45f, 0.98f, 0.58f), TextAnchor.MiddleCenter);
            _hiddenRevealBonusText.rectTransform.anchoredPosition = new Vector2(0f, -160f);
            _hiddenRevealBonusText.rectTransform.sizeDelta = new Vector2(540f, 26f);

            var okBtn = CreateButton("CloseRevealBtn", window, "AWESOME! KEEP EXPLORING", new Color(0.18f, 0.74f, 0.34f), 17, () =>
            {
                _hiddenCardRevealModal.SetActive(false);
                SyncPlayerMovementLock();
            });
            var okRt = okBtn.GetComponent<RectTransform>();
            okRt.anchoredPosition = new Vector2(0f, -206f);
            okRt.sizeDelta = new Vector2(300f, 46f);

            _hiddenCardRevealModal.SetActive(false);
        }

        private void HandleHiddenCardCollected(HiddenCardCollectible collectible, TeksCardDefinition card, int coinBonus, int gemBonus)
        {
            var inv = PlayerInventory.Instance;
            int foundCount = inv != null ? inv.CountDiscoveredHiddenCards() : 1;
            int totalCount = BarangayCourtBuilder.Instance != null ? BarangayCourtBuilder.Instance.GetTotalHiddenCardCount() : 12;

            if (card != null && _hiddenCardRevealModal != null)
            {
                Color rCol = TeksDatabase.GetRarityColor(card.Rarity);
                if (_hiddenRevealCardFrame != null) _hiddenRevealCardFrame.color = rCol;
                if (_hiddenRevealCardArt != null) _hiddenRevealCardArt.sprite = CardArtGenerator.GetCardPortraitSprite(card, 1);
                if (_hiddenRevealSpotText != null)
                {
                    string spot = collectible != null ? collectible.SpotLabel : "Barangay Secret Spot";
                    string area = collectible != null ? BarangayCourtBuilder.GetAreaDisplayName(collectible.AreaId) : "Barangay";
                    _hiddenRevealSpotText.text = $"Found at: {spot} ({area})";
                }
                if (_hiddenRevealCardNameText != null) _hiddenRevealCardNameText.text = card.Name.ToUpper();
                if (_hiddenRevealMetaText != null)
                {
                    _hiddenRevealMetaText.text = $"{TeksDatabase.GetRarityName(card.Rarity)}  •  {TeksDatabase.GetCategoryName(card.Category)}";
                    _hiddenRevealMetaText.color = rCol;
                }
                if (_hiddenRevealBonusText != null)
                {
                    _hiddenRevealBonusText.text = $"+{coinBonus} Barya Coins   |   +{gemBonus} Gems   |   Hidden Cards: {foundCount} / {totalCount}";
                }

                _hiddenCardRevealModal.SetActive(true);
                SyncPlayerMovementLock();
            }

            string cardTitle = card != null ? card.Name : "Hidden Teks Card";
            ShowToast($"Found Hidden Card: {cardTitle}! (+{coinBonus} Coins, +{gemBonus} Gems) [{foundCount}/{totalCount}]");
            RefreshAllUI();
        }

        private void HandleLockedGateInteracted(AreaGateBarrier gate)
        {
            if (gate == null) return;
            TeksAudioManager.Instance?.PlayUiClick();
            ShowToast($"LOCKED ({gate.GateTitle}): {gate.UnlockHint}");
        }

        private void HandleAreaUnlocked(BarangayAreaId areaId)
        {
            TeksAudioManager.Instance?.PlayWinFanfare();
            BarangayCourtBuilder.Instance?.RefreshAreaGates();
            ShowToast($"NEW AREA UNLOCKED: {BarangayCourtBuilder.GetAreaDisplayName(areaId)}! Gate barriers opened!");
            RefreshAllUI();
        }

        private void HandleAreasChanged()
        {
            BarangayCourtBuilder.Instance?.RefreshAreaGates();
            if (_mapModal != null && _mapModal.activeSelf) PopulateMapModal();
            if (_modesModal != null && _modesModal.activeSelf) PopulateModesModal();
        }

        private void HandleAudioSettingsChanged(AudioSettingsData settings)
        {
            if (_settingsModal != null && _settingsModal.activeSelf) RefreshSettingsModal();
        }

        private void HandleGameModeStateChanged()
        {
            if (_modesModal != null && _modesModal.activeSelf) PopulateModesModal();
        }

        private void HandleGameModeCompleted(GameModeType mode, bool succeeded, string summary, int bonusCoins, int bonusGems)
        {
            if (!string.IsNullOrEmpty(summary))
            {
                ShowToast(summary);
            }
            RefreshAllUI();
        }

        // =====================================================================
        // 8. CENTRAL MODAL MANAGEMENT, ESCAPE ROUTING & MOVEMENT LOCK
        // =====================================================================

        private void HandleEscapePressed()
        {
            var mm = TeksMatchManager.Instance;
            if (mm != null && mm.State != MatchState.Idle)
            {
                if (mm.State == MatchState.WagerScreen)
                {
                    if (_wagerModal != null) _wagerModal.SetActive(false);
                    mm.CancelChallenge();
                    SyncPlayerMovementLock();
                }
                return;
            }

            if (_hiddenCardRevealModal != null && _hiddenCardRevealModal.activeSelf)
            {
                _hiddenCardRevealModal.SetActive(false);
                SyncPlayerMovementLock();
                return;
            }

            if (_settingsModal != null && _settingsModal.activeSelf && _openedSettingsFromPause)
            {
                _settingsModal.SetActive(false);
                _openedSettingsFromPause = false;
                if (_pauseModal != null) _pauseModal.SetActive(true);
                SyncPlayerMovementLock();
                return;
            }

            if (IsAnySubModalOpen())
            {
                CloseAllModals();
                return;
            }

            if (_startScreenRoot != null && _startScreenRoot.activeSelf)
            {
                return;
            }

            TogglePauseModal();
        }

        private bool IsAnySubModalOpen()
        {
            return (_inventoryModal != null && _inventoryModal.activeSelf) ||
                   (_shopModal != null && _shopModal.activeSelf) ||
                   (_questsModal != null && _questsModal.activeSelf) ||
                   (_mapModal != null && _mapModal.activeSelf) ||
                   (_modesModal != null && _modesModal.activeSelf) ||
                   (_pauseModal != null && _pauseModal.activeSelf) ||
                   (_settingsModal != null && _settingsModal.activeSelf) ||
                   (_hiddenCardRevealModal != null && _hiddenCardRevealModal.activeSelf);
        }

        private void CloseAllModals()
        {
            _openedSettingsFromPause = false;

            if (_inventoryModal != null) _inventoryModal.SetActive(false);
            if (_shopModal != null) _shopModal.SetActive(false);
            if (_questsModal != null) _questsModal.SetActive(false);
            if (_mapModal != null) _mapModal.SetActive(false);
            if (_modesModal != null) _modesModal.SetActive(false);
            if (_pauseModal != null) _pauseModal.SetActive(false);
            if (_settingsModal != null) _settingsModal.SetActive(false);
            if (_hiddenCardRevealModal != null) _hiddenCardRevealModal.SetActive(false);

            if (_openedSubModalFromStartScreen)
            {
                _openedSubModalFromStartScreen = false;
                if (_startScreenRoot != null) _startScreenRoot.SetActive(true);
                if (Camera.main != null)
                {
                    var camFollow = Camera.main.GetComponent<CameraFollowController>();
                    if (camFollow != null) camFollow.IsTitleCameraActive = true;
                }
            }

            SyncPlayerMovementLock();
        }

        private bool IsAnyModalOpen()
        {
            return (_startScreenRoot != null && _startScreenRoot.activeSelf) ||
                   (_wagerModal != null && _wagerModal.activeSelf) ||
                   IsAnySubModalOpen();
        }

        private void SyncPlayerMovementLock()
        {
            if (PlayerController.Instance == null) return;
            bool inMatch = TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle;
            PlayerController.Instance.MovementLocked = inMatch || IsAnyModalOpen();
        }
    }
}
