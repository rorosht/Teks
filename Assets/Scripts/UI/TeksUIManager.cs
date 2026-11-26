using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TeksMaster.Audio;
using TeksMaster.Core;
using TeksMaster.Characters;
using TeksMaster.Gameplay;
using TeksMaster.Shop;
using TeksMaster.Visuals;
using TeksMaster.World;

namespace TeksMaster.UI
{
    public partial class TeksUIManager : MonoBehaviour
    {
        public static TeksUIManager Instance { get; private set; }

        private Font _font;
        private Canvas _canvas;

        // Top Left HUD
        private Text _coinsText;
        private Text _gemsText;
        private Text _hudCharacterSubText;
        private Button _avatarProfileBtn;
        private Image _hudAvatarPortrait;
        private Text _hudProfileNameRank;
        private Text _hudCompactStatsText;
        private Text _hudStreakBadge;
        private GameObject _questsBadgePill;
        private Text _questsBadgeText;
        private Text _proximityPromptText;
        private GameObject _proximityPromptPanel;
        private Action _avatarProfileClickHandler;

        // Start Screen (Title / Nickname / Character Type)
        private GameObject _startScreenRoot;
        private InputField _nicknameInput;
        private Image _startPreviewPortrait;
        private Text _startCharacterDescText;
        private readonly Image[] _startCharacterBtnImgs = new Image[8];
        private readonly Outline[] _startCharacterBtnOutlines = new Outline[8];
        private PlayerCharacterType _pendingStartCharacterType = PlayerCharacterType.StreetKid;

        // Quests Modal
        private GameObject _questsModal;
        private Transform _questsListContent;

        // Top Center Zone Pill
        private Text _zonePillText;
        private string _lastDisplayedZone = string.Empty;

        // Top Right HUD
        private Text _shopTimerText;

        // Bottom Right "MY PAMATO" HUD Deck
        private GameObject _pamatoPanel;
        private GameObject _pamatoMinimizedBtn;
        private Text _pamatoHeaderText;
        private Image _frontCardFrame;
        private Image _frontCardPortrait;
        private Text _frontCardStars;
        private Text _frontCardName;
        private Text _frontCardRarity;
        private Text _frontCardBottomStats;
        private Image _midCardFrame;
        private Image _midCardPortrait;
        private Text _midCardName;
        private Text _midCardRarity;
        private Image _backCardFrame;
        private Image _backCardPortrait;
        private readonly Image[] _statFillBars = new Image[4];
        private readonly Text[] _statValueTexts = new Text[4];
        private Text _pamatoUpgradeSubText;

        // Bottom Center Match HUD
        private GameObject _matchHudRoot;
        private GameObject _matchBannerRoot;
        private CurvedFlickGauge _flickGauge;
        private Text _matchBannerTitle;
        private Text _matchBannerSub;
        private GameObject _roundResultPanel;
        private Text _roundResultTitle;
        private Text _roundResultSubtitle;
        private Text _roundResultCardsState;
        private GameObject _roundButtonsRow;
        private Button _roundPrimaryBtn;
        private Text _roundPrimaryBtnText;
        private Button _roundSecondaryBtn;
        private Text _roundSecondaryBtnText;
        private GameObject _cardFlipShowcaseRoot;
        private readonly FlipCardWidget[] _flipWidgets = new FlipCardWidget[2];

        private class FlipCardWidget
        {
            public RectTransform SlotRoot;
            public RectTransform CardTransform;
            public Image FrameImage;
            public Image FaceImage;
            public GameObject FrontOverlay;
            public Text CardNameText;
            public Text StarsText;
            public Text RoleText;
            public Image StatusPillBg;
            public Text StatusPillText;
            public GameObject WinnerTag;

            public Sprite FrontSprite;
            public Sprite BackSprite;
            public Color RarityColor;
            public float FlipAngle;
            public float SpinSpeed;
            public float BaseCardY;
            public float SettledPopTimer;
            public bool WasSettled;
        }

        // Modals
        private GameObject _inventoryModal;
        private GameObject _inventoryScrollRoot;
        private Transform _inventoryGridContent;
        private GameObject _fusionLabRoot;
        private Transform _fusionListContent;
        private Image _tabCardsBtnImg;
        private Image _tabFusionBtnImg;
        private bool _isFusionTabOpen;
        private Text _inventoryHeaderCoins;

        private GameObject _shopModal;
        private Text _shopVendorBannerText;
        private Text _shopModalTimerText;
        private Text _shopModalCoinsText;
        private Button _shopDailyFreeBtn;
        private Text _shopDailyFreeBtnText;
        private Button _shopDailyPackBtn;
        private Text _shopDailyPackBtnText;
        private Image _shopDailyPackPillImg;
        private Transform _shopSlotsContainer;
        private Transform _packRevealContainer;
        private Text _packRevealHeader;

        private GameObject _wagerModal;
        private Text _wagerChallengeQuoteText;
        private Text _wagerPlayerName;
        private Text _wagerPlayerOddsPillText;
        private Image _wagerPlayerAvatarImg;
        private Text _wagerPlayerBadgeText;
        private Image _wagerPlayerCardFrame;
        private Image _wagerPlayerCardImg;
        private Text _wagerPlayerCardLevelBadge;
        private Text _wagerPlayerCardStarsText;
        private Text _wagerPlayerCardText;
        private Text _wagerOppName;
        private Text _wagerOppOddsPillText;
        private Image _wagerOppAvatarImg;
        private Text _wagerOppBadgeText;
        private Image _wagerOppCardFrame;
        private Image _wagerOppCardImg;
        private Text _wagerOppCardLevelBadge;
        private Text _wagerOppCardStarsText;
        private Text _wagerOppCardText;
        private Text _wagerPwrCompareText;
        private Text _wagerHpCompareText;
        private Text _wagerAtkCompareText;
        private Text _wagerAmountText;
        private Text _wagerBonusInfoText;
        private readonly Image[] _wagerChipImgs = new Image[5];
        private readonly Outline[] _wagerChipOutlines = new Outline[5];
        private readonly Text[] _wagerChipTexts = new Text[5];
        private static readonly int[] WagerChipValues = { 0, 50, 100, 200, 500 };
        private int _selectedWagerAmount = 100;

        // Card Picker Modal (opened from Wager Modal)
        private GameObject _cardPickerModal;
        private Text _cardPickerSubtitleText;
        private Transform _cardPickerGridContent;

        // Sell Confirmation Modal
        private GameObject _sellConfirmModal;
        private Text _sellConfirmTitleText;
        private Text _sellConfirmCardInfoText;
        private Text _sellConfirmWarningText;
        private string _pendingSellCardId;

        // Toast
        private GameObject _toastPanel;
        private Text _toastText;
        private float _toastTimer;

        // NPC Floating Tags
        private Transform _npcTagsLayer;
        private readonly List<NpcTagEntry> _npcTags = new List<NpcTagEntry>();

        private class NpcTagEntry
        {
            public NpcOpponent Npc;
            public Transform TargetTransform;
            public string StaticLabel;
            public float VerticalOffset = 1.78f;
            public RectTransform Root;
            public Text Label;
            public Image Arrow;
            public GameObject SpeechBubble;
            public Text SpeechLabel;
        }

        private bool _built;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void BuildUIIfNeeded()
        {
            if (_built && _canvas != null) return;
            _built = true;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            EnsureEventSystem();
            CreateMainCanvas();
            CreateNpcTagsLayer();
            CreateTopLeftHud();
            CreateTopRightHud();
            CreateTopCenterActionDock();
            CreateCurrentMissionTrackerHud();
            CreateProximityPrompt();
            CreateBottomRightPamatoPanel();
            CreateMatchHud();
            CreateInventoryModal();
            CreateShopModal();
            CreateQuestsModal();
            CreateWagerModal();
            CreateCardPickerModal();
            BuildExtendedUI();
            CreateSellConfirmModal();
            CreateStartScreen();
            CreateToastBanner();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ExternalInteractHandlerActive = true;
            }

            HookEvents();
            RefreshAllUI();
            SyncPlayerMovementLock();
        }

        private void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                var oldModule = existing.GetComponent<StandaloneInputModule>();
                if (oldModule != null) Destroy(oldModule);
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private void CreateMainCanvas()
        {
            var canvasGo = new GameObject("TeksMasterCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private void CreateNpcTagsLayer()
        {
            var layerGo = new GameObject("NpcHoverTagsLayer", typeof(RectTransform));
            layerGo.transform.SetParent(_canvas.transform, false);
            var rt = layerGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _npcTagsLayer = layerGo.transform;

            RebuildNpcTags();
        }

        public void RebuildNpcTags()
        {
            if (_npcTagsLayer == null) return;
            for (int i = _npcTagsLayer.childCount - 1; i >= 0; i--)
            {
                Destroy(_npcTagsLayer.GetChild(i).gameObject);
            }
            _npcTags.Clear();

            var court = BarangayCourtBuilder.Instance;
            if (court == null) return;

            foreach (var npc in court.SpawnedOpponents)
            {
                if (npc == null) continue;
                var pill = CreateRoundedPanel("Tag_" + npc.NpcName, _npcTagsLayer, new Color(0.08f, 0.11f, 0.17f, 0.85f), new Vector2(216f, 34f));
                var btn = pill.gameObject.AddComponent<Button>();
                var capturedNpc = npc;
                btn.onClick.AddListener(() =>
                {
                    if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State == MatchState.Idle)
                    {
                        CloseAllModals();
                        TeksMatchManager.Instance.StartChallenge(capturedNpc);
                    }
                });

                var lbl = CreateText("Label", pill, npc.GetDisplayTag(), 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                StretchFull(lbl.rectTransform, 6f);

                var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                arrowGo.transform.SetParent(pill, false);
                var arrowRt = arrowGo.GetComponent<RectTransform>();
                arrowRt.anchorMin = new Vector2(0.5f, 0f);
                arrowRt.anchorMax = new Vector2(0.5f, 0f);
                arrowRt.anchoredPosition = new Vector2(0f, -8f);
                arrowRt.sizeDelta = new Vector2(16f, 11f);
                var arrowImg = arrowGo.GetComponent<Image>();
                arrowImg.sprite = CardArtGenerator.GetTriangleSprite(true);
                arrowImg.color = npc.GetDifficultyColor();
                arrowImg.raycastTarget = false;

                // Comic speech bubble above NPC tag when player is nearby
                var bubbleRt = CreateRoundedPanel("SpeechBubble", pill, new Color(0.98f, 0.98f, 0.95f, 0.96f), new Vector2(250f, 34f));
                bubbleRt.anchorMin = new Vector2(0.5f, 1f);
                bubbleRt.anchorMax = new Vector2(0.5f, 1f);
                bubbleRt.anchoredPosition = new Vector2(0f, 24f);
                AddOutline(bubbleRt.gameObject, new Color(0.12f, 0.16f, 0.24f), 1.5f);
                var speechTxt = CreateText("SpeechTxt", bubbleRt, npc.GetActiveCallout(), 13, FontStyle.Bold, new Color(0.08f, 0.10f, 0.16f), TextAnchor.MiddleCenter);
                StretchFull(speechTxt.rectTransform, 5f);
                bubbleRt.gameObject.SetActive(false);

                _npcTags.Add(new NpcTagEntry
                {
                    Npc = npc,
                    Root = pill,
                    Label = lbl,
                    Arrow = arrowImg,
                    SpeechBubble = bubbleRt.gameObject,
                    SpeechLabel = speechTxt
                });
            }

            if (court.TournamentHostTransform != null)
            {
                var hostPill = CreateRoundedPanel("Tag_TournamentHost", _npcTagsLayer, new Color(0.14f, 0.10f, 0.04f, 0.92f), new Vector2(278f, 36f));
                AddOutline(hostPill.gameObject, new Color(1.0f, 0.80f, 0.22f), 2f);
                var hostBtn = hostPill.gameObject.AddComponent<Button>();
                hostBtn.onClick.AddListener(() =>
                {
                    if (TeksMatchManager.Instance == null || TeksMatchManager.Instance.State == MatchState.Idle)
                    {
                        CloseAllModals();
                        OpenTournamentModal();
                    }
                });

                var hostLbl = CreateText("Label", hostPill, BarangayCourtBuilder.TournamentHostDisplayName, 15, FontStyle.Bold, new Color(1.0f, 0.88f, 0.34f), TextAnchor.MiddleCenter);
                StretchFull(hostLbl.rectTransform, 6f);

                var hostArrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                hostArrowGo.transform.SetParent(hostPill, false);
                var hostArrowRt = hostArrowGo.GetComponent<RectTransform>();
                hostArrowRt.anchorMin = new Vector2(0.5f, 0f);
                hostArrowRt.anchorMax = new Vector2(0.5f, 0f);
                hostArrowRt.anchoredPosition = new Vector2(0f, -8f);
                hostArrowRt.sizeDelta = new Vector2(16f, 11f);
                var hostArrowImg = hostArrowGo.GetComponent<Image>();
                hostArrowImg.sprite = CardArtGenerator.GetTriangleSprite(true);
                hostArrowImg.color = new Color(1.0f, 0.80f, 0.22f);
                hostArrowImg.raycastTarget = false;

                _npcTags.Add(new NpcTagEntry
                {
                    Npc = null,
                    TargetTransform = court.TournamentHostTransform,
                    StaticLabel = BarangayCourtBuilder.TournamentHostDisplayName,
                    VerticalOffset = 1.78f,
                    Root = hostPill,
                    Label = hostLbl,
                    Arrow = hostArrowImg
                });
            }
        }

        private void CreateTopLeftHud()
        {
            // Player Profile Card (Top-Left)
            var panel = CreateRoundedPanel("TopLeftHUD", _canvas.transform, new Color(0.10f, 0.15f, 0.23f, 0.94f), new Vector2(456f, 126f));
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(22f, -18f);
            AddOutline(panel.gameObject, new Color(0.34f, 0.48f, 0.68f), 2f);

            var profileCardBtn = panel.gameObject.AddComponent<Button>();
            ApplyButtonTint(profileCardBtn, new Color(0.10f, 0.15f, 0.23f, 0.94f));
            profileCardBtn.onClick.AddListener(OnAvatarPortraitClicked);

            // Clickable Avatar Portrait Badge (opens Character Profile when wired in Extended UI)
            var avatarBox = CreateRoundedPanel("AvatarPortraitBox", panel, new Color(0.18f, 0.25f, 0.36f, 1f), new Vector2(90f, 94f));
            avatarBox.anchorMin = new Vector2(0f, 0.5f);
            avatarBox.anchorMax = new Vector2(0f, 0.5f);
            avatarBox.anchoredPosition = new Vector2(56f, 0f);
            AddOutline(avatarBox.gameObject, new Color(1.0f, 0.84f, 0.28f), 1.6f);
            _avatarProfileBtn = avatarBox.gameObject.AddComponent<Button>();
            _avatarProfileBtn.onClick.AddListener(OnAvatarPortraitClicked);

            _hudAvatarPortrait = CreateImage("AvatarPortrait", avatarBox, CharacterResourceGenerator.GetCharacterPortraitSprite(PlayerCharacterType.StreetKid));
            StretchFull(_hudAvatarPortrait.rectTransform, 3f);

            var profilePill = CreateRoundedPanel("AvatarProfileTag", avatarBox, new Color(0.06f, 0.10f, 0.18f, 0.88f), new Vector2(82f, 18f));
            profilePill.anchorMin = new Vector2(0.5f, 0f);
            profilePill.anchorMax = new Vector2(0.5f, 0f);
            profilePill.anchoredPosition = new Vector2(0f, 10f);
            profilePill.GetComponent<Image>().raycastTarget = false;
            var profileTagTxt = CreateText("ProfileTagTxt", profilePill, "PROFILE", 10, FontStyle.Bold, new Color(1.0f, 0.88f, 0.38f), TextAnchor.MiddleCenter);
            StretchFull(profileTagTxt.rectTransform, 1f);

            _hudProfileNameRank = CreateText("ProfileNameRank", panel, "TEKSMASTER  •  Baguhan (Rookie)", 15, FontStyle.Bold, new Color(0.80f, 0.92f, 1.0f), TextAnchor.MiddleLeft);
            _hudProfileNameRank.rectTransform.anchorMin = new Vector2(0f, 1f);
            _hudProfileNameRank.rectTransform.anchorMax = new Vector2(1f, 1f);
            _hudProfileNameRank.rectTransform.offsetMin = new Vector2(112f, -30f);
            _hudProfileNameRank.rectTransform.offsetMax = new Vector2(-12f, -6f);

            _hudCharacterSubText = CreateText("ProfileCharSub", panel, "STREET KID  •  PROFILE [U]", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            _hudCharacterSubText.resizeTextForBestFit = true;
            _hudCharacterSubText.resizeTextMinSize = 10;
            _hudCharacterSubText.resizeTextMaxSize = 14;
            _hudCharacterSubText.verticalOverflow = VerticalWrapMode.Truncate;
            _hudCharacterSubText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _hudCharacterSubText.rectTransform.anchorMax = new Vector2(0.72f, 1f);
            _hudCharacterSubText.rectTransform.offsetMin = new Vector2(112f, -55f);
            _hudCharacterSubText.rectTransform.offsetMax = new Vector2(-4f, -31f);

            _hudStreakBadge = CreateText("StreakBadge", panel, "", 13, FontStyle.Bold, new Color(1.0f, 0.52f, 0.16f), TextAnchor.MiddleRight);
            _hudStreakBadge.rectTransform.anchorMin = new Vector2(0.72f, 1f);
            _hudStreakBadge.rectTransform.anchorMax = new Vector2(1f, 1f);
            _hudStreakBadge.rectTransform.offsetMin = new Vector2(4f, -55f);
            _hudStreakBadge.rectTransform.offsetMax = new Vector2(-12f, -31f);

            _hudCompactStatsText = CreateText("HudCompactStats", panel, "W/L: 0-0 (0%)  •  Pamato: Hero 1★", 12, FontStyle.Bold, new Color(0.84f, 0.92f, 0.98f), TextAnchor.MiddleLeft);
            _hudCompactStatsText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _hudCompactStatsText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _hudCompactStatsText.rectTransform.offsetMin = new Vector2(112f, -79f);
            _hudCompactStatsText.rectTransform.offsetMax = new Vector2(-12f, -55f);

            var coinIcon = CreateImage("CoinIcon", panel, CardArtGenerator.GetUIIconSprite("coin"));
            coinIcon.rectTransform.anchorMin = new Vector2(0f, 0f);
            coinIcon.rectTransform.anchorMax = new Vector2(0f, 0f);
            coinIcon.rectTransform.anchoredPosition = new Vector2(124f, 24f);
            coinIcon.rectTransform.sizeDelta = new Vector2(22f, 22f);

            _coinsText = CreateText("CoinsValue", panel, "2,450 COINS", 17, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            _coinsText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _coinsText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _coinsText.rectTransform.offsetMin = new Vector2(142f, 8f);
            _coinsText.rectTransform.offsetMax = new Vector2(-156f, 40f);

            var gemIcon = CreateImage("GemIcon", panel, CardArtGenerator.GetUIIconSprite("gem"));
            gemIcon.rectTransform.anchorMin = new Vector2(0f, 0f);
            gemIcon.rectTransform.anchorMax = new Vector2(0f, 0f);
            gemIcon.rectTransform.anchoredPosition = new Vector2(304f, 24f);
            gemIcon.rectTransform.sizeDelta = new Vector2(21f, 21f);

            _gemsText = CreateText("GemsValue", panel, "25 GEMS", 17, FontStyle.Bold, new Color(0.38f, 0.92f, 1.0f), TextAnchor.MiddleLeft);
            _gemsText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _gemsText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _gemsText.rectTransform.offsetMin = new Vector2(322f, 8f);
            _gemsText.rectTransform.offsetMax = new Vector2(-10f, 40f);

            // Dedicated Settings Icon Button directly below Profile card (replaced old quick-button strip)
            var settingsPanel = CreateRoundedPanel("SettingsIconBtn", _canvas.transform, new Color(0.10f, 0.15f, 0.24f, 0.95f), new Vector2(52f, 52f));
            settingsPanel.anchorMin = new Vector2(0f, 1f);
            settingsPanel.anchorMax = new Vector2(0f, 1f);
            settingsPanel.pivot = new Vector2(0f, 1f);
            settingsPanel.anchoredPosition = new Vector2(22f, -152f);
            AddOutline(settingsPanel.gameObject, new Color(0.36f, 0.56f, 0.80f), 1.8f);

            var topGloss = CreateRoundedPanel("Gloss", settingsPanel, new Color(1f, 1f, 1f, 0.10f), Vector2.zero);
            topGloss.anchorMin = new Vector2(0.08f, 1f);
            topGloss.anchorMax = new Vector2(0.92f, 1f);
            topGloss.offsetMin = new Vector2(0f, -4f);
            topGloss.offsetMax = new Vector2(0f, -1f);
            topGloss.GetComponent<Image>().raycastTarget = false;

            var settingsBtn = settingsPanel.gameObject.AddComponent<Button>();
            ApplyButtonTint(settingsBtn, new Color(0.10f, 0.15f, 0.24f, 0.95f));
            settingsBtn.onClick.AddListener(() =>
            {
                TeksAudioManager.Instance?.PlayUiClick();
                ToggleSettingsModal();
            });

            var settingsIcon = CreateImage("SettingsGearIcon", settingsPanel, CardArtGenerator.GetUIIconSprite("settings"));
            settingsIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            settingsIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            settingsIcon.rectTransform.anchoredPosition = Vector2.zero;
            settingsIcon.rectTransform.sizeDelta = new Vector2(30f, 30f);
        }

        private void CreateTopRightHud()
        {
            var container = new GameObject("TopRightHUD", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(_canvas.transform, false);
            container.anchorMin = new Vector2(1f, 1f);
            container.anchorMax = new Vector2(1f, 1f);
            container.pivot = new Vector2(1f, 1f);
            container.anchoredPosition = new Vector2(-22f, -18f);
            container.sizeDelta = new Vector2(330f, 184f);

            var shopPanel = CreateRoundedPanel("ShopBtnPanel", container, new Color(0.10f, 0.15f, 0.23f, 0.95f), new Vector2(310f, 84f));
            shopPanel.anchorMin = new Vector2(1f, 1f);
            shopPanel.anchorMax = new Vector2(1f, 1f);
            shopPanel.pivot = new Vector2(1f, 1f);
            shopPanel.anchoredPosition = Vector2.zero;
            AddOutline(shopPanel.gameObject, new Color(0.36f, 0.50f, 0.70f), 2f);

            var shopBtn = shopPanel.gameObject.AddComponent<Button>();
            ApplyButtonTint(shopBtn, new Color(0.10f, 0.15f, 0.23f, 0.95f));
            shopBtn.onClick.AddListener(ToggleShopModal);

            var shopLabel = CreateText("ShopTitle", shopPanel, "SHOP [P]", 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            shopLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            shopLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            shopLabel.rectTransform.offsetMin = new Vector2(14f, 6f);
            shopLabel.rectTransform.offsetMax = new Vector2(196f, -6f);
            AddTextShadow(shopLabel.gameObject);

            var iconBg = CreateRoundedPanel("ShopIconBox", shopPanel, new Color(0.17f, 0.24f, 0.35f, 1f), new Vector2(82f, 70f));
            iconBg.anchorMin = new Vector2(1f, 0.5f);
            iconBg.anchorMax = new Vector2(1f, 0.5f);
            iconBg.anchoredPosition = new Vector2(-49f, 0f);
            iconBg.GetComponent<Image>().raycastTarget = false;

            var shopIcon = CreateImage("ShopIcon", iconBg, CardArtGenerator.GetUIIconSprite("shop"));
            StretchFull(shopIcon.rectTransform, 6f);

            // Live 5M Refresh Cooldown badge directly under Shop button
            var timerPill = CreateRoundedPanel("ShopTimerPill", container, new Color(0.08f, 0.12f, 0.19f, 0.95f), new Vector2(310f, 38f));
            timerPill.anchorMin = new Vector2(1f, 1f);
            timerPill.anchorMax = new Vector2(1f, 1f);
            timerPill.pivot = new Vector2(1f, 1f);
            timerPill.anchoredPosition = new Vector2(0f, -90f);
            AddOutline(timerPill.gameObject, new Color(0.28f, 0.78f, 0.44f), 1.6f);

            _shopTimerText = CreateText("ShopTimerLabel", timerPill, "5M REFRESH  |  04:59", 16, FontStyle.Bold, new Color(0.58f, 1.0f, 0.52f), TextAnchor.MiddleCenter);
            StretchFull(_shopTimerText.rectTransform, 6f);

            // Area Status badge directly below the Shop Refresh Cooldown pill
            var areaPill = CreateRoundedPanel("AreaStatusPill", container, new Color(0.07f, 0.12f, 0.20f, 0.95f), new Vector2(310f, 40f));
            areaPill.anchorMin = new Vector2(1f, 1f);
            areaPill.anchorMax = new Vector2(1f, 1f);
            areaPill.pivot = new Vector2(1f, 1f);
            areaPill.anchoredPosition = new Vector2(0f, -134f);
            AddOutline(areaPill.gameObject, new Color(0.30f, 0.72f, 0.96f), 1.6f);

            var locIcon = CreateImage("AreaPinIcon", areaPill, CardArtGenerator.GetUIIconSprite("location"));
            locIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            locIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            locIcon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            locIcon.rectTransform.sizeDelta = new Vector2(18f, 18f);

            _zonePillText = CreateText("ZonePillTxt", areaPill, "AREA: PLAYER'S NEIGHBORHOOD", 14, FontStyle.Bold, new Color(0.88f, 0.96f, 1.0f), TextAnchor.MiddleCenter);
            _zonePillText.resizeTextForBestFit = true;
            _zonePillText.resizeTextMinSize = 11;
            _zonePillText.resizeTextMaxSize = 14;
            _zonePillText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _zonePillText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _zonePillText.rectTransform.offsetMin = new Vector2(36f, 4f);
            _zonePillText.rectTransform.offsetMax = new Vector2(-12f, -4f);
            AddTextShadow(_zonePillText.gameObject);
        }

        private void CreateProximityPrompt()
        {
            _proximityPromptPanel = CreateRoundedPanel("ProximityPrompt", _canvas.transform, new Color(0.07f, 0.10f, 0.16f, 0.90f), new Vector2(840f, 44f)).gameObject;
            var rt = _proximityPromptPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 44f);
            AddOutline(_proximityPromptPanel, new Color(0.98f, 0.80f, 0.24f), 2f);

            _proximityPromptText = CreateText("PromptTxt", rt, "[E] CHALLENGE", 17, FontStyle.Bold, new Color(1.0f, 0.92f, 0.45f), TextAnchor.MiddleCenter);
            StretchFull(_proximityPromptText.rectTransform, 6f);
            _proximityPromptPanel.SetActive(false);
        }

        private void CreateBottomRightPamatoPanel()
        {
            _pamatoPanel = CreateRoundedPanel("MyPamatoPanel", _canvas.transform, new Color(0.11f, 0.16f, 0.25f, 0.95f), new Vector2(510f, 224f)).gameObject;
            var rt = _pamatoPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-22f, 20f);
            AddOutline(_pamatoPanel, new Color(0.32f, 0.45f, 0.64f), 2f);

            // Red X close/minimize button
            var closeBtn = CreateButton("MinimizePamatoBtn", rt, "X", new Color(0.84f, 0.18f, 0.20f), 16, () =>
            {
                _pamatoPanel.SetActive(false);
                _pamatoMinimizedBtn.SetActive(true);
            });
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-10f, -10f);
            closeRt.sizeDelta = new Vector2(30f, 30f);

            // Minimized restore button
            var restoreBtn = CreateButton("RestorePamatoBtn", _canvas.transform, "MY PAMATO", new Color(0.14f, 0.22f, 0.34f), 16, () =>
            {
                _pamatoPanel.SetActive(true);
                _pamatoMinimizedBtn.SetActive(false);
            });
            var resRt = restoreBtn.GetComponent<RectTransform>();
            resRt.anchorMin = new Vector2(1f, 0f);
            resRt.anchorMax = new Vector2(1f, 0f);
            resRt.pivot = new Vector2(1f, 0f);
            resRt.anchoredPosition = new Vector2(-22f, 20f);
            resRt.sizeDelta = new Vector2(150f, 44f);
            _pamatoMinimizedBtn = restoreBtn.gameObject;
            _pamatoMinimizedBtn.SetActive(false);

            // Left half: Fanned 3 cards (Back, Middle Bakunawa, Front Hero)
            var backCard = CreatePamatoMiniCard("BackCard", rt, new Vector2(88f, 114f), 15f, out _backCardFrame, out _backCardPortrait, out _, out _, out _, out _);
            var midCard = CreatePamatoMiniCard("MidCard", rt, new Vector2(138f, 118f), 7f, out _midCardFrame, out _midCardPortrait, out _, out _midCardName, out _midCardRarity, out _);
            var frontCard = CreatePamatoMiniCard("FrontCard", rt, new Vector2(198f, 122f), 0f, out _frontCardFrame, out _frontCardPortrait, out _frontCardStars, out _frontCardName, out _frontCardRarity, out _frontCardBottomStats);

            var frontBtn = frontCard.gameObject.AddComponent<Button>();
            frontBtn.onClick.AddListener(ToggleInventoryModal);

            // Right half: Dynamic "<PLAYER>'S PAMATO" title + 4 stat bars + Green UPGRADE button
            _pamatoHeaderText = CreateText("MyPamatoHeader", rt, $"{GetDisplayPlayerName().ToUpper()}'S PAMATO", 20, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            _pamatoHeaderText.resizeTextForBestFit = true;
            _pamatoHeaderText.resizeTextMinSize = 13;
            _pamatoHeaderText.resizeTextMaxSize = 20;
            _pamatoHeaderText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _pamatoHeaderText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _pamatoHeaderText.rectTransform.offsetMin = new Vector2(278f, -38f);
            _pamatoHeaderText.rectTransform.offsetMax = new Vector2(-42f, -8f);

            string[] iconNames = { "heart", "sword", "spin", "shield" };
            Color[] barColors = {
                new Color(0.94f, 0.24f, 0.28f),
                new Color(0.96f, 0.72f, 0.16f),
                new Color(0.22f, 0.85f, 0.36f),
                new Color(0.22f, 0.68f, 0.96f)
            };

            for (int i = 0; i < 4; i++)
            {
                float yPos = -54f - i * 24f;
                var icon = CreateImage("StatIcon_" + i, rt, CardArtGenerator.GetUIIconSprite(iconNames[i]));
                icon.rectTransform.anchorMin = new Vector2(0f, 1f);
                icon.rectTransform.anchorMax = new Vector2(0f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(288f, yPos);
                icon.rectTransform.sizeDelta = new Vector2(20f, 20f);

                var barBg = CreateRoundedPanel("StatBg_" + i, rt, new Color(0.06f, 0.09f, 0.15f, 0.95f), new Vector2(130f, 13f));
                barBg.anchorMin = new Vector2(0f, 1f);
                barBg.anchorMax = new Vector2(0f, 1f);
                barBg.anchoredPosition = new Vector2(368f, yPos);

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(barBg, false);
                var fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(0.8f, 1f);
                fillRt.offsetMin = new Vector2(1f, 1f);
                fillRt.offsetMax = new Vector2(-1f, -1f);
                var fillImg = fillGo.GetComponent<Image>();
                fillImg.color = barColors[i];
                _statFillBars[i] = fillImg;

                var valTxt = CreateText("StatVal_" + i, rt, "100", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
                valTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                valTxt.rectTransform.anchorMax = new Vector2(0f, 1f);
                valTxt.rectTransform.anchoredPosition = new Vector2(465f, yPos);
                valTxt.rectTransform.sizeDelta = new Vector2(54f, 22f);
                _statValueTexts[i] = valTxt;
            }

            // Green UPGRADE button
            var upgBtn = CreateButton("UpgradePamatoBtn", rt, "", new Color(0.18f, 0.72f, 0.30f), 16, OnPamatoUpgradeClicked);
            var upgRt = upgBtn.GetComponent<RectTransform>();
            upgRt.anchorMin = new Vector2(0f, 0f);
            upgRt.anchorMax = new Vector2(0f, 0f);
            upgRt.anchoredPosition = new Vector2(384f, 40f);
            upgRt.sizeDelta = new Vector2(204f, 52f);
            AddOutline(upgBtn.gameObject, new Color(0.68f, 1.0f, 0.58f), 1.5f);

            var upgTitle = CreateText("UpgTitle", upgRt, "UPGRADE", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            upgTitle.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            upgTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            upgTitle.rectTransform.offsetMin = Vector2.zero;
            upgTitle.rectTransform.offsetMax = Vector2.zero;

            _pamatoUpgradeSubText = CreateText("UpgSub", upgRt, "(Card Level up)", 13, FontStyle.Normal, new Color(0.88f, 1.0f, 0.88f), TextAnchor.MiddleCenter);
            _pamatoUpgradeSubText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _pamatoUpgradeSubText.rectTransform.anchorMax = new Vector2(1f, 0.48f);
            _pamatoUpgradeSubText.rectTransform.offsetMin = Vector2.zero;
            _pamatoUpgradeSubText.rectTransform.offsetMax = Vector2.zero;
        }

        private RectTransform CreatePamatoMiniCard(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            float rotZ,
            out Image frameImg,
            out Image portraitImg,
            out Text starsTxt,
            out Text nameTxt,
            out Text rarityTxt,
            out Text bottomStatsTxt)
        {
            var cardRt = CreateRoundedPanel(name, parent, new Color(0.96f, 0.68f, 0.14f), new Vector2(118f, 184f));
            cardRt.anchorMin = new Vector2(0f, 0f);
            cardRt.anchorMax = new Vector2(0f, 0f);
            cardRt.anchoredPosition = anchoredPos;
            cardRt.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            frameImg = cardRt.GetComponent<Image>();

            var inner = CreateRoundedPanel("Inner", cardRt, new Color(0.08f, 0.10f, 0.16f), Vector2.zero);
            StretchFull(inner, 4f);

            var portGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portGo.transform.SetParent(inner, false);
            var portRt = portGo.GetComponent<RectTransform>();
            portRt.anchorMin = new Vector2(0f, 0.25f);
            portRt.anchorMax = new Vector2(1f, 0.87f);
            portRt.offsetMin = new Vector2(4f, 2f);
            portRt.offsetMax = new Vector2(-4f, -2f);
            portraitImg = portGo.GetComponent<Image>();
            portraitImg.preserveAspect = false;

            starsTxt = CreateText("Stars", inner, "★★★★★", 13, FontStyle.Bold, new Color(1.0f, 0.85f, 0.18f), TextAnchor.MiddleCenter);
            starsTxt.rectTransform.anchorMin = new Vector2(0f, 0.86f);
            starsTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            starsTxt.rectTransform.offsetMin = Vector2.zero;
            starsTxt.rectTransform.offsetMax = Vector2.zero;

            nameTxt = CreateText("Name", inner, "HERO", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            nameTxt.rectTransform.anchorMin = new Vector2(0f, 0.16f);
            nameTxt.rectTransform.anchorMax = new Vector2(1f, 0.29f);
            nameTxt.rectTransform.offsetMin = new Vector2(2f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-2f, 0f);

            rarityTxt = CreateText("Rarity", inner, "Legendary", 11, FontStyle.Italic, new Color(0.92f, 0.84f, 0.56f), TextAnchor.MiddleCenter);
            rarityTxt.rectTransform.anchorMin = new Vector2(0f, 0.09f);
            rarityTxt.rectTransform.anchorMax = new Vector2(1f, 0.18f);
            rarityTxt.rectTransform.offsetMin = Vector2.zero;
            rarityTxt.rectTransform.offsetMax = Vector2.zero;

            bottomStatsTxt = CreateText("BottomStats", inner, "★ 48  ⚡ 21", 12, FontStyle.Bold, new Color(1.0f, 0.88f, 0.45f), TextAnchor.MiddleCenter);
            bottomStatsTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottomStatsTxt.rectTransform.anchorMax = new Vector2(1f, 0.10f);
            bottomStatsTxt.rectTransform.offsetMin = Vector2.zero;
            bottomStatsTxt.rectTransform.offsetMax = Vector2.zero;

            return cardRt;
        }

        private void CreateMatchHud()
        {
            _matchHudRoot = new GameObject("MatchHUDRoot", typeof(RectTransform));
            _matchHudRoot.transform.SetParent(_canvas.transform, false);
            var rootRt = _matchHudRoot.GetComponent<RectTransform>();
            StretchFull(rootRt, 0f);

            // Top-center match status banner
            var banner = CreateRoundedPanel("MatchTopBanner", rootRt, new Color(0.08f, 0.12f, 0.20f, 0.90f), new Vector2(640f, 74f));
            banner.anchorMin = new Vector2(0.5f, 1f);
            banner.anchorMax = new Vector2(0.5f, 1f);
            banner.anchoredPosition = new Vector2(0f, -54f);
            AddOutline(banner.gameObject, new Color(0.95f, 0.78f, 0.22f), 2f);
            _matchBannerRoot = banner.gameObject;

            _matchBannerTitle = CreateText("BannerTitle", banner, "TEKS SHOWDOWN", 22, FontStyle.Bold, new Color(1.0f, 0.88f, 0.30f), TextAnchor.MiddleCenter);
            _matchBannerTitle.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            _matchBannerTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            _matchBannerTitle.rectTransform.offsetMin = Vector2.zero;
            _matchBannerTitle.rectTransform.offsetMax = Vector2.zero;

            _matchBannerSub = CreateText("BannerSub", banner, "Hit the green sweet spot for maximum spin!", 16, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
            _matchBannerSub.rectTransform.anchorMin = new Vector2(0f, 0f);
            _matchBannerSub.rectTransform.anchorMax = new Vector2(1f, 0.50f);
            _matchBannerSub.rectTransform.offsetMin = Vector2.zero;
            _matchBannerSub.rectTransform.offsetMax = Vector2.zero;

            // Curved Flick Gauge at bottom center
            _flickGauge = CurvedFlickGauge.Create(rootRt, _font);
            var gaugeRt = _flickGauge.GetComponent<RectTransform>();
            gaugeRt.anchorMin = new Vector2(0.5f, 0f);
            gaugeRt.anchorMax = new Vector2(0.5f, 0f);
            gaugeRt.anchoredPosition = new Vector2(0f, 128f);
            _flickGauge.OnFlickClicked += () =>
            {
                TeksMatchManager.Instance?.ExecuteFlick();
            };

            // Round Result Panel (expanded to frame the 3 settled visual cards + outcome)
            _roundResultPanel = CreateRoundedPanel("RoundResultPanel", rootRt, new Color(0.08f, 0.12f, 0.21f, 0.96f), new Vector2(800f, 515f)).gameObject;
            var resRt = _roundResultPanel.GetComponent<RectTransform>();
            resRt.anchorMin = new Vector2(0.5f, 0.5f);
            resRt.anchorMax = new Vector2(0.5f, 0.5f);
            resRt.anchoredPosition = new Vector2(0f, 6f);
            AddOutline(_roundResultPanel, new Color(1.0f, 0.82f, 0.24f), 2.5f);

            _roundResultTitle = CreateText("ResultTitle", resRt, "YOU WIN!", 30, FontStyle.Bold, new Color(0.35f, 0.96f, 0.48f), TextAnchor.MiddleCenter);
            _roundResultTitle.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            _roundResultTitle.rectTransform.anchorMax = new Vector2(1f, 0.97f);
            _roundResultTitle.rectTransform.offsetMin = new Vector2(16f, 0f);
            _roundResultTitle.rectTransform.offsetMax = new Vector2(-16f, 0f);

            _roundResultCardsState = CreateText("ResultCardsState", resRt, "Your Pamato: CHUB (Face-Up)  |  Opponent: CHA (Face-Down)", 16, FontStyle.Bold, new Color(0.84f, 0.92f, 1.0f), TextAnchor.MiddleCenter);
            _roundResultCardsState.rectTransform.anchorMin = new Vector2(0f, 0.76f);
            _roundResultCardsState.rectTransform.anchorMax = new Vector2(1f, 0.85f);
            _roundResultCardsState.rectTransform.offsetMin = new Vector2(16f, 0f);
            _roundResultCardsState.rectTransform.offsetMax = new Vector2(-16f, 0f);

            _roundResultSubtitle = CreateText("ResultSub", resRt, "", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _roundResultSubtitle.rectTransform.anchorMin = new Vector2(0f, 0.14f);
            _roundResultSubtitle.rectTransform.anchorMax = new Vector2(1f, 0.25f);
            _roundResultSubtitle.rectTransform.offsetMin = new Vector2(16f, 0f);
            _roundResultSubtitle.rectTransform.offsetMax = new Vector2(-16f, 0f);

            _roundButtonsRow = new GameObject("ButtonsRow", typeof(RectTransform));
            _roundButtonsRow.transform.SetParent(resRt, false);
            var rowRt = _roundButtonsRow.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 0.02f);
            rowRt.anchorMax = new Vector2(1f, 0.14f);
            rowRt.offsetMin = Vector2.zero;
            rowRt.offsetMax = Vector2.zero;

            var rematchBtn = CreateButton("RematchBtn", rowRt, "REMATCH", new Color(0.18f, 0.72f, 0.32f), 19, () =>
            {
                if (TeksGameModeManager.Instance != null && TeksGameModeManager.Instance.IsSpecialModeActive)
                {
                    TeksGameModeManager.Instance.HandleResultPrimaryAction();
                }
                else
                {
                    TeksMatchManager.Instance?.Rematch();
                }
            });
            var rmRt = rematchBtn.GetComponent<RectTransform>();
            rmRt.anchoredPosition = new Vector2(-120f, 0f);
            rmRt.sizeDelta = new Vector2(210f, 44f);
            _roundPrimaryBtn = rematchBtn;
            _roundPrimaryBtnText = rematchBtn.GetComponentInChildren<Text>();

            var backBtn = CreateButton("BackToCourtBtn", rowRt, "BACK TO COURT", new Color(0.28f, 0.38f, 0.56f), 18, () =>
            {
                if (TeksGameModeManager.Instance != null && TeksGameModeManager.Instance.IsSpecialModeActive)
                {
                    TeksGameModeManager.Instance.HandleResultSecondaryAction();
                }
                else
                {
                    TeksMatchManager.Instance?.CancelChallenge();
                }
            });
            var bkRt = backBtn.GetComponent<RectTransform>();
            bkRt.anchoredPosition = new Vector2(120f, 0f);
            bkRt.sizeDelta = new Vector2(210f, 44f);
            _roundSecondaryBtn = backBtn;
            _roundSecondaryBtnText = backBtn.GetComponentInChildren<Text>();

            // Live 3-Card Flip & Result Showcase (rendered above RoundResultPanel so it sits inside it when RoundComplete and floats cleanly during FlickInAir)
            BuildCardFlipShowcase(rootRt);

            _matchHudRoot.SetActive(false);
        }

        private void BuildCardFlipShowcase(Transform parent)
        {
            var showcaseRt = CreateRoundedPanel("CardFlipShowcase", parent, new Color(0.06f, 0.09f, 0.16f, 0.72f), new Vector2(580f, 276f));
            showcaseRt.anchorMin = new Vector2(0.5f, 0.5f);
            showcaseRt.anchorMax = new Vector2(0.5f, 0.5f);
            showcaseRt.anchoredPosition = new Vector2(0f, 34f);
            AddOutline(showcaseRt.gameObject, new Color(0.32f, 0.52f, 0.78f, 0.8f), 2f);
            _cardFlipShowcaseRoot = showcaseRt.gameObject;

            var vsTxt = CreateText("ShowcaseVS", showcaseRt, "VS", 32, FontStyle.Bold, new Color(1.0f, 0.82f, 0.22f), TextAnchor.MiddleCenter);
            vsTxt.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            vsTxt.rectTransform.sizeDelta = new Vector2(80f, 50f);

            string[] roles = { "YOUR PAMATO", "OPPONENT'S PAMATO" };
            float[] xOffsets = { -150f, 150f };

            for (int i = 0; i < 2; i++)
            {
                var slotGo = new GameObject("FlipSlot_" + i, typeof(RectTransform));
                slotGo.transform.SetParent(showcaseRt, false);
                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.anchoredPosition = new Vector2(xOffsets[i], 0f);
                slotRt.sizeDelta = new Vector2(212f, 260f);

                var roleTxt = CreateText("RoleLabel", slotRt, roles[i], 14, FontStyle.Bold, new Color(0.84f, 0.92f, 1.0f), TextAnchor.MiddleCenter);
                roleTxt.rectTransform.anchoredPosition = new Vector2(0f, 114f);
                roleTxt.rectTransform.sizeDelta = new Vector2(210f, 24f);

                var winnerPill = CreateRoundedPanel("WinnerTag", slotRt, new Color(0.96f, 0.74f, 0.10f), new Vector2(118f, 24f));
                winnerPill.anchoredPosition = new Vector2(0f, 92f);
                var winTxt = CreateText("WinTxt", winnerPill, "WINNER!", 13, FontStyle.Bold, new Color(0.10f, 0.06f, 0.02f), TextAnchor.MiddleCenter);
                StretchFull(winTxt.rectTransform, 2f);
                winnerPill.gameObject.SetActive(false);

                // Animated Card Container (scales X via Cos(flipAngle) for 2.5D flip)
                var cardFrame = CreateRoundedPanel("FlipCardFrame", slotRt, Color.white, new Vector2(126f, 174f));
                cardFrame.anchoredPosition = new Vector2(0f, 2f);

                var inner = CreateRoundedPanel("Inner", cardFrame, new Color(0.08f, 0.10f, 0.16f), Vector2.zero);
                StretchFull(inner, 4f);

                var faceImg = CreateImage("FaceImage", inner, null);
                StretchFull(faceImg.rectTransform, 2f);

                var frontOverlay = new GameObject("FrontOverlay", typeof(RectTransform));
                frontOverlay.transform.SetParent(inner, false);
                StretchFull(frontOverlay.GetComponent<RectTransform>(), 0f);

                var starsTxt = CreateText("Stars", frontOverlay.transform, "★★★★★", 12, FontStyle.Bold, new Color(1.0f, 0.86f, 0.20f), TextAnchor.MiddleCenter);
                starsTxt.rectTransform.anchorMin = new Vector2(0f, 0.84f);
                starsTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                starsTxt.rectTransform.offsetMin = Vector2.zero;
                starsTxt.rectTransform.offsetMax = Vector2.zero;

                var nameBg = new GameObject("NameBg", typeof(RectTransform), typeof(Image));
                nameBg.transform.SetParent(frontOverlay.transform, false);
                var nbRt = nameBg.GetComponent<RectTransform>();
                nbRt.anchorMin = new Vector2(0f, 0f);
                nbRt.anchorMax = new Vector2(1f, 0.22f);
                nbRt.offsetMin = Vector2.zero;
                nbRt.offsetMax = Vector2.zero;
                nameBg.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.88f);

                var nameTxt = CreateText("Name", nameBg.transform, "HERO", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                StretchFull(nameTxt.rectTransform, 2f);

                // Status Stamp Pill at bottom of slot ("FLIPPING..." -> "CHUB (FACE-UP)" / "CHA (FACE-DOWN)")
                var statusPill = CreateRoundedPanel("StatusPill", slotRt, new Color(0.14f, 0.20f, 0.30f, 0.96f), new Vector2(182f, 30f));
                statusPill.anchoredPosition = new Vector2(0f, -108f);
                var statusTxt = CreateText("StatusTxt", statusPill, "FLIPPING...", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                StretchFull(statusTxt.rectTransform, 3f);

                _flipWidgets[i] = new FlipCardWidget
                {
                    SlotRoot = slotRt,
                    CardTransform = cardFrame,
                    FrameImage = cardFrame.GetComponent<Image>(),
                    FaceImage = faceImg,
                    FrontOverlay = frontOverlay,
                    CardNameText = nameTxt,
                    StarsText = starsTxt,
                    RoleText = roleTxt,
                    StatusPillBg = statusPill.GetComponent<Image>(),
                    StatusPillText = statusTxt,
                    WinnerTag = winnerPill.gameObject,
                    BackSprite = CardArtGenerator.GetCardBackSprite(),
                    BaseCardY = 2f
                };
            }

            _cardFlipShowcaseRoot.SetActive(false);
        }

        private void CreateInventoryModal()
        {
            _inventoryModal = CreateModalBackdrop("InventoryModal", new Vector2(1060f, 720f), out RectTransform window);

            var header = CreateText("InvModalHeader", window, "TEKS ALBUM, UPGRADES & CARD FUSION", 26, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.70f, 1f);
            header.rectTransform.offsetMin = new Vector2(28f, -58f);
            header.rectTransform.offsetMax = new Vector2(0f, -12f);

            _inventoryHeaderCoins = CreateText("InvModalCoins", window, "2,450 COINS", 22, FontStyle.Bold, new Color(1.0f, 0.88f, 0.36f), TextAnchor.MiddleRight);
            _inventoryHeaderCoins.rectTransform.anchorMin = new Vector2(0.6f, 1f);
            _inventoryHeaderCoins.rectTransform.anchorMax = new Vector2(1f, 1f);
            _inventoryHeaderCoins.rectTransform.offsetMin = new Vector2(0f, -56f);
            _inventoryHeaderCoins.rectTransform.offsetMax = new Vector2(-78f, -14f);

            var closeBtn = CreateButton("CloseInvBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, () =>
            {
                CloseSellConfirmModal();
                CloseAllModals();
            });
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            // Top Tab Switcher: MY CARDS | CARD FUSION LAB
            var cardsTabBtn = CreateButton("TabMyCardsBtn", window, "MY TEKS CARDS (UPGRADE & SELL)", new Color(0.20f, 0.52f, 0.86f), 15, () => SwitchInventoryTab(false));
            var ctRt = cardsTabBtn.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(0f, 1f);
            ctRt.anchoredPosition = new Vector2(180f, -84f);
            ctRt.sizeDelta = new Vector2(308f, 36f);
            _tabCardsBtnImg = cardsTabBtn.GetComponent<Image>();

            var fusionTabBtn = CreateButton("TabFusionBtn", window, "CARD FUSION LAB (RECIPES)", new Color(0.45f, 0.22f, 0.68f), 15, () => SwitchInventoryTab(true));
            var ftRt = fusionTabBtn.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0f, 1f);
            ftRt.anchorMax = new Vector2(0f, 1f);
            ftRt.anchoredPosition = new Vector2(496f, -84f);
            ftRt.sizeDelta = new Vector2(300f, 36f);
            _tabFusionBtnImg = fusionTabBtn.GetComponent<Image>();

            // Tab 1: Owned Cards Grid
            var scroll = CreateScrollView("InventoryScroll", window, new Vector2(24f, 22f), new Vector2(-24f, -112f), out _inventoryGridContent);
            _inventoryScrollRoot = scroll.gameObject;
            var grid = _inventoryGridContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(315f, 240f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var fitter = _inventoryGridContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Tab 2: Card Fusion Lab
            var fusionScroll = CreateScrollView("FusionScroll", window, new Vector2(24f, 22f), new Vector2(-24f, -112f), out _fusionListContent);
            _fusionLabRoot = fusionScroll.gameObject;
            var vlg = _fusionListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.padding = new RectOffset(14, 14, 14, 14);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fusionFitter = _fusionListContent.gameObject.AddComponent<ContentSizeFitter>();
            fusionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _fusionLabRoot.SetActive(false);

            _inventoryModal.SetActive(false);
        }

        private void CreateShopModal()
        {
            _shopModal = CreateModalBackdrop("ShopModal", new Vector2(1160f, 824f), out RectTransform window);

            var header = CreateText("ShopModalHeader", window, "SARI-SARI STORE (ALING NENA'S TEKS)", 25, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.56f, 1f);
            header.rectTransform.offsetMin = new Vector2(28f, -50f);
            header.rectTransform.offsetMax = new Vector2(0f, -10f);

            _shopModalTimerText = CreateText("ShopModalTimer", window, "NEW CARDS IN 04:59", 19, FontStyle.Bold, new Color(0.55f, 1.0f, 0.45f), TextAnchor.MiddleRight);
            _shopModalTimerText.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            _shopModalTimerText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _shopModalTimerText.rectTransform.offsetMin = new Vector2(0f, -38f);
            _shopModalTimerText.rectTransform.offsetMax = new Vector2(-84f, -10f);

            _shopModalCoinsText = CreateText("ShopModalCoins", window, "2,450 COINS", 16, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleRight);
            _shopModalCoinsText.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            _shopModalCoinsText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _shopModalCoinsText.rectTransform.offsetMin = new Vector2(0f, -62f);
            _shopModalCoinsText.rectTransform.offsetMax = new Vector2(-84f, -36f);

            var closeBtn = CreateButton("CloseShopBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            // Aligned Top Utility Row (Aling Nena Greeting + Quick Daily Draw + Sell Dupes + Refresh)
            var utilityGo = new GameObject("ShopUtilityRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            utilityGo.transform.SetParent(window, false);
            var utilRt = utilityGo.GetComponent<RectTransform>();
            utilRt.anchorMin = new Vector2(0f, 1f);
            utilRt.anchorMax = new Vector2(1f, 1f);
            utilRt.offsetMin = new Vector2(26f, -104f);
            utilRt.offsetMax = new Vector2(-26f, -66f);
            var utilHlg = utilityGo.GetComponent<HorizontalLayoutGroup>();
            utilHlg.spacing = 10f;
            utilHlg.childControlWidth = true;
            utilHlg.childControlHeight = true;
            utilHlg.childForceExpandWidth = false;
            utilHlg.childForceExpandHeight = true;

            var vendorBox = CreateRoundedPanel("VendorGreetingBox", utilityGo.transform, new Color(0.13f, 0.21f, 0.29f, 0.95f), new Vector2(540f, 38f));
            AddOutline(vendorBox.gameObject, new Color(0.28f, 0.44f, 0.62f), 1.4f);
            var vendorLe = vendorBox.gameObject.AddComponent<LayoutElement>();
            vendorLe.flexibleWidth = 1f;
            vendorLe.minWidth = 420f;
            _shopVendorBannerText = CreateText("VendorTxt", vendorBox, "Aling Nena: \"Bili na! May -30% Sulit Sale ngayon!\"", 14, FontStyle.Italic, new Color(1.0f, 0.94f, 0.72f), TextAnchor.MiddleLeft);
            _shopVendorBannerText.resizeTextForBestFit = true;
            _shopVendorBannerText.resizeTextMinSize = 11;
            _shopVendorBannerText.resizeTextMaxSize = 14;
            StretchFull(_shopVendorBannerText.rectTransform, 12f);

            _shopDailyFreeBtn = CreateButton("DailyBaryaBtn", utilityGo.transform, "DAILY FREE (3 CARDS)", new Color(0.84f, 0.54f, 0.10f), 13, OnDailyFreeDrawClicked);
            AddOutline(_shopDailyFreeBtn.gameObject, new Color(1.0f, 0.86f, 0.34f), 1.4f);
            var dfLe = _shopDailyFreeBtn.gameObject.AddComponent<LayoutElement>();
            dfLe.preferredWidth = 194f;
            _shopDailyFreeBtnText = _shopDailyFreeBtn.GetComponentInChildren<Text>();

            var sellAllBtn = CreateButton("SellAllDupesBtn", utilityGo.transform, "SELL EXTRA DUPES", new Color(0.62f, 0.34f, 0.16f), 13, () =>
            {
                if (PlayerInventory.Instance != null && PlayerInventory.Instance.TrySellAllExtraDuplicates(out int sold, out int earned))
                {
                    TeksAudioManager.Instance?.PlayCoinClink();
                    ShowToast($"Sold {sold} duplicate cards for +{earned} Barya Coins!");
                    PopulateShopModal();
                }
                else
                {
                    ShowToast("No extra duplicate cards to sell!");
                }
            });
            var saLe = sellAllBtn.gameObject.AddComponent<LayoutElement>();
            saLe.preferredWidth = 170f;

            var refreshBtn = CreateButton("ManualRefreshBtn", utilityGo.transform, "REFRESH (50c)", new Color(0.22f, 0.44f, 0.78f), 13, () =>
            {
                if (ShopManager.Instance != null && ShopManager.Instance.TryManualRefresh(out string msg))
                {
                    TeksAudioManager.Instance?.PlayCoinClink();
                    ShowToast(msg);
                    PopulateShopModal();
                }
                else
                {
                    ShowToast("Need 50 Coins to refresh early!");
                }
            });
            var rfLe = refreshBtn.gameObject.AddComponent<LayoutElement>();
            rfLe.preferredWidth = 142f;

            // Rotating Singles Row Header
            var singlesTitle = CreateText("SinglesTitle", window, "5-MINUTE ROTATING SINGLES (Includes a -30% SULIT SALE & Slot 4 SUPER SLOT Rare+)", 15, FontStyle.Bold, new Color(0.82f, 0.90f, 1.0f), TextAnchor.MiddleLeft);
            singlesTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            singlesTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            singlesTitle.rectTransform.offsetMin = new Vector2(28f, -130f);
            singlesTitle.rectTransform.offsetMax = new Vector2(-28f, -108f);

            // 4 Single Slots Container
            var slotsGo = new GameObject("SlotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slotsGo.transform.SetParent(window, false);
            var slotsRt = slotsGo.GetComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0f, 1f);
            slotsRt.anchorMax = new Vector2(1f, 1f);
            slotsRt.offsetMin = new Vector2(26f, -388f);
            slotsRt.offsetMax = new Vector2(-26f, -132f);
            var hlg = slotsGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            _shopSlotsContainer = slotsGo.transform;

            // Booster Packs Section (Illustrated Pack Cards with Foil Art, Ribbon, Odds & Action Pill)
            var boosterTitle = CreateText("BoosterTitle", window, "BOOSTER PACKS & DAILY FREE 3-CARD DRAW", 16, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            boosterTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            boosterTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            boosterTitle.rectTransform.offsetMin = new Vector2(28f, -418f);
            boosterTitle.rectTransform.offsetMax = new Vector2(-28f, -394f);

            var packsRow = new GameObject("PacksRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            packsRow.transform.SetParent(window, false);
            var prRt = packsRow.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f);
            prRt.anchorMax = new Vector2(1f, 1f);
            prRt.offsetMin = new Vector2(26f, -618f);
            prRt.offsetMax = new Vector2(-26f, -420f);
            var pHlg = packsRow.GetComponent<HorizontalLayoutGroup>();
            pHlg.spacing = 12f;
            pHlg.childControlWidth = true;
            pHlg.childControlHeight = true;
            pHlg.childForceExpandWidth = true;
            pHlg.childForceExpandHeight = true;

            CreateShopPackCard(
                "BasicPackBtn",
                packsRow.transform,
                ShopPackType.Basic,
                "CLASSIC • 3 CARDS",
                "BASIC PACK",
                "55/25/12/6/2 Drop Rates",
                "150 COINS",
                "coin",
                new Color(0.11f, 0.22f, 0.40f),
                new Color(0.16f, 0.44f, 0.78f),
                new Color(0.32f, 0.72f, 1.0f),
                new Color(0.16f, 0.56f, 0.88f),
                () => OpenShopPack(ShopPackType.Basic),
                out _,
                out _
            );

            CreateShopPackCard(
                "PremiumPackBtn",
                packsRow.transform,
                ShopPackType.Premium,
                "VALUE • 5 CARDS",
                "PREMIUM PACK",
                "Guaranteed Uncommon+ 5th",
                "300 COINS",
                "coin",
                new Color(0.09f, 0.24f, 0.20f),
                new Color(0.14f, 0.48f, 0.34f),
                new Color(0.28f, 0.88f, 0.54f),
                new Color(0.16f, 0.66f, 0.40f),
                () => OpenShopPack(ShopPackType.Premium),
                out _,
                out _
            );

            CreateShopPackCard(
                "RarePackBtn",
                packsRow.transform,
                ShopPackType.Rare,
                "BOOSTED • 4 CARDS",
                "RARE PACK",
                "High Odds + Rare+ 4th",
                "400 COINS",
                "coin",
                new Color(0.20f, 0.11f, 0.34f),
                new Color(0.46f, 0.20f, 0.72f),
                new Color(0.76f, 0.46f, 1.0f),
                new Color(0.56f, 0.24f, 0.84f),
                () => OpenShopPack(ShopPackType.Rare),
                out _,
                out _
            );

            CreateShopPackCard(
                "SpecialGemPackBtn",
                packsRow.transform,
                ShopPackType.SpecialGem,
                "ELITE • 5 RARE+",
                "MYTHIC GEM PACK",
                "Guaranteed Epic/Legend!",
                "30 GEMS",
                "gem",
                new Color(0.26f, 0.14f, 0.10f),
                new Color(0.70f, 0.34f, 0.08f),
                new Color(1.0f, 0.76f, 0.24f),
                new Color(0.84f, 0.46f, 0.10f),
                () => OpenShopPack(ShopPackType.SpecialGem),
                out _,
                out _
            );

            _shopDailyPackBtn = CreateShopPackCard(
                "DailyFreePackBtn",
                packsRow.transform,
                ShopPackType.DailyFree,
                "FREE • 3 CARDS + 200c",
                "DAILY FREE DRAW",
                "3 Free Cards Every Day!",
                "CLAIM FREE",
                "star",
                new Color(0.26f, 0.16f, 0.08f),
                new Color(0.72f, 0.42f, 0.10f),
                new Color(1.0f, 0.86f, 0.28f),
                new Color(0.18f, 0.72f, 0.34f),
                OnDailyFreeDrawClicked,
                out _shopDailyPackPillImg,
                out _shopDailyPackBtnText
            );

            // Recessed Pack Reveal Tray (Cleanly Centered Alignment for 3 Daily Cards or 4-5 Pack Cards)
            var revealTray = CreateRoundedPanel("PackRevealTray", window, new Color(0.05f, 0.08f, 0.14f, 0.96f), Vector2.zero);
            revealTray.anchorMin = new Vector2(0f, 0f);
            revealTray.anchorMax = new Vector2(1f, 0f);
            revealTray.offsetMin = new Vector2(26f, 14f);
            revealTray.offsetMax = new Vector2(-26f, 194f);
            AddOutline(revealTray.gameObject, new Color(0.26f, 0.38f, 0.56f), 1.5f);

            _packRevealHeader = CreateText("PackRevealHeader", revealTray, "Open a Booster Pack or Claim your Daily Free 3-Card Draw to reveal new Teks cards here!", 14, FontStyle.Italic, new Color(0.84f, 0.90f, 0.98f), TextAnchor.MiddleLeft);
            _packRevealHeader.rectTransform.anchorMin = new Vector2(0f, 1f);
            _packRevealHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
            _packRevealHeader.rectTransform.offsetMin = new Vector2(16f, -30f);
            _packRevealHeader.rectTransform.offsetMax = new Vector2(-16f, -6f);

            var revealGo = new GameObject("PackRevealRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            revealGo.transform.SetParent(revealTray, false);
            var revRt = revealGo.GetComponent<RectTransform>();
            revRt.anchorMin = new Vector2(0f, 0f);
            revRt.anchorMax = new Vector2(1f, 1f);
            revRt.offsetMin = new Vector2(14f, 10f);
            revRt.offsetMax = new Vector2(-14f, -32f);
            var revHlg = revealGo.GetComponent<HorizontalLayoutGroup>();
            revHlg.spacing = 12f;
            revHlg.childAlignment = TextAnchor.MiddleCenter;
            revHlg.childControlWidth = false;
            revHlg.childControlHeight = true;
            revHlg.childForceExpandWidth = false;
            revHlg.childForceExpandHeight = true;
            _packRevealContainer = revealGo.transform;

            _shopModal.SetActive(false);
        }

        private Button CreateShopPackCard(
            string name,
            Transform parent,
            ShopPackType packType,
            string ribbonLabel,
            string packTitle,
            string oddsSummary,
            string actionLabel,
            string actionIconName,
            Color cardBgColor,
            Color ribbonBgColor,
            Color borderColor,
            Color pillBgColor,
            Action onClick,
            out Image pillImg,
            out Text pillText)
        {
            var cardRt = CreateRoundedPanel(name, parent, cardBgColor, new Vector2(210f, 196f));
            AddOutline(cardRt.gameObject, borderColor, 2f);

            var btn = cardRt.gameObject.AddComponent<Button>();
            ApplyButtonTint(btn, cardBgColor);
            btn.onClick.AddListener(() =>
            {
                TeksAudioManager.Instance?.PlayUiClick();
                onClick?.Invoke();
            });

            // Top Ribbon Banner
            var ribbonRt = CreateRoundedPanel("Ribbon", cardRt, ribbonBgColor, Vector2.zero);
            ribbonRt.anchorMin = new Vector2(0.04f, 1f);
            ribbonRt.anchorMax = new Vector2(0.96f, 1f);
            ribbonRt.offsetMin = new Vector2(0f, -26f);
            ribbonRt.offsetMax = new Vector2(0f, -4f);
            ribbonRt.GetComponent<Image>().raycastTarget = false;

            var ribbonTxt = CreateText("RibbonTxt", ribbonRt, ribbonLabel, 10, FontStyle.Bold, new Color(1.0f, 0.96f, 0.84f), TextAnchor.MiddleCenter);
            StretchFull(ribbonTxt.rectTransform, 2f);

            // Center Foil Pack Artwork Backdrop + Sprite
            var haloRt = CreateRoundedPanel("PackHalo", cardRt, new Color(1f, 1f, 1f, 0.08f), new Vector2(82f, 82f));
            haloRt.anchorMin = new Vector2(0.5f, 1f);
            haloRt.anchorMax = new Vector2(0.5f, 1f);
            haloRt.anchoredPosition = new Vector2(0f, -74f);
            haloRt.GetComponent<Image>().raycastTarget = false;

            var artImg = CreateImage("PackArt", cardRt, CardArtGenerator.GetShopPackSprite(packType));
            artImg.preserveAspect = true;
            artImg.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            artImg.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            artImg.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            artImg.rectTransform.sizeDelta = new Vector2(68f, 88f);

            // Pack Title & Odds Subtext
            var titleTxt = CreateText("PackTitle", cardRt, packTitle, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 10;
            titleTxt.resizeTextMaxSize = 13;
            titleTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleTxt.rectTransform.offsetMin = new Vector2(8f, -141f);
            titleTxt.rectTransform.offsetMax = new Vector2(-8f, -121f);
            AddTextShadow(titleTxt.gameObject);

            var subTxt = CreateText("PackSub", cardRt, oddsSummary, 10, FontStyle.Normal, new Color(0.84f, 0.92f, 1.0f), TextAnchor.MiddleCenter);
            subTxt.resizeTextForBestFit = true;
            subTxt.resizeTextMinSize = 9;
            subTxt.resizeTextMaxSize = 11;
            subTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            subTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            subTxt.rectTransform.offsetMin = new Vector2(8f, -159f);
            subTxt.rectTransform.offsetMax = new Vector2(-8f, -141f);

            // Bottom Action / Price Pill
            var pillRt = CreateRoundedPanel("ActionPill", cardRt, pillBgColor, Vector2.zero);
            pillRt.anchorMin = new Vector2(0f, 0f);
            pillRt.anchorMax = new Vector2(1f, 0f);
            pillRt.offsetMin = new Vector2(8f, 7f);
            pillRt.offsetMax = new Vector2(-8f, 35f);
            pillImg = pillRt.GetComponent<Image>();
            pillImg.raycastTarget = false;
            AddOutline(pillRt.gameObject, new Color(1f, 1f, 1f, 0.35f), 1f);

            var actIcon = CreateImage("ActionIcon", pillRt, CardArtGenerator.GetUIIconSprite(actionIconName));
            actIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            actIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            actIcon.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            actIcon.rectTransform.sizeDelta = new Vector2(16f, 16f);

            pillText = CreateText("PriceTxt", pillRt, actionLabel, 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            pillText.resizeTextForBestFit = true;
            pillText.resizeTextMinSize = 10;
            pillText.resizeTextMaxSize = 12;
            pillText.rectTransform.anchorMin = new Vector2(0f, 0f);
            pillText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pillText.rectTransform.offsetMin = new Vector2(26f, 2f);
            pillText.rectTransform.offsetMax = new Vector2(-6f, -2f);
            AddTextShadow(pillText.gameObject);

            return btn;
        }

        private void CreateWagerModal()
        {
            _wagerModal = CreateModalBackdrop("WagerModal", new Vector2(940f, 664f), out RectTransform window);
            window.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.18f, 0.99f);
            AddOutline(window.gameObject, new Color(0.36f, 0.56f, 0.86f), 2.5f);

            // Top Royal-Navy Arcade Showdown Header Bar
            var headerBar = CreateRoundedPanel("WagerHeaderBar", window, new Color(0.05f, 0.08f, 0.16f, 1f), Vector2.zero);
            headerBar.anchorMin = new Vector2(0f, 1f);
            headerBar.anchorMax = new Vector2(1f, 1f);
            headerBar.offsetMin = new Vector2(2f, -56f);
            headerBar.offsetMax = new Vector2(-2f, -2f);
            headerBar.GetComponent<Image>().raycastTarget = false;

            var headerGoldRule = CreateRoundedPanel("HeaderGoldRule", headerBar, new Color(0.96f, 0.76f, 0.22f), Vector2.zero);
            headerGoldRule.anchorMin = new Vector2(0f, 0f);
            headerGoldRule.anchorMax = new Vector2(1f, 0f);
            headerGoldRule.offsetMin = new Vector2(0f, 0f);
            headerGoldRule.offsetMax = new Vector2(0f, 2f);
            headerGoldRule.GetComponent<Image>().raycastTarget = false;

            var title = CreateText("WagerTitle", headerBar, "TEKS SHOWDOWN & WAGER SETUP", 23, FontStyle.Bold, new Color(1.0f, 0.86f, 0.28f), TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(24f, 2f);
            title.rectTransform.offsetMax = new Vector2(-220f, -2f);
            AddTextShadow(title.gameObject);

            var modePill = CreateRoundedPanel("ModeFormatPill", headerBar, new Color(0.14f, 0.22f, 0.36f), new Vector2(178f, 28f));
            modePill.anchorMin = new Vector2(1f, 0.5f);
            modePill.anchorMax = new Vector2(1f, 0.5f);
            modePill.anchoredPosition = new Vector2(-112f, 0f);
            modePill.GetComponent<Image>().raycastTarget = false;
            AddOutline(modePill.gameObject, new Color(0.96f, 0.78f, 0.28f), 1.3f);
            var modePillTxt = CreateText("ModePillTxt", modePill, "2-CARD PAMATO DUEL", 11, FontStyle.Bold, new Color(1.0f, 0.92f, 0.56f), TextAnchor.MiddleCenter);
            StretchFull(modePillTxt.rectTransform, 2f);

            // Challenge Dialogue Quote Ribbon
            var quoteBox = CreateRoundedPanel("WagerQuoteBox", window, new Color(0.10f, 0.16f, 0.27f, 0.98f), new Vector2(884f, 38f));
            quoteBox.anchoredPosition = new Vector2(0f, 246f);
            quoteBox.GetComponent<Image>().raycastTarget = false;
            AddOutline(quoteBox.gameObject, new Color(0.94f, 0.74f, 0.24f, 0.78f), 1.4f);

            var quoteAccent = CreateRoundedPanel("QuoteAccent", quoteBox, new Color(0.98f, 0.78f, 0.24f), Vector2.zero);
            quoteAccent.anchorMin = new Vector2(0f, 0f);
            quoteAccent.anchorMax = new Vector2(0f, 1f);
            quoteAccent.offsetMin = new Vector2(4f, 5f);
            quoteAccent.offsetMax = new Vector2(8f, -5f);
            quoteAccent.GetComponent<Image>().raycastTarget = false;

            _wagerChallengeQuoteText = CreateText("WagerQuoteTxt", quoteBox, "Dodong: \"Hoy! Teks tayo!\"", 14, FontStyle.Italic, new Color(1.0f, 0.95f, 0.76f), TextAnchor.MiddleCenter);
            _wagerChallengeQuoteText.resizeTextForBestFit = true;
            _wagerChallengeQuoteText.resizeTextMinSize = 11;
            _wagerChallengeQuoteText.resizeTextMaxSize = 14;
            _wagerChallengeQuoteText.verticalOverflow = VerticalWrapMode.Truncate;
            _wagerChallengeQuoteText.rectTransform.anchorMin = Vector2.zero;
            _wagerChallengeQuoteText.rectTransform.anchorMax = Vector2.one;
            _wagerChallengeQuoteText.rectTransform.offsetMin = new Vector2(18f, 4f);
            _wagerChallengeQuoteText.rectTransform.offsetMax = new Vector2(-14f, -4f);

            // LEFT PLAYER DUELIST CARD (Emerald Theme)
            var playerBox = CreateRoundedPanel("WagerPlayerBox", window, new Color(0.09f, 0.14f, 0.23f, 1f), new Vector2(392f, 294f));
            playerBox.anchoredPosition = new Vector2(-244f, 68f);
            AddOutline(playerBox.gameObject, new Color(0.28f, 0.82f, 0.48f), 2.0f);

            var pHdr = CreateRoundedPanel("PlayerHeaderBand", playerBox, new Color(0.08f, 0.24f, 0.20f, 1f), Vector2.zero);
            pHdr.anchorMin = new Vector2(0f, 1f);
            pHdr.anchorMax = new Vector2(1f, 1f);
            pHdr.offsetMin = new Vector2(2f, -38f);
            pHdr.offsetMax = new Vector2(-2f, -2f);
            pHdr.GetComponent<Image>().raycastTarget = false;

            _wagerPlayerName = CreateText("PlayerPamatoLbl", pHdr, "PLAYER", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            _wagerPlayerName.resizeTextForBestFit = true;
            _wagerPlayerName.resizeTextMinSize = 11;
            _wagerPlayerName.resizeTextMaxSize = 15;
            _wagerPlayerName.rectTransform.anchorMin = Vector2.zero;
            _wagerPlayerName.rectTransform.anchorMax = Vector2.one;
            _wagerPlayerName.rectTransform.offsetMin = new Vector2(14f, 2f);
            _wagerPlayerName.rectTransform.offsetMax = new Vector2(-114f, -2f);
            AddTextShadow(_wagerPlayerName.gameObject);

            var pOddsPill = CreateRoundedPanel("PlayerOddsPill", pHdr, new Color(0.10f, 0.46f, 0.26f), new Vector2(98f, 24f));
            pOddsPill.anchorMin = new Vector2(1f, 0.5f);
            pOddsPill.anchorMax = new Vector2(1f, 0.5f);
            pOddsPill.anchoredPosition = new Vector2(-58f, 0f);
            pOddsPill.GetComponent<Image>().raycastTarget = false;
            AddOutline(pOddsPill.gameObject, new Color(0.45f, 0.98f, 0.58f), 1.3f);
            _wagerPlayerOddsPillText = CreateText("POddsTxt", pOddsPill, "50% CHUB", 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(_wagerPlayerOddsPillText.rectTransform, 2f);

            var playerAvatarFrame = CreateRoundedPanel("WagerPlayerAvatarFrame", playerBox, new Color(0.06f, 0.10f, 0.18f), new Vector2(98f, 98f));
            playerAvatarFrame.anchoredPosition = new Vector2(-114f, 48f);
            AddOutline(playerAvatarFrame.gameObject, new Color(1.0f, 0.84f, 0.28f), 1.8f);
            _wagerPlayerAvatarImg = CreateImage("WagerPlayerAvatarImg", playerAvatarFrame, CharacterResourceGenerator.GetCharacterPortraitSprite(PlayerCharacterType.StreetKid));
            StretchFull(_wagerPlayerAvatarImg.rectTransform, 4f);

            var playerBadgeBox = CreateRoundedPanel("WagerPlayerBadgeBox", playerBox, new Color(0.06f, 0.10f, 0.17f, 0.96f), new Vector2(138f, 44f));
            playerBadgeBox.anchoredPosition = new Vector2(-114f, -32f);
            playerBadgeBox.GetComponent<Image>().raycastTarget = false;
            AddOutline(playerBadgeBox.gameObject, new Color(0.28f, 0.62f, 0.46f, 0.7f), 1f);
            _wagerPlayerBadgeText = CreateText("WagerPlayerBadgeTxt", playerBadgeBox, "Baguhan\nWins: 0", 11, FontStyle.Bold, new Color(0.84f, 0.94f, 1.0f), TextAnchor.MiddleCenter);
            _wagerPlayerBadgeText.resizeTextForBestFit = true;
            _wagerPlayerBadgeText.resizeTextMinSize = 9;
            _wagerPlayerBadgeText.resizeTextMaxSize = 11;
            StretchFull(_wagerPlayerBadgeText.rectTransform, 4f);

            var cycleBtn = CreateButton("CyclePamatoBtn", playerBox, "", new Color(0.16f, 0.44f, 0.78f), 13, OpenCardPickerModal);
            var cyRt = cycleBtn.GetComponent<RectTransform>();
            cyRt.anchoredPosition = new Vector2(-114f, -94f);
            cyRt.sizeDelta = new Vector2(138f, 38f);
            ApplyButtonTint(cycleBtn, new Color(0.16f, 0.44f, 0.78f));
            AddOutline(cycleBtn.gameObject, new Color(0.46f, 0.80f, 1.0f), 1.4f);
            var cycleGloss = CreateRoundedPanel("CycleGloss", cyRt, new Color(1f, 1f, 1f, 0.14f), Vector2.zero);
            cycleGloss.anchorMin = new Vector2(0.04f, 0.58f);
            cycleGloss.anchorMax = new Vector2(0.96f, 0.93f);
            cycleGloss.offsetMin = Vector2.zero;
            cycleGloss.offsetMax = Vector2.zero;
            cycleGloss.GetComponent<Image>().raycastTarget = false;
            var cycleLbl = CreateText("Label", cyRt, "CHOOSE CARD", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(cycleLbl.rectTransform, 4f);
            AddTextShadow(cycleLbl.gameObject);

            // Player Framed 2:3 Collectible Pamato Card Slab
            var playerCardSlab = CreateCollectibleCardSlab("WagerPlayerCardSlab", playerBox, null, 1, new Vector2(116f, 174f), out _wagerPlayerCardFrame, out _wagerPlayerCardImg);
            playerCardSlab.anchoredPosition = new Vector2(68f, 16f);
            AttachWagerCardBadges(playerCardSlab, out _wagerPlayerCardLevelBadge, out _wagerPlayerCardStarsText);

            _wagerPlayerCardText = CreateText("WagerPlayerCardTxt", playerBox, "HERO\n[Legendary • Heroes]", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _wagerPlayerCardText.supportRichText = true;
            _wagerPlayerCardText.resizeTextForBestFit = true;
            _wagerPlayerCardText.resizeTextMinSize = 10;
            _wagerPlayerCardText.resizeTextMaxSize = 12;
            _wagerPlayerCardText.rectTransform.anchoredPosition = new Vector2(68f, -104f);
            _wagerPlayerCardText.rectTransform.sizeDelta = new Vector2(204f, 44f);
            AddTextShadow(_wagerPlayerCardText.gameObject);

            // RIGHT OPPONENT DUELIST CARD (Fiery Amber Theme — Mirrored!)
            var oppBox = CreateRoundedPanel("WagerOppBox", window, new Color(0.09f, 0.14f, 0.23f, 1f), new Vector2(392f, 294f));
            oppBox.anchoredPosition = new Vector2(244f, 68f);
            AddOutline(oppBox.gameObject, new Color(0.96f, 0.52f, 0.22f), 2.0f);

            var oHdr = CreateRoundedPanel("OppHeaderBand", oppBox, new Color(0.28f, 0.14f, 0.10f, 1f), Vector2.zero);
            oHdr.anchorMin = new Vector2(0f, 1f);
            oHdr.anchorMax = new Vector2(1f, 1f);
            oHdr.offsetMin = new Vector2(2f, -38f);
            oHdr.offsetMax = new Vector2(-2f, -2f);
            oHdr.GetComponent<Image>().raycastTarget = false;

            _wagerOppName = CreateText("WagerOppLbl", oHdr, "OPPONENT", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            _wagerOppName.resizeTextForBestFit = true;
            _wagerOppName.resizeTextMinSize = 11;
            _wagerOppName.resizeTextMaxSize = 15;
            _wagerOppName.rectTransform.anchorMin = Vector2.zero;
            _wagerOppName.rectTransform.anchorMax = Vector2.one;
            _wagerOppName.rectTransform.offsetMin = new Vector2(14f, 2f);
            _wagerOppName.rectTransform.offsetMax = new Vector2(-114f, -2f);
            AddTextShadow(_wagerOppName.gameObject);

            var oOddsPill = CreateRoundedPanel("OppOddsPill", oHdr, new Color(0.56f, 0.26f, 0.08f), new Vector2(98f, 24f));
            oOddsPill.anchorMin = new Vector2(1f, 0.5f);
            oOddsPill.anchorMax = new Vector2(1f, 0.5f);
            oOddsPill.anchoredPosition = new Vector2(-58f, 0f);
            oOddsPill.GetComponent<Image>().raycastTarget = false;
            AddOutline(oOddsPill.gameObject, new Color(1.0f, 0.72f, 0.30f), 1.3f);
            _wagerOppOddsPillText = CreateText("OOddsTxt", oOddsPill, "50% CHUB", 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(_wagerOppOddsPillText.rectTransform, 2f);

            // Opponent Framed 2:3 Collectible Pamato Card Slab (facing center VS!)
            var oppCardSlab = CreateCollectibleCardSlab("WagerOppCardSlab", oppBox, null, 1, new Vector2(116f, 174f), out _wagerOppCardFrame, out _wagerOppCardImg);
            oppCardSlab.anchoredPosition = new Vector2(-68f, 16f);
            AttachWagerCardBadges(oppCardSlab, out _wagerOppCardLevelBadge, out _wagerOppCardStarsText);

            _wagerOppCardText = CreateText("WagerOppCardTxt", oppBox, "PAMATO\n[Rare • Heroes]", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _wagerOppCardText.supportRichText = true;
            _wagerOppCardText.resizeTextForBestFit = true;
            _wagerOppCardText.resizeTextMinSize = 10;
            _wagerOppCardText.resizeTextMaxSize = 12;
            _wagerOppCardText.rectTransform.anchoredPosition = new Vector2(-68f, -104f);
            _wagerOppCardText.rectTransform.sizeDelta = new Vector2(204f, 44f);
            AddTextShadow(_wagerOppCardText.gameObject);

            var oppAvatarFrame = CreateRoundedPanel("WagerOppAvatarFrame", oppBox, new Color(0.06f, 0.10f, 0.18f), new Vector2(98f, 98f));
            oppAvatarFrame.anchoredPosition = new Vector2(114f, 48f);
            AddOutline(oppAvatarFrame.gameObject, new Color(0.98f, 0.60f, 0.24f), 1.8f);
            _wagerOppAvatarImg = CreateImage("WagerOppAvatarImg", oppAvatarFrame, null);
            StretchFull(_wagerOppAvatarImg.rectTransform, 4f);

            var oppBadgeBox = CreateRoundedPanel("WagerOppBadgeBox", oppBox, new Color(0.06f, 0.10f, 0.17f, 0.96f), new Vector2(138f, 74f));
            oppBadgeBox.anchoredPosition = new Vector2(114f, -46f);
            oppBadgeBox.GetComponent<Image>().raycastTarget = false;
            AddOutline(oppBadgeBox.gameObject, new Color(0.94f, 0.56f, 0.24f, 0.7f), 1f);
            _wagerOppBadgeText = CreateText("WagerOppBadgeTxt", oppBadgeBox, "Area\n[Story]", 11, FontStyle.Bold, new Color(1.0f, 0.88f, 0.74f), TextAnchor.MiddleCenter);
            _wagerOppBadgeText.resizeTextForBestFit = true;
            _wagerOppBadgeText.resizeTextMinSize = 9;
            _wagerOppBadgeText.resizeTextMaxSize = 11;
            StretchFull(_wagerOppBadgeText.rectTransform, 4f);

            // Center Glowing VS Medallion Badge (layered cleanly on top of gap)
            var vsBadge = CreateRoundedPanel("VsMedallion", window, new Color(0.07f, 0.10f, 0.18f, 1f), new Vector2(78f, 78f));
            vsBadge.anchoredPosition = new Vector2(0f, 68f);
            vsBadge.GetComponent<Image>().raycastTarget = false;
            AddOutline(vsBadge.gameObject, new Color(1.0f, 0.84f, 0.24f), 2.8f);

            var vsInner = CreateRoundedPanel("VsInner", vsBadge, new Color(0.32f, 0.13f, 0.08f, 1f), Vector2.zero);
            StretchFull(vsInner, 7f);
            vsInner.GetComponent<Image>().raycastTarget = false;
            AddOutline(vsInner.gameObject, new Color(1.0f, 0.58f, 0.20f), 1.4f);

            var vsTxt = CreateText("VsText", vsInner, "VS", 30, FontStyle.Bold, new Color(1.0f, 0.88f, 0.26f), TextAnchor.MiddleCenter);
            StretchFull(vsTxt.rectTransform, 2f);
            AddTextShadow(vsTxt.gameObject);

            // Head-to-Head Colored Stat Pills Row
            var statRowGo = new GameObject("WagerStatCompareRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            statRowGo.transform.SetParent(window, false);
            var statRowRt = statRowGo.GetComponent<RectTransform>();
            statRowRt.anchoredPosition = new Vector2(0f, -110f);
            statRowRt.sizeDelta = new Vector2(884f, 44f);
            var statHlg = statRowGo.GetComponent<HorizontalLayoutGroup>();
            statHlg.spacing = 12f;
            statHlg.childControlWidth = true;
            statHlg.childControlHeight = true;
            statHlg.childForceExpandWidth = true;
            statHlg.childForceExpandHeight = true;

            _wagerPwrCompareText = CreateWagerStatPill("PwrPill", statRowGo.transform, new Color(1.0f, 0.80f, 0.24f), "POWER: 0 vs 0");
            _wagerHpCompareText = CreateWagerStatPill("HpPill", statRowGo.transform, new Color(0.34f, 0.92f, 0.56f), "HP: 0 vs 0");
            _wagerAtkCompareText = CreateWagerStatPill("AtkPill", statRowGo.transform, new Color(1.0f, 0.48f, 0.36f), "ATK: 0 vs 0");

            // Recessed Wager Control Deck
            var wagerDeck = CreateRoundedPanel("WagerControlDeck", window, new Color(0.05f, 0.08f, 0.15f, 0.98f), new Vector2(884f, 114f));
            wagerDeck.anchoredPosition = new Vector2(0f, -198f);
            AddOutline(wagerDeck.gameObject, new Color(0.28f, 0.42f, 0.64f), 1.5f);

            _wagerAmountText = CreateText("WagerAmountTxt", wagerDeck, "MATCH WAGER: 100 COINS", 18, FontStyle.Bold, new Color(1.0f, 0.88f, 0.32f), TextAnchor.MiddleCenter);
            _wagerAmountText.resizeTextForBestFit = true;
            _wagerAmountText.resizeTextMinSize = 13;
            _wagerAmountText.resizeTextMaxSize = 18;
            _wagerAmountText.rectTransform.anchoredPosition = new Vector2(0f, 34f);
            _wagerAmountText.rectTransform.sizeDelta = new Vector2(844f, 28f);
            AddTextShadow(_wagerAmountText.gameObject);

            for (int i = 0; i < WagerChipValues.Length; i++)
            {
                int val = WagerChipValues[i];
                string chipLabel = val == 0 ? "FREE (0c)" : $"{val} COINS";
                var chipBtn = CreateButton("Wager_" + val, wagerDeck, chipLabel, new Color(0.18f, 0.27f, 0.42f), 13, () =>
                {
                    int maxCoins = PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : 0;
                    if (val == 0)
                    {
                        _selectedWagerAmount = 0;
                    }
                    else
                    {
                        _selectedWagerAmount = Mathf.Clamp(val, 0, Mathf.Max(0, maxCoins));
                        if (val > maxCoins)
                        {
                            ShowToast($"Need {val:N0} Coins for that wager — set to {maxCoins:N0} Coins.");
                        }
                    }
                    RefreshWagerText();
                });

                var bRt = chipBtn.GetComponent<RectTransform>();
                bRt.anchoredPosition = new Vector2(-304f + i * 152f, -2f);
                bRt.sizeDelta = new Vector2(138f, 34f);
                ApplyButtonTint(chipBtn, new Color(0.18f, 0.27f, 0.42f));
                AddOutline(chipBtn.gameObject, new Color(0.36f, 0.56f, 0.84f), 1.3f);

                _wagerChipImgs[i] = chipBtn.GetComponent<Image>();
                _wagerChipOutlines[i] = chipBtn.GetComponent<Outline>();
                _wagerChipTexts[i] = chipBtn.GetComponentInChildren<Text>();
                if (_wagerChipTexts[i] != null)
                {
                    _wagerChipTexts[i].resizeTextForBestFit = true;
                    _wagerChipTexts[i].resizeTextMinSize = 10;
                    _wagerChipTexts[i].resizeTextMaxSize = 13;
                    AddTextShadow(_wagerChipTexts[i].gameObject);
                }
            }

            _wagerBonusInfoText = CreateText("WagerBonusTxt", wagerDeck, "", 13, FontStyle.Italic, new Color(0.68f, 0.94f, 0.78f), TextAnchor.MiddleCenter);
            _wagerBonusInfoText.resizeTextForBestFit = true;
            _wagerBonusInfoText.resizeTextMinSize = 10;
            _wagerBonusInfoText.resizeTextMaxSize = 13;
            _wagerBonusInfoText.rectTransform.anchoredPosition = new Vector2(0f, -38f);
            _wagerBonusInfoText.rectTransform.sizeDelta = new Vector2(844f, 24f);

            // Beveled Primary Action Buttons
            var startBtn = CreateButton("StartMatchBtn", window, "", new Color(0.16f, 0.72f, 0.32f), 19, () =>
            {
                CloseCardPickerModal();
                _wagerModal.SetActive(false);
                TeksMatchManager.Instance?.BeginFlickMatch(_selectedWagerAmount);
            });
            var stRt = startBtn.GetComponent<RectTransform>();
            stRt.anchoredPosition = new Vector2(-102f, -292f);
            stRt.sizeDelta = new Vector2(300f, 48f);
            ApplyButtonTint(startBtn, new Color(0.16f, 0.72f, 0.32f));
            AddOutline(startBtn.gameObject, new Color(0.56f, 1.0f, 0.66f), 2.0f);
            var stGloss = CreateRoundedPanel("StartGloss", stRt, new Color(1f, 1f, 1f, 0.14f), Vector2.zero);
            stGloss.anchorMin = new Vector2(0.03f, 0.58f);
            stGloss.anchorMax = new Vector2(0.97f, 0.94f);
            stGloss.offsetMin = Vector2.zero;
            stGloss.offsetMax = Vector2.zero;
            stGloss.GetComponent<Image>().raycastTarget = false;
            var stLbl = CreateText("Label", stRt, "START FLICK MATCH!", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(stLbl.rectTransform, 4f);
            AddTextShadow(stLbl.gameObject);

            var cancelBtn = CreateButton("CancelWagerBtn", window, "", new Color(0.56f, 0.20f, 0.24f), 17, () =>
            {
                CloseCardPickerModal();
                _wagerModal.SetActive(false);
                TeksMatchManager.Instance?.CancelChallenge();
            });
            var caRt = cancelBtn.GetComponent<RectTransform>();
            caRt.anchoredPosition = new Vector2(164f, -292f);
            caRt.sizeDelta = new Vector2(172f, 48f);
            ApplyButtonTint(cancelBtn, new Color(0.56f, 0.20f, 0.24f));
            AddOutline(cancelBtn.gameObject, new Color(0.96f, 0.52f, 0.54f), 1.5f);
            var caLbl = CreateText("Label", caRt, "CANCEL", 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(caLbl.rectTransform, 4f);
            AddTextShadow(caLbl.gameObject);

            _wagerModal.SetActive(false);
        }

        private void AttachWagerCardBadges(RectTransform slab, out Text levelBadgeText, out Text starsText)
        {
            var lvlBadge = CreateRoundedPanel("LevelBadge", slab, new Color(0.05f, 0.08f, 0.14f, 0.92f), new Vector2(40f, 18f));
            lvlBadge.anchorMin = new Vector2(1f, 1f);
            lvlBadge.anchorMax = new Vector2(1f, 1f);
            lvlBadge.pivot = new Vector2(1f, 1f);
            lvlBadge.anchoredPosition = new Vector2(-4f, -4f);
            lvlBadge.GetComponent<Image>().raycastTarget = false;
            AddOutline(lvlBadge.gameObject, new Color(1.0f, 0.84f, 0.24f), 1f);
            levelBadgeText = CreateText("LvlTxt", lvlBadge, "Lv.1", 10, FontStyle.Bold, new Color(1.0f, 0.88f, 0.32f), TextAnchor.MiddleCenter);
            StretchFull(levelBadgeText.rectTransform, 1f);

            var starScrim = CreateRoundedPanel("StarScrim", slab, new Color(0.03f, 0.05f, 0.10f, 0.88f), Vector2.zero);
            starScrim.anchorMin = new Vector2(0f, 0f);
            starScrim.anchorMax = new Vector2(1f, 0f);
            starScrim.offsetMin = new Vector2(3.5f, 3.5f);
            starScrim.offsetMax = new Vector2(-3.5f, 21f);
            starScrim.GetComponent<Image>().raycastTarget = false;
            starsText = CreateText("StarsTxt", starScrim, "★☆☆☆☆", 11, FontStyle.Bold, new Color(1.0f, 0.86f, 0.24f), TextAnchor.MiddleCenter);
            StretchFull(starsText.rectTransform, 1f);
        }

        private Text CreateWagerStatPill(string name, Transform parent, Color accentColor, string initialText)
        {
            var pill = CreateRoundedPanel(name, parent, new Color(0.05f, 0.09f, 0.16f, 0.98f), new Vector2(280f, 44f));
            pill.GetComponent<Image>().raycastTarget = false;
            AddOutline(pill.gameObject, new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f), 1.4f);

            var accent = CreateRoundedPanel("Accent", pill, accentColor, Vector2.zero);
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.offsetMin = new Vector2(4f, 6f);
            accent.offsetMax = new Vector2(8f, -6f);
            accent.GetComponent<Image>().raycastTarget = false;

            var txt = CreateText("StatTxt", pill, initialText, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            txt.supportRichText = true;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 10;
            txt.resizeTextMaxSize = 13;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(14f, 2f);
            txt.rectTransform.offsetMax = new Vector2(-8f, -2f);
            AddTextShadow(txt.gameObject);
            return txt;
        }

        private void CreateCardPickerModal()
        {
            _cardPickerModal = CreateModalBackdrop("CardPickerModal", new Vector2(820f, 520f), out RectTransform window);
            AddOutline(window.gameObject, new Color(0.42f, 0.72f, 1.0f), 2.5f);

            var header = CreateText("CardPickerTitle", window, "CHOOSE YOUR PAMATO CARD", 24, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.80f, 1f);
            header.rectTransform.offsetMin = new Vector2(24f, -46f);
            header.rectTransform.offsetMax = new Vector2(0f, -10f);

            _cardPickerSubtitleText = CreateText("CardPickerSubtitle", window, "Allowed Cards: 0", 13, FontStyle.Bold, new Color(0.76f, 0.90f, 1.0f), TextAnchor.MiddleLeft);
            _cardPickerSubtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _cardPickerSubtitleText.rectTransform.anchorMax = new Vector2(0.80f, 1f);
            _cardPickerSubtitleText.rectTransform.offsetMin = new Vector2(24f, -74f);
            _cardPickerSubtitleText.rectTransform.offsetMax = new Vector2(0f, -46f);

            var closeBtn = CreateButton("CloseCardPickerBtn", window, "CLOSE", new Color(0.84f, 0.20f, 0.20f), 15, CloseCardPickerModal);
            var clRt = closeBtn.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1f, 1f);
            clRt.anchorMax = new Vector2(1f, 1f);
            clRt.anchoredPosition = new Vector2(-68f, -38f);
            clRt.sizeDelta = new Vector2(96f, 36f);

            CreateScrollView("CardPickerScroll", window, new Vector2(20f, 20f), new Vector2(-20f, -84f), out _cardPickerGridContent);
            var grid = _cardPickerGridContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180f, 198f);
            grid.spacing = new Vector2(10f, 12f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = _cardPickerGridContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _cardPickerModal.SetActive(false);
        }

        private void OpenCardPickerModal()
        {
            if (_cardPickerModal == null) return;
            PopulateCardPickerModal();
            _cardPickerModal.transform.SetAsLastSibling();
            if (_toastPanel != null) _toastPanel.transform.SetAsLastSibling();
            _cardPickerModal.SetActive(true);
        }

        private void CloseCardPickerModal()
        {
            if (_cardPickerModal != null) _cardPickerModal.SetActive(false);
        }

        private void PopulateCardPickerModal()
        {
            var mm = TeksMatchManager.Instance;
            var inv = PlayerInventory.Instance;
            if (inv == null || _cardPickerGridContent == null) return;

            for (int i = _cardPickerGridContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardPickerGridContent.GetChild(i).gameObject);
            }

            IReadOnlyList<OwnedTeksCard> allowed = mm != null ? mm.GetAllowedPamatoCards() : inv.OwnedCards;
            int allowedCount = allowed != null ? allowed.Count : 0;

            if (_cardPickerSubtitleText != null)
            {
                if (mm != null && mm.HasCardRestriction && !string.IsNullOrEmpty(mm.CardRestrictionLabel))
                {
                    _cardPickerSubtitleText.text = $"Allowed Cards: {allowedCount}   •   Restriction: {mm.CardRestrictionLabel}";
                }
                else
                {
                    _cardPickerSubtitleText.text = $"Allowed Cards: {allowedCount}   •   Click any card to equip as your Pamato";
                }
            }

            if (allowed == null || allowed.Count == 0) return;

            var activePamato = mm != null ? mm.GetActivePlayerPamato(out _) : inv.GetEquippedPamato(out _);
            string equippedId = activePamato != null ? activePamato.CardId : inv.EquippedPamatoId;

            for (int i = 0; i < allowed.Count; i++)
            {
                var owned = allowed[i];
                if (owned == null) continue;
                var def = TeksDatabase.GetCard(owned.CardId);
                if (def == null) continue;

                bool isEquipped = owned.CardId == equippedId;
                Color rarityCol = TeksDatabase.GetRarityColor(def.Rarity);
                int chubPct = Mathf.RoundToInt(TeksMatchManager.CalculatePlayerChubProbability(def.Rarity, owned.StarRank) * 100f);

                var tile = CreateRoundedPanel(
                    "PickCard_" + def.Id,
                    _cardPickerGridContent,
                    isEquipped ? new Color(0.14f, 0.24f, 0.34f, 1f) : new Color(0.11f, 0.16f, 0.25f, 1f),
                    new Vector2(180f, 198f)
                );
                AddOutline(tile.gameObject, isEquipped ? new Color(1.0f, 0.85f, 0.22f) : rarityCol, isEquipped ? 3f : 2f);

                string capturedCardId = owned.CardId;
                var tileBtn = tile.gameObject.AddComponent<Button>();
                tileBtn.onClick.AddListener(() =>
                {
                    TeksAudioManager.Instance?.PlayUiClick();
                    var curMm = TeksMatchManager.Instance;
                    var curInv = PlayerInventory.Instance;
                    if (curMm != null)
                    {
                        curMm.TrySelectPamatoCard(capturedCardId, out string msg);
                        if (!string.IsNullOrEmpty(msg)) ShowToast(msg);
                    }
                    else if (curInv != null)
                    {
                        curInv.EquipPamato(capturedCardId);
                    }
                    CloseCardPickerModal();
                    PopulateWagerModal();
                });

                // Card Portrait Frame
                var artFrame = CreateRoundedPanel("ArtFrame", tile, rarityCol, new Vector2(72f, 96f));
                artFrame.anchorMin = new Vector2(0.5f, 1f);
                artFrame.anchorMax = new Vector2(0.5f, 1f);
                artFrame.anchoredPosition = new Vector2(0f, -58f);
                artFrame.GetComponent<Image>().raycastTarget = false;

                var artImg = CreateImage("Art", artFrame, CardArtGenerator.GetCardPortraitSprite(def, owned.StarRank));
                StretchFull(artImg.rectTransform, 3f);

                if (isEquipped)
                {
                    var eqBadge = CreateRoundedPanel("EquippedBadge", tile, new Color(0.14f, 0.72f, 0.32f, 0.96f), new Vector2(72f, 20f));
                    eqBadge.anchorMin = new Vector2(1f, 1f);
                    eqBadge.anchorMax = new Vector2(1f, 1f);
                    eqBadge.anchoredPosition = new Vector2(-42f, -15f);
                    eqBadge.GetComponent<Image>().raycastTarget = false;
                    AddOutline(eqBadge.gameObject, new Color(1.0f, 0.88f, 0.32f), 1f);

                    var eqTxt = CreateText("EqTxt", eqBadge, "EQUIPPED", 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                    StretchFull(eqTxt.rectTransform, 1f);
                }

                // Card Name
                var nameTxt = CreateText("Name", tile, def.Name, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                nameTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                nameTxt.rectTransform.anchorMax = new Vector2(1f, 0f);
                nameTxt.rectTransform.offsetMin = new Vector2(6f, 56f);
                nameTxt.rectTransform.offsetMax = new Vector2(-6f, 84f);

                // Rarity + Level + Fusion Stage
                string fusionStr = owned.FusionStage > 0 ? $" • F{owned.FusionStage}" : string.Empty;
                var metaTxt = CreateText("Meta", tile, $"{TeksDatabase.GetRarityName(def.Rarity)}  •  Lv.{owned.StarRank}{fusionStr}", 11, FontStyle.Bold, rarityCol, TextAnchor.MiddleCenter);
                metaTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                metaTxt.rectTransform.anchorMax = new Vector2(1f, 0f);
                metaTxt.rectTransform.offsetMin = new Vector2(6f, 34f);
                metaTxt.rectTransform.offsetMax = new Vector2(-6f, 56f);

                // Live CHUB% Pill
                var chubPill = CreateRoundedPanel("ChubPill", tile, new Color(0.07f, 0.11f, 0.18f, 0.96f), new Vector2(146f, 24f));
                chubPill.anchorMin = new Vector2(0.5f, 0f);
                chubPill.anchorMax = new Vector2(0.5f, 0f);
                chubPill.anchoredPosition = new Vector2(0f, 18f);
                chubPill.GetComponent<Image>().raycastTarget = false;

                var chubTxt = CreateText("ChubTxt", chubPill, $"{chubPct}% CHUB", 12, FontStyle.Bold, new Color(0.45f, 0.98f, 0.58f), TextAnchor.MiddleCenter);
                StretchFull(chubTxt.rectTransform, 2f);
            }

            var contentRt = _cardPickerGridContent as RectTransform;
            if (contentRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            }
        }

        private void CreateSellConfirmModal()
        {
            _sellConfirmModal = CreateModalBackdrop("SellConfirmModal", new Vector2(540f, 305f), out RectTransform window);
            AddOutline(window.gameObject, new Color(0.95f, 0.54f, 0.18f), 2.5f);

            _sellConfirmTitleText = CreateText("SellConfirmTitle", window, "CONFIRM CARD SALE", 24, FontStyle.Bold, new Color(1.0f, 0.84f, 0.28f), TextAnchor.MiddleCenter);
            _sellConfirmTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _sellConfirmTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _sellConfirmTitleText.rectTransform.offsetMin = new Vector2(20f, -54f);
            _sellConfirmTitleText.rectTransform.offsetMax = new Vector2(-20f, -14f);

            _sellConfirmCardInfoText = CreateText("SellConfirmInfo", window, "Sell Card for +0 Coins?", 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _sellConfirmCardInfoText.rectTransform.anchorMin = new Vector2(0f, 0.50f);
            _sellConfirmCardInfoText.rectTransform.anchorMax = new Vector2(1f, 0.80f);
            _sellConfirmCardInfoText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _sellConfirmCardInfoText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            _sellConfirmWarningText = CreateText("SellConfirmWarning", window, "", 15, FontStyle.Bold, new Color(1.0f, 0.36f, 0.32f), TextAnchor.MiddleCenter);
            _sellConfirmWarningText.rectTransform.anchorMin = new Vector2(0f, 0.26f);
            _sellConfirmWarningText.rectTransform.anchorMax = new Vector2(1f, 0.52f);
            _sellConfirmWarningText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _sellConfirmWarningText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            var confirmBtn = CreateButton("ConfirmSellBtn", window, "CONFIRM SELL", new Color(0.84f, 0.24f, 0.20f), 17, OnConfirmSellCardClicked);
            var cfRt = confirmBtn.GetComponent<RectTransform>();
            cfRt.anchorMin = new Vector2(0.5f, 0f);
            cfRt.anchorMax = new Vector2(0.5f, 0f);
            cfRt.anchoredPosition = new Vector2(-106f, 44f);
            cfRt.sizeDelta = new Vector2(190f, 44f);

            var cancelBtn = CreateButton("CancelSellBtn", window, "CANCEL", new Color(0.28f, 0.36f, 0.50f), 17, CloseSellConfirmModal);
            var cnRt = cancelBtn.GetComponent<RectTransform>();
            cnRt.anchorMin = new Vector2(0.5f, 0f);
            cnRt.anchorMax = new Vector2(0.5f, 0f);
            cnRt.anchoredPosition = new Vector2(106f, 44f);
            cnRt.sizeDelta = new Vector2(160f, 44f);

            _sellConfirmModal.SetActive(false);
        }

        private void OpenSellConfirmModal(string cardId)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _sellConfirmModal == null) return;

            if (!inv.CanSellCard(cardId, out string reason))
            {
                ShowToast(reason);
                return;
            }

            var owned = inv.GetOwnedCard(cardId);
            var def = TeksDatabase.GetCard(cardId);
            if (owned == null || def == null) return;

            _pendingSellCardId = cardId;
            if (_sellConfirmTitleText != null) _sellConfirmTitleText.text = "CONFIRM CARD SALE";
            if (_sellConfirmCardInfoText != null)
            {
                _sellConfirmCardInfoText.text = $"{def.Name}  [{TeksDatabase.GetRarityName(def.Rarity)} • Lv.{owned.Level}]\nPayout: +{def.SellPrice} Coins";
            }
            if (_sellConfirmWarningText != null)
            {
                if (owned.Quantity <= 1)
                {
                    string extra = inv.EquippedPamatoId == cardId
                        ? "\nEquipped Pamato will auto-switch to another card."
                        : string.Empty;
                    _sellConfirmWarningText.color = new Color(1.0f, 0.36f, 0.32f);
                    _sellConfirmWarningText.text = "WARNING: This is your ONLY copy of this card!" + extra;
                }
                else
                {
                    _sellConfirmWarningText.color = new Color(0.80f, 0.90f, 1.0f);
                    _sellConfirmWarningText.text = $"You own x{owned.Quantity} copies. You will keep {owned.Quantity - 1} copy/copies.";
                }
            }

            _sellConfirmModal.transform.SetAsLastSibling();
            if (_toastPanel != null) _toastPanel.transform.SetAsLastSibling();
            _sellConfirmModal.SetActive(true);
        }

        private void CloseSellConfirmModal()
        {
            _pendingSellCardId = null;
            if (_sellConfirmModal != null) _sellConfirmModal.SetActive(false);
        }

        private void OnConfirmSellCardClicked()
        {
            var inv = PlayerInventory.Instance;
            string cardId = _pendingSellCardId;
            CloseSellConfirmModal();
            if (inv == null || string.IsNullOrEmpty(cardId)) return;

            var def = TeksDatabase.GetCard(cardId);
            string cardName = def != null ? def.Name : cardId;

            if (inv.TrySellCard(cardId, out int earned, out bool removedLastCopy, out string autoEquippedCardName))
            {
                TeksAudioManager.Instance?.PlayCoinClink();
                if (removedLastCopy && !string.IsNullOrEmpty(autoEquippedCardName))
                {
                    ShowToast($"Sold {cardName} for +{earned} Coins! Auto-equipped {autoEquippedCardName}.");
                }
                else
                {
                    ShowToast($"Sold 1x {cardName} for +{earned} Coins!");
                }
                RefreshAllUI();
            }
            else
            {
                inv.CanSellCard(cardId, out string reason);
                ShowToast(string.IsNullOrEmpty(reason) ? "Cannot sell this card!" : reason);
            }
        }

        private string GetDisplayPlayerName()
        {
            return PlayerInventory.Instance != null && !string.IsNullOrWhiteSpace(PlayerInventory.Instance.PlayerNickname)
                ? PlayerInventory.Instance.PlayerNickname
                : "Player";
        }

        private int GetCurrentPlayerChubPercent()
        {
            var mm = TeksMatchManager.Instance;
            var inv = PlayerInventory.Instance;
            TeksCardDefinition pDef = null;
            OwnedTeksCard pOwned = mm != null
                ? mm.GetActivePlayerPamato(out pDef)
                : (inv != null ? inv.GetEquippedPamato(out pDef) : null);
            if (pOwned == null || pDef == null) return 50;
            return Mathf.RoundToInt(TeksMatchManager.CalculatePlayerChubProbability(pDef.Rarity, pOwned.StarRank) * 100f);
        }

        public void OpenTournamentModal()
        {
            OpenModesModalOnTab(1);
        }

        public void OpenModesModalOnTab(int tabIndex)
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            CloseSellConfirmModal();
            CloseAllModals();
            _activeModesTabIndex = Mathf.Clamp(tabIndex, 0, 2);
            PopulateModesModal();
            if (_modesModal != null) _modesModal.SetActive(true);
            SyncPlayerMovementLock();
        }

        public void RegisterAvatarProfileClickHandler(Action handler)
        {
            _avatarProfileClickHandler = handler;
        }

        private void OnAvatarPortraitClicked()
        {
            TeksAudioManager.Instance?.PlayUiClick();
            if (_avatarProfileClickHandler != null)
            {
                _avatarProfileClickHandler.Invoke();
            }
            else
            {
                ToggleInventoryModal();
            }
        }

        private void CreateToastBanner()
        {
            _toastPanel = CreateRoundedPanel("ToastBanner", _canvas.transform, new Color(0.06f, 0.10f, 0.17f, 0.96f), new Vector2(680f, 50f)).gameObject;
            var rt = _toastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -94f);
            AddOutline(_toastPanel, new Color(0.95f, 0.78f, 0.24f), 2f);

            _toastText = CreateText("ToastLabel", rt, "", 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _toastText.lineSpacing = 1.15f;
            StretchFull(_toastText.rectTransform, 10f);
            AddTextShadow(_toastText.gameObject);
            _toastPanel.SetActive(false);
        }

        private void HookEvents()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshAllUI;
                PlayerInventory.Instance.OnInventoryChanged += RefreshAllUI;
                PlayerInventory.Instance.OnCharacterTypeChanged -= HandleCharacterTypeChanged;
                PlayerInventory.Instance.OnCharacterTypeChanged += HandleCharacterTypeChanged;
            }
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopUpdated -= RefreshShopDisplay;
                ShopManager.Instance.OnShopUpdated += RefreshShopDisplay;
            }
            if (TeksMatchManager.Instance != null)
            {
                TeksMatchManager.Instance.OnMatchStateChanged -= HandleMatchStateChanged;
                TeksMatchManager.Instance.OnMatchStateChanged += HandleMatchStateChanged;
            }
            HookExtendedEvents();
        }

        private void HandleCharacterTypeChanged(PlayerCharacterType _)
        {
            RefreshAllUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshAllUI;
                PlayerInventory.Instance.OnCharacterTypeChanged -= HandleCharacterTypeChanged;
            }
            if (ShopManager.Instance != null) ShopManager.Instance.OnShopUpdated -= RefreshShopDisplay;
            if (TeksMatchManager.Instance != null) TeksMatchManager.Instance.OnMatchStateChanged -= HandleMatchStateChanged;
            UnhookExtendedEvents();
        }

        private void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f && _toastPanel != null) _toastPanel.SetActive(false);
            }

            if (ShopManager.Instance != null)
            {
                string fmt = ShopManager.Instance.GetFormattedTimer();
                if (_shopTimerText != null) _shopTimerText.text = $"5M REFRESH  |  {fmt}";
                if (_shopModalTimerText != null && _shopModal.activeSelf) _shopModalTimerText.text = $"NEW CARDS IN {fmt}";
            }

            if (PlayerController.Instance != null && _zonePillText != null)
            {
                var areaId = BarangayCourtBuilder.GetAreaAtPosition(PlayerController.Instance.transform.position);
                string currentZone = BarangayCourtBuilder.GetAreaDisplayName(areaId).ToUpper();
                if (currentZone != _lastDisplayedZone)
                {
                    _lastDisplayedZone = currentZone;
                    _zonePillText.text = $"AREA: {currentZone}";
                }
            }

            if (_sellConfirmModal != null && _sellConfirmModal.activeSelf && (_inventoryModal == null || !_inventoryModal.activeSelf))
            {
                CloseSellConfirmModal();
            }
            if (_cardPickerModal != null && _cardPickerModal.activeSelf && (_wagerModal == null || !_wagerModal.activeSelf))
            {
                CloseCardPickerModal();
            }

            UpdateMatchLiveUI();
            UpdateNpcFloatingTags();
            UpdateProximityPrompt();
            HandleGlobalHotkeys();
        }

        private void HandleGlobalHotkeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (_cardPickerModal != null && _cardPickerModal.activeSelf)
                {
                    CloseCardPickerModal();
                    return;
                }
                if (_sellConfirmModal != null && _sellConfirmModal.activeSelf)
                {
                    CloseSellConfirmModal();
                    return;
                }
                HandleEscapePressed();
                return;
            }

            if (_startScreenRoot != null && _startScreenRoot.activeSelf) return;

            var matchState = TeksMatchManager.Instance != null ? TeksMatchManager.Instance.State : MatchState.Idle;
            if (matchState != MatchState.Idle) return;

            if (_pauseModal != null && _pauseModal.activeSelf) return;

            if (kb.iKey.wasPressedThisFrame) ToggleInventoryModal();
            if (kb.fKey.wasPressedThisFrame) OpenFusionLabDirect();
            if (kb.qKey.wasPressedThisFrame) ToggleQuestsModal();
            if (kb.pKey.wasPressedThisFrame) ToggleShopModal();
            if (kb.gKey.wasPressedThisFrame) ToggleModesModal();
            if (kb.uKey.wasPressedThisFrame) ToggleProfileModal();

            if (kb.eKey.wasPressedThisFrame && !IsAnyModalOpen())
            {
                var pc = PlayerController.Instance;
                if (pc != null)
                {
                    pc.ExternalInteractHandlerActive = true;
                    if (pc.NearestHiddenCard != null && pc.NearestHiddenCard.CanCollect)
                    {
                        pc.TryInteractNearestHiddenCard();
                    }
                    else if (pc.NearestOpponent != null)
                    {
                        TeksMatchManager.Instance?.StartChallenge(pc.NearestOpponent);
                    }
                    else if (pc.IsNearSariSariStore)
                    {
                        ToggleShopModal();
                    }
                    else if (pc.IsNearTournamentBooth)
                    {
                        OpenTournamentModal();
                    }
                    else if (pc.NearestLockedGate != null && pc.NearestLockedGate.IsLocked)
                    {
                        pc.TryInteractNearestLockedGate();
                    }
                }
            }
        }

        private void UpdateMatchLiveUI()
        {
            var mm = TeksMatchManager.Instance;
            if (mm == null) return;

            if (mm.State == MatchState.ReadyToFlick && _flickGauge != null)
            {
                _flickGauge.SetSweetSpotHalfWidth(mm.SweetSpotHalfWidth);
                _flickGauge.SetNeedleValue(mm.NeedleValue);
            }
            else if (mm.State == MatchState.FlickInAir || mm.State == MatchState.RoundComplete)
            {
                UpdateFlipCardWidgets(mm);
            }
        }

        private void PrepareFlipShowcase(TeksMatchManager mm, bool showImmediately = false)
        {
            if (_cardFlipShowcaseRoot == null) return;

            var inv = PlayerInventory.Instance;
            TeksCardDefinition pDef = null;
            var pOwned = mm != null ? mm.GetActivePlayerPamato(out pDef) : (inv != null ? inv.GetEquippedPamato(out pDef) : null);
            int pStars = pOwned != null ? pOwned.StarRank : 5;
            string pCardId = pOwned != null ? pOwned.CardId : null;

            string oppCardId = mm != null && mm.ActiveOpponent != null
                ? mm.GetResolvedOpponentCardId(mm.ActiveOpponent, pCardId)
                : "bakunawa";
            int oStars = mm != null && mm.ActiveOpponent != null ? mm.ActiveOpponent.CardStarRank : 3;
            var oDef = TeksDatabase.GetCard(oppCardId);

            SetupFlipWidget(0, GetDisplayPlayerName().ToUpper(), pDef, pStars, 16.8f);
            SetupFlipWidget(1, mm != null && mm.ActiveOpponent != null ? mm.ActiveOpponent.NpcName.ToUpper() : "OPPONENT", oDef, oStars, 18.2f);

            var showcaseImg = _cardFlipShowcaseRoot.GetComponent<Image>();
            if (showcaseImg != null)
            {
                showcaseImg.color = new Color(0.06f, 0.09f, 0.16f, 0.56f);
            }
            _cardFlipShowcaseRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 105f);
            _cardFlipShowcaseRoot.SetActive(showImmediately);
        }

        private void SetupFlipWidget(int idx, string role, TeksCardDefinition def, int stars, float spinSpeed)
        {
            var w = _flipWidgets[idx];
            if (w == null) return;

            w.RoleText.text = role;
            w.FrontSprite = def != null ? CardArtGenerator.GetCardPortraitSprite(def, stars) : null;
            w.BackSprite = CardArtGenerator.GetCardBackSprite();
            w.RarityColor = def != null ? TeksDatabase.GetRarityColor(def.Rarity) : Color.white;
            w.CardNameText.text = def != null ? def.Name : "TEKS";
            w.StarsText.text = new string('★', Mathf.Clamp(stars, 1, 5));
            w.FlipAngle = idx * 1.1f;
            w.SpinSpeed = spinSpeed;
            w.SettledPopTimer = -1f;
            w.WasSettled = false;

            w.CardTransform.localScale = Vector3.one;
            w.CardTransform.anchoredPosition = new Vector2(0f, w.BaseCardY);
            w.WinnerTag.SetActive(false);
            w.StatusPillBg.color = new Color(0.14f, 0.20f, 0.30f, 0.96f);
            w.StatusPillText.text = "FLIPPING...";
        }

        private void UpdateFlipCardWidgets(TeksMatchManager mm)
        {
            if (_cardFlipShowcaseRoot == null || !_cardFlipShowcaseRoot.activeSelf) return;

            TeksCardPhysics[] physCards = { mm.PlayerPhysicalCard, mm.OpponentPhysicalCard };
            bool[] fallbackFaceUp = { mm.PlayerCardFaceUp, mm.OpponentCardFaceUp };

            for (int i = 0; i < 2; i++)
            {
                var w = _flipWidgets[i];
                if (w == null) continue;

                var phys = physCards[i];
                bool settled = mm.State == MatchState.RoundComplete || (phys != null && phys.HasSettled);

                if (!settled)
                {
                    w.FlipAngle += Time.deltaTime * w.SpinSpeed;
                    float cos = Mathf.Cos(w.FlipAngle);
                    float absCos = Mathf.Max(0.08f, Mathf.Abs(cos));
                    bool showFront = cos >= 0f;

                    // Parabolic arc lift during air flight
                    float arc = Mathf.Abs(Mathf.Sin(mm.FlickAirElapsed * 2.6f + i * 0.4f)) * 28f;
                    w.CardTransform.anchoredPosition = new Vector2(0f, w.BaseCardY + arc);
                    w.CardTransform.localScale = new Vector3(absCos, 1.0f + arc * 0.004f, 1f);
                    w.CardTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(w.FlipAngle * 0.5f) * 8f);

                    ApplyWidgetFace(w, showFront);
                }
                else
                {
                    bool finalFaceUp = phys != null ? phys.TargetFaceUp : fallbackFaceUp[i];

                    if (!w.WasSettled)
                    {
                        w.WasSettled = true;
                        w.SettledPopTimer = 0f;
                    }

                    float popScale = 1f;
                    if (w.SettledPopTimer >= 0f)
                    {
                        w.SettledPopTimer += Time.deltaTime;
                        float t = Mathf.Clamp01(w.SettledPopTimer / 0.22f);
                        popScale = Mathf.Lerp(1.24f, 1.0f, t);
                        if (t >= 1f) w.SettledPopTimer = -1f;
                    }

                    w.CardTransform.anchoredPosition = new Vector2(0f, w.BaseCardY);
                    w.CardTransform.localRotation = Quaternion.identity;
                    w.CardTransform.localScale = Vector3.one * popScale;

                    ApplyWidgetFace(w, finalFaceUp);

                    if (finalFaceUp)
                    {
                        w.StatusPillBg.color = new Color(0.14f, 0.72f, 0.34f, 0.98f);
                        w.StatusPillText.text = "CHUB (FACE-UP)";
                    }
                    else
                    {
                        w.StatusPillBg.color = new Color(0.76f, 0.22f, 0.22f, 0.98f);
                        w.StatusPillText.text = "CHA (FACE-DOWN)";
                    }

                    if (mm.State == MatchState.RoundComplete && !mm.WasTablaLastRound)
                    {
                        bool isWinnerCard = (i == 0 && mm.PlayerWonLastRound) || (i == 1 && !mm.PlayerWonLastRound);
                        w.WinnerTag.SetActive(isWinnerCard);
                    }
                }
            }
        }

        private static void ApplyWidgetFace(FlipCardWidget w, bool showFront)
        {
            if (showFront)
            {
                w.FaceImage.sprite = w.FrontSprite;
                w.FrameImage.color = w.RarityColor;
                w.FrontOverlay.SetActive(true);
            }
            else
            {
                w.FaceImage.sprite = w.BackSprite;
                w.FrameImage.color = new Color(0.45f, 0.30f, 0.16f);
                w.FrontOverlay.SetActive(false);
            }
        }

        private void UpdateNpcFloatingTags()
        {
            var cam = Camera.main;
            bool showTags = cam != null && (TeksMatchManager.Instance == null || TeksMatchManager.Instance.State == MatchState.Idle) && !IsAnyModalOpen();

            for (int i = 0; i < _npcTags.Count; i++)
            {
                var entry = _npcTags[i];
                if (entry.Root == null || (entry.Npc == null && entry.TargetTransform == null)) continue;

                if (!showTags)
                {
                    entry.Root.gameObject.SetActive(false);
                    continue;
                }

                Vector3 worldPos = entry.Npc != null
                    ? entry.Npc.GetTagWorldPosition()
                    : entry.TargetTransform.position + Vector3.up * entry.VerticalOffset;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                bool visible = screenPos.z > 0.2f && screenPos.x > -50f && screenPos.x < Screen.width + 50f && screenPos.y > -50f && screenPos.y < Screen.height + 50f;
                entry.Root.gameObject.SetActive(visible);

                if (visible)
                {
                    entry.Root.position = new Vector3(screenPos.x, screenPos.y, 0f);
                    if (entry.Npc != null)
                    {
                        if (entry.Label != null)
                        {
                            string dynamicTag = entry.Npc.GetDisplayTag();
                            if (entry.Label.text != dynamicTag)
                            {
                                entry.Label.text = dynamicTag;
                            }
                        }
                        if (entry.SpeechBubble != null && entry.SpeechLabel != null)
                        {
                            bool showSpeech = entry.Npc.IsPlayerWithin(7.2f);
                            entry.SpeechBubble.SetActive(showSpeech);
                            if (showSpeech)
                            {
                                entry.SpeechLabel.text = "\"" + entry.Npc.GetActiveCallout() + "\"";
                            }
                        }
                    }
                    else if (entry.Label != null && !string.IsNullOrEmpty(entry.StaticLabel) && entry.Label.text != entry.StaticLabel)
                    {
                        entry.Label.text = entry.StaticLabel;
                    }
                }
            }
        }

        private void UpdateProximityPrompt()
        {
            if (_proximityPromptPanel == null) return;
            var pc = PlayerController.Instance;
            if (pc != null) pc.ExternalInteractHandlerActive = true;

            bool idle = TeksMatchManager.Instance == null || TeksMatchManager.Instance.State == MatchState.Idle;

            if (!idle || pc == null || IsAnyModalOpen())
            {
                _proximityPromptPanel.SetActive(false);
                return;
            }

            if (pc.NearestHiddenCard != null && pc.NearestHiddenCard.CanCollect)
            {
                _proximityPromptPanel.SetActive(true);
                _proximityPromptText.text = pc.NearestHiddenCard.GetPromptText();
            }
            else if (pc.NearestOpponent != null)
            {
                _proximityPromptPanel.SetActive(true);
                int reward = TeksMatchManager.GetBaseCoinReward(pc.NearestOpponent);
                _proximityPromptText.text = $"[E] CHALLENGE {pc.NearestOpponent.GetDisplayTag().ToUpper()} (WIN +{reward} COINS)";
            }
            else if (pc.IsNearSariSariStore)
            {
                _proximityPromptPanel.SetActive(true);
                _proximityPromptText.text = "[E] OPEN SARI-SARI STORE (BUY TEKS & PACKS)";
            }
            else if (pc.IsNearTournamentBooth)
            {
                _proximityPromptPanel.SetActive(true);
                _proximityPromptText.text = $"[E] TOURNAMENT ARENA — {TeksGameModeManager.TournamentRoundCount}-ROUND BRACKET (FEE: {TeksGameModeManager.TournamentEntryFee} COINS | PRIZE: {TeksGameModeManager.TournamentTrophyCoins} COINS + TROPHY)";
            }
            else if (pc.NearestLockedGate != null && pc.NearestLockedGate.IsLocked)
            {
                _proximityPromptPanel.SetActive(true);
                _proximityPromptText.text = pc.NearestLockedGate.GetPromptText();
            }
            else
            {
                _proximityPromptPanel.SetActive(false);
            }
        }

        private void HandleMatchStateChanged()
        {
            var mm = TeksMatchManager.Instance;
            if (mm == null) return;

            if (mm.State == MatchState.Idle)
            {
                CloseCardPickerModal();
                _wagerModal.SetActive(false);
                _matchHudRoot.SetActive(false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);
                SyncPlayerMovementLock();
                RefreshAllUI();
                return;
            }

            if (mm.State == MatchState.WagerScreen)
            {
                CloseCardPickerModal();
                _matchHudRoot.SetActive(false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);
                _selectedWagerAmount = mm.CurrentCoinWager;
                PopulateWagerModal();
                _wagerModal.SetActive(true);
                SyncPlayerMovementLock();
                return;
            }

            CloseCardPickerModal();
            _wagerModal.SetActive(false);
            _matchHudRoot.SetActive(true);
            SyncPlayerMovementLock();

            string oppLabel = mm.ActiveOpponent != null ? mm.ActiveOpponent.GetDisplayTag() : "Opponent";

            if (mm.State == MatchState.ReadyToFlick)
            {
                if (_matchBannerRoot != null) _matchBannerRoot.SetActive(true);
                _flickGauge.gameObject.SetActive(true);
                _flickGauge.SetInteractive(true, "TAP [SPACE] TO FLICK!");
                _flickGauge.SetSweetSpotHalfWidth(mm.SweetSpotHalfWidth);
                _roundResultPanel.SetActive(false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);

                string modePrefix = (TeksGameModeManager.Instance != null && TeksGameModeManager.Instance.IsSpecialModeActive)
                    ? TeksGameModeManager.Instance.GetModeStatusBanner() + "  |  "
                    : string.Empty;
                int wager = mm.CurrentCoinWager;
                string wagerLabel = wager > 0
                    ? $"WIN +{wager:N0}c / LOSE -{wager:N0}c"
                    : $"WIN +{TeksMatchManager.GetBaseCoinReward(mm.ActiveOpponent):N0}c (FREE)";
                int chubPct = GetCurrentPlayerChubPercent();
                int chaPct = 100 - chubPct;
                _matchBannerTitle.text = $"{modePrefix}VS {oppLabel.ToUpper()}  |  {wagerLabel}";
                _matchBannerSub.text = $"Press SPACE or click inside the GREEN Sweet Spot for power & spin! ({chubPct}% CHUB / {chaPct}% CHA)";
            }
            else if (mm.State == MatchState.FlickInAir)
            {
                _flickGauge.gameObject.SetActive(false);
                _roundResultPanel.SetActive(false);
                if (_matchBannerRoot != null) _matchBannerRoot.SetActive(false);
                PrepareFlipShowcase(mm, false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);
            }
            else if (mm.State == MatchState.RoundComplete)
            {
                if (_matchBannerRoot != null) _matchBannerRoot.SetActive(true);
                _flickGauge.gameObject.SetActive(false);
                _roundResultPanel.SetActive(true);

                _matchBannerTitle.text = mm.RoundResultTitle;
                if (mm.WasTablaLastRound)
                {
                    _matchBannerSub.text = "Tabla! Auto-retrying the toss...";
                }
                else if (mm.PlayerWonLastRound)
                {
                    int wonCoins = mm.CurrentCoinWager > 0 ? mm.CurrentCoinWager : TeksMatchManager.GetBaseCoinReward(mm.ActiveOpponent);
                    _matchBannerSub.text = $"Victory! +{wonCoins:N0} Coins wager pot claimed!";
                }
                else
                {
                    _matchBannerSub.text = mm.LastCoinLoss > 0
                        ? $"Defeat! -{mm.LastCoinLoss:N0} Coins deducted from wager."
                        : "Defeat! No coins lost.";
                }

                if (_cardFlipShowcaseRoot != null)
                {
                    var showcaseImg = _cardFlipShowcaseRoot.GetComponent<Image>();
                    if (showcaseImg != null) showcaseImg.color = new Color(0.05f, 0.08f, 0.14f, 0.90f);
                    _cardFlipShowcaseRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 16f);
                    _cardFlipShowcaseRoot.SetActive(true);
                    UpdateFlipCardWidgets(mm);
                }

                _roundResultTitle.text = mm.RoundResultTitle;
                _roundResultTitle.color = mm.WasTablaLastRound
                    ? new Color(1.0f, 0.86f, 0.24f)
                    : (mm.PlayerWonLastRound ? new Color(0.28f, 0.96f, 0.48f) : new Color(0.98f, 0.36f, 0.32f));

                string playerName = GetDisplayPlayerName();
                string pSide = mm.PlayerCardFaceUp ? "CHUB (Face-Up)" : "CHA (Face-Down)";
                string oSide = mm.OpponentCardFaceUp ? "CHUB (Face-Up)" : "CHA (Face-Down)";
                _roundResultCardsState.text = $"{playerName}: {pSide}   |   {oppLabel}: {oSide}";

                string banter = string.Empty;
                if (mm.ActiveOpponent != null)
                {
                    string line = mm.WasTablaLastRound
                        ? mm.ActiveOpponent.TablaBanter
                        : (mm.PlayerWonLastRound ? mm.ActiveOpponent.LoseBanter : mm.ActiveOpponent.WinBanter);
                    banter = $"\n{mm.ActiveOpponent.NpcName}: \"{line}\"";
                }

                var gm = TeksGameModeManager.Instance;
                string modeBonusNote = string.Empty;
                if (gm != null && gm.IsSpecialModeActive)
                {
                    string note = gm.GetModeResultBonusNote();
                    if (!string.IsNullOrEmpty(note))
                    {
                        modeBonusNote = "\n" + note;
                    }
                    if (_roundPrimaryBtnText != null) _roundPrimaryBtnText.text = gm.GetResultPrimaryButtonLabel();
                    bool showSec = gm.ShouldShowResultSecondaryButton();
                    if (_roundSecondaryBtn != null) _roundSecondaryBtn.gameObject.SetActive(showSec);
                    if (showSec && _roundSecondaryBtnText != null) _roundSecondaryBtnText.text = gm.GetResultSecondaryButtonLabel();
                }
                else
                {
                    if (_roundPrimaryBtnText != null) _roundPrimaryBtnText.text = "REMATCH";
                    if (_roundSecondaryBtn != null) _roundSecondaryBtn.gameObject.SetActive(true);
                    if (_roundSecondaryBtnText != null) _roundSecondaryBtnText.text = "BACK TO COURT";
                }

                string coinDeltaSummary = mm.RoundResultSubtitle;
                if (!mm.WasTablaLastRound && !mm.PlayerWonLastRound && mm.LastCoinLoss > 0 && !coinDeltaSummary.Contains("-"))
                {
                    coinDeltaSummary += $"  (-{mm.LastCoinLoss:N0} Coins)";
                }

                _roundResultSubtitle.text = coinDeltaSummary + banter + modeBonusNote;
                _roundButtonsRow.SetActive(!mm.WasTablaLastRound);

                RefreshAllUI();
            }
        }

        private void RefreshAllUI()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;

            if (_coinsText != null) _coinsText.text = $"{inv.Coins:N0} COINS";
            if (_gemsText != null) _gemsText.text = $"{inv.Gems:N0} GEMS";
            if (_hudAvatarPortrait != null) _hudAvatarPortrait.sprite = CharacterResourceGenerator.GetCharacterPortraitSprite(inv.SelectedCharacterType);
            if (_hudProfileNameRank != null) _hudProfileNameRank.text = $"{GetDisplayPlayerName().ToUpper()}  •  {inv.GetBarangayRankTitle()}";
            if (_hudStreakBadge != null)
            {
                _hudStreakBadge.text = inv.WinStreak >= 2 ? $"HOT HAND x{inv.WinStreak}!" : (inv.MatchesWon > 0 ? $"WINS: {inv.MatchesWon}" : "");
            }
            if (_hudCompactStatsText != null)
            {
                int losses = Mathf.Max(0, inv.MatchesPlayed - inv.MatchesWon);
                int winPct = inv.MatchesPlayed > 0 ? Mathf.RoundToInt(100f * inv.MatchesWon / inv.MatchesPlayed) : 0;
                var eq = inv.GetEquippedPamato(out var eqDef);
                string pamatoTag = (eq != null && eqDef != null) ? $"{eqDef.Name} {eq.StarRank}★" : "None";
                _hudCompactStatsText.text = $"W/L: {inv.MatchesWon}-{losses} ({winPct}%)  •  Pamato: {pamatoTag}";
            }
            if (_questsBadgePill != null && _questsBadgeText != null)
            {
                int claimable = inv.GetUnclaimedCompletedQuestCount();
                _questsBadgePill.SetActive(claimable > 0);
                if (claimable > 0)
                {
                    _questsBadgeText.text = claimable > 99 ? "99+" : claimable.ToString();
                }
            }
            if (_hudCharacterSubText != null)
            {
                var preset = TeksCharacterPresets.GetPreset(inv.SelectedCharacterType);
                _hudCharacterSubText.text = $"{preset.DisplayName.ToUpper()}  •  PROFILE [U]";
            }

            RefreshPamatoPanel();

            if (_inventoryModal != null && _inventoryModal.activeSelf)
            {
                if (_inventoryHeaderCoins != null) _inventoryHeaderCoins.text = $"{inv.Coins:N0} COINS  |  {inv.Gems:N0} GEMS";
                if (!_isFusionTabOpen) PopulateInventoryModal();
            }
            if (_shopModal != null && _shopModal.activeSelf) PopulateShopModal();

            RefreshExtendedUI();
        }

        private void RefreshPamatoPanel()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;

            if (_pamatoHeaderText != null)
            {
                _pamatoHeaderText.text = $"{GetDisplayPlayerName().ToUpper()}'S PAMATO";
            }

            var equipped = inv.GetEquippedPamato(out var def);
            if (equipped == null || def == null) return;

            // Front equipped card
            _frontCardFrame.color = TeksDatabase.GetRarityColor(def.Rarity);
            _frontCardPortrait.sprite = CardArtGenerator.GetCardPortraitSprite(def, equipped.StarRank);
            _frontCardStars.text = new string('★', equipped.StarRank) + new string('☆', 5 - equipped.StarRank);
            _frontCardName.text = def.Name;
            _frontCardRarity.text = TeksDatabase.GetRarityName(def.Rarity);
            _frontCardBottomStats.text = $"★ {equipped.GetPower(def)}  ⚡ {equipped.GetSpeed(def)}";

            // Background fanned cards
            OwnedTeksCard midOwned = null;
            OwnedTeksCard backOwned = null;
            for (int i = 0; i < inv.OwnedCards.Count; i++)
            {
                var c = inv.OwnedCards[i];
                if (c.CardId == equipped.CardId) continue;
                if (midOwned == null) midOwned = c;
                else if (backOwned == null) { backOwned = c; break; }
            }

            if (midOwned == null) midOwned = equipped;
            if (backOwned == null) backOwned = midOwned;

            var midDef = TeksDatabase.GetCard(midOwned.CardId);
            if (midDef != null)
            {
                _midCardFrame.color = midDef.Id == "bakunawa" ? new Color(0.18f, 0.88f, 1.0f) : TeksDatabase.GetRarityColor(midDef.Rarity);
                _midCardPortrait.sprite = CardArtGenerator.GetCardPortraitSprite(midDef, midOwned.StarRank);
                _midCardName.text = midDef.Name;
                _midCardRarity.text = TeksDatabase.GetRarityName(midDef.Rarity);
            }

            var backDef = TeksDatabase.GetCard(backOwned.CardId);
            if (backDef != null)
            {
                _backCardFrame.color = TeksDatabase.GetRarityColor(backDef.Rarity);
                _backCardPortrait.sprite = CardArtGenerator.GetCardPortraitSprite(backDef, backOwned.StarRank);
            }

            // 4 Stat bars matching screenshot (HP, Atk, Agility, Def)
            int[] values = {
                equipped.GetHP(def),
                equipped.GetAtk(def),
                equipped.GetAgility(def),
                equipped.GetDef(def)
            };
            float[] maxRefs = { 110f, 100f, 50f, 45f };

            for (int i = 0; i < 4; i++)
            {
                if (_statValueTexts[i] != null) _statValueTexts[i].text = values[i].ToString();
                if (_statFillBars[i] != null)
                {
                    float norm = Mathf.Clamp01(values[i] / maxRefs[i]);
                    _statFillBars[i].rectTransform.anchorMax = new Vector2(Mathf.Max(0.12f, norm), 1f);
                }
            }

            if (_pamatoUpgradeSubText != null)
            {
                if (equipped.StarRank >= 5)
                {
                    _pamatoUpgradeSubText.text = "(MAX 5★ / Open Album)";
                }
                else if (inv.CanUpgradeCard(equipped.CardId, out int cost, out _, out _))
                {
                    _pamatoUpgradeSubText.text = $"(Fuse {cost} Coins!)";
                }
                else
                {
                    _pamatoUpgradeSubText.text = "(Card Level up)";
                }
            }
        }

        private void OnPamatoUpgradeClicked()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;

            var equipped = inv.GetEquippedPamato(out var def);
            if (equipped != null && equipped.StarRank < 5 && inv.TryUpgradeCard(equipped.CardId, out string msg))
            {
                ShowToast(msg);
                RefreshAllUI();
            }
            else
            {
                ToggleInventoryModal();
                if (equipped != null && equipped.StarRank >= 5 && def != null)
                {
                    ShowToast($"{def.Name} is already 5★ MAX! Upgrade other cards here.");
                }
            }
        }

        private void RefreshShopDisplay()
        {
            if (_shopModal != null && _shopModal.activeSelf) PopulateShopModal();
        }

        private void ToggleInventoryModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = !_inventoryModal.activeSelf;
            CloseSellConfirmModal();
            CloseAllModals();
            if (next)
            {
                _inventoryModal.SetActive(true);
                SwitchInventoryTab(false);
                SyncPlayerMovementLock();
            }
        }

        private void OpenFusionLabDirect()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            CloseSellConfirmModal();
            if (_inventoryModal.activeSelf && _isFusionTabOpen)
            {
                CloseAllModals();
                return;
            }
            CloseAllModals();
            _inventoryModal.SetActive(true);
            SwitchInventoryTab(true);
            SyncPlayerMovementLock();
        }

        private void SwitchInventoryTab(bool fusionTab)
        {
            CloseSellConfirmModal();
            _isFusionTabOpen = fusionTab;
            if (_inventoryScrollRoot != null) _inventoryScrollRoot.SetActive(!fusionTab);
            if (_fusionLabRoot != null) _fusionLabRoot.SetActive(fusionTab);
            if (_tabCardsBtnImg != null) _tabCardsBtnImg.color = fusionTab ? new Color(0.18f, 0.24f, 0.34f) : new Color(0.20f, 0.54f, 0.88f);
            if (_tabFusionBtnImg != null) _tabFusionBtnImg.color = fusionTab ? new Color(0.62f, 0.24f, 0.88f) : new Color(0.26f, 0.18f, 0.36f);

            if (fusionTab) PopulateFusionLab();
            else PopulateInventoryModal();
        }

        private void PopulateFusionLab(TeksCardDefinition lastForgedCard = null)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _fusionListContent == null) return;

            if (_inventoryHeaderCoins != null) _inventoryHeaderCoins.text = $"{inv.Coins:N0} COINS  |  {inv.Gems:N0} GEMS";

            for (int i = _fusionListContent.childCount - 1; i >= 0; i--)
            {
                var child = _fusionListContent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            // Optional Fusion Result Banner when a card was just forged
            if (lastForgedCard != null)
            {
                var resBox = CreateRoundedPanel("FusionResultBanner", _fusionListContent, new Color(0.16f, 0.11f, 0.26f, 1f), new Vector2(960f, 118f));
                var resLe = resBox.gameObject.AddComponent<LayoutElement>();
                resLe.preferredHeight = 118f;
                AddOutline(resBox.gameObject, TeksDatabase.GetRarityColor(lastForgedCard.Rarity), 2.5f);

                var resImg = CreateImage("ForgedArt", resBox, CardArtGenerator.GetCardPortraitSprite(lastForgedCard, 1));
                resImg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                resImg.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                resImg.rectTransform.anchoredPosition = new Vector2(62f, 0f);
                resImg.rectTransform.sizeDelta = new Vector2(72f, 98f);

                var resTxt = CreateText(
                    "ForgedTxt",
                    resBox,
                    $"FUSION COMPLETE! OBTAINED: {lastForgedCard.Name} ({TeksDatabase.GetRarityName(lastForgedCard.Rarity)})\n\"{lastForgedCard.Tagline}\"  |  HP:{lastForgedCard.BaseHP} ATK:{lastForgedCard.BaseAtk} PWR:{lastForgedCard.BasePower}",
                    17,
                    FontStyle.Bold,
                    new Color(1.0f, 0.90f, 0.42f),
                    TextAnchor.MiddleLeft
                );
                resTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                resTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                resTxt.rectTransform.offsetMin = new Vector2(118f, 8f);
                resTxt.rectTransform.offsetMax = new Vector2(-180f, -8f);

                var eqForgedBtn = CreateButton("EquipForgedBtn", resBox, "EQUIP PAMATO", new Color(0.18f, 0.72f, 0.32f), 14, () =>
                {
                    inv.EquipPamato(lastForgedCard.Id);
                    ShowToast($"Equipped {lastForgedCard.Name} as My Pamato!");
                });
                var eqRt = eqForgedBtn.GetComponent<RectTransform>();
                eqRt.anchorMin = new Vector2(1f, 0.5f);
                eqRt.anchorMax = new Vector2(1f, 0.5f);
                eqRt.anchoredPosition = new Vector2(-92f, 0f);
                eqRt.sizeDelta = new Vector2(150f, 40f);
            }

            // Special Cross-Card Fusion Recipes
            AddFusionSectionHeader("SPECIAL CARD FUSION RECIPES (Combine 2 Cards + Coins -> Exclusive Fusion Card!)");

            foreach (var recipe in TeksDatabase.FusionRecipes)
            {
                var defA = TeksDatabase.GetCard(recipe.IngredientAId);
                var defB = TeksDatabase.GetCard(recipe.IngredientBId);
                var defRes = TeksDatabase.GetCard(recipe.ResultCardId);
                if (defA == null || defB == null || defRes == null) continue;

                bool canFuse = inv.CanFuseRecipe(recipe, out string reason);
                Color resCol = TeksDatabase.GetRarityColor(defRes.Rarity);

                var row = CreateRoundedPanel("Recipe_" + recipe.RecipeId, _fusionListContent, new Color(0.12f, 0.16f, 0.26f, 1f), new Vector2(960f, 114f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 114f;
                AddOutline(row.gameObject, canFuse ? resCol : new Color(0.28f, 0.34f, 0.44f), 2f);

                // Ingredient A
                CreateRecipeMiniThumb(row, defA, inv.GetOwnedCard(defA.Id) != null, new Vector2(68f, 0f));
                CreateText("Plus", row, "+", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter).rectTransform.anchoredPosition = new Vector2(-345f, 0f);
                // Ingredient B
                CreateRecipeMiniThumb(row, defB, inv.GetOwnedCard(defB.Id) != null, new Vector2(215f, 0f));
                CreateText("Equals", row, "=", 28, FontStyle.Bold, new Color(1.0f, 0.85f, 0.24f), TextAnchor.MiddleCenter).rectTransform.anchoredPosition = new Vector2(-195f, 0f);
                // Result Card
                CreateRecipeMiniThumb(row, defRes, true, new Vector2(365f, 0f));

                var descTxt = CreateText(
                    "RecipeDesc",
                    row,
                    $"{defRes.Name} ({TeksDatabase.GetRarityName(defRes.Rarity)})\n{defA.Name} + {defB.Name}\nBase HP:{defRes.BaseHP}  ATK:{defRes.BaseAtk}  PWR:{defRes.BasePower}",
                    14,
                    FontStyle.Bold,
                    Color.white,
                    TextAnchor.MiddleLeft
                );
                descTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                descTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                descTxt.rectTransform.offsetMin = new Vector2(424f, 8f);
                descTxt.rectTransform.offsetMax = new Vector2(-200f, -8f);

                var capturedRecipe = recipe;
                var fuseBtn = CreateButton("FuseRecipeBtn", row, reason, canFuse ? new Color(0.64f, 0.24f, 0.90f) : new Color(0.25f, 0.28f, 0.35f), 14, () =>
                {
                    if (inv.TryFuseRecipe(capturedRecipe, out var forged, out string msg))
                    {
                        ShowToast(msg);
                        PopulateFusionLab(forged);
                    }
                    else
                    {
                        ShowToast(msg);
                    }
                });
                var frRt = fuseBtn.GetComponent<RectTransform>();
                frRt.anchorMin = new Vector2(1f, 0.5f);
                frRt.anchorMax = new Vector2(1f, 0.5f);
                frRt.anchoredPosition = new Vector2(-102f, 0f);
                frRt.sizeDelta = new Vector2(174f, 46f);
            }

            var fusionContentRt = _fusionListContent as RectTransform;
            if (fusionContentRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(fusionContentRt);
            }
        }

        private void AddFusionSectionHeader(string text)
        {
            var go = new GameObject("FusionHeader", typeof(RectTransform), typeof(LayoutElement), typeof(Text));
            go.transform.SetParent(_fusionListContent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 28f;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = 17;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1.0f, 0.85f, 0.30f);
            t.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateRecipeMiniThumb(RectTransform parent, TeksCardDefinition def, bool owned, Vector2 leftOffset)
        {
            var frame = CreateRoundedPanel("Thumb_" + def.Id, parent, owned ? TeksDatabase.GetRarityColor(def.Rarity) : new Color(0.32f, 0.32f, 0.36f), new Vector2(74f, 98f));
            frame.anchorMin = new Vector2(0f, 0.5f);
            frame.anchorMax = new Vector2(0f, 0.5f);
            frame.anchoredPosition = leftOffset;

            var img = CreateImage("Img", frame, CardArtGenerator.GetCardPortraitSprite(def, 3));
            StretchFull(img.rectTransform, 3f);
            if (!owned) img.color = new Color(0.42f, 0.42f, 0.42f, 0.85f);
        }

        private void ToggleShopModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = !_shopModal.activeSelf;
            CloseAllModals();
            if (next)
            {
                PopulateShopModal();
                _shopModal.SetActive(true);
                SyncPlayerMovementLock();
            }
        }

        private void CreateTopCenterActionDock()
        {
            // Top-Center Quick Action Dock (Inventory, Fusion Lab & Missions with refined glass styling)
            var dock = CreateRoundedPanel("TopCenterActionDock", _canvas.transform, new Color(0.06f, 0.10f, 0.17f, 0.94f), new Vector2(630f, 60f));
            dock.anchorMin = new Vector2(0.5f, 1f);
            dock.anchorMax = new Vector2(0.5f, 1f);
            dock.pivot = new Vector2(0.5f, 1f);
            dock.anchoredPosition = new Vector2(0f, -18f);
            AddOutline(dock.gameObject, new Color(0.38f, 0.54f, 0.78f), 2f);

            // Subtle inner glass highlight along top edge of dock
            var dockRim = CreateRoundedPanel("DockTopRim", dock, new Color(1f, 1f, 1f, 0.11f), Vector2.zero);
            dockRim.anchorMin = new Vector2(0.02f, 1f);
            dockRim.anchorMax = new Vector2(0.98f, 1f);
            dockRim.offsetMin = new Vector2(0f, -4f);
            dockRim.offsetMax = new Vector2(0f, -1f);
            dockRim.GetComponent<Image>().raycastTarget = false;

            var rowGo = new GameObject("DockButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(dock, false);
            var rowRt = rowGo.GetComponent<RectTransform>();
            StretchFull(rowRt, 8f);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(2, 2, 0, 0);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateTopDockButton(
                "DockInventoryBtn",
                rowGo.transform,
                "INVENTORY",
                "[I]",
                "backpack",
                new Color(0.14f, 0.38f, 0.72f),
                new Color(0.46f, 0.80f, 1.0f),
                ToggleInventoryModal
            );

            CreateTopDockButton(
                "DockFusionBtn",
                rowGo.transform,
                "FUSION LAB",
                "[F]",
                "fusion",
                new Color(0.44f, 0.18f, 0.68f),
                new Color(0.80f, 0.52f, 1.0f),
                OpenFusionLabDirect
            );

            var missionsBtn = CreateTopDockButton(
                "DockMissionsBtn",
                rowGo.transform,
                "MISSIONS",
                "[Q]",
                "missions",
                new Color(0.70f, 0.40f, 0.10f),
                new Color(1.0f, 0.84f, 0.30f),
                ToggleQuestsModal
            );

            // Claimable quest notification badge pill at top-right corner of Missions button
            var badgeRt = CreateRoundedPanel("MissionsBadgePill", missionsBtn.GetComponent<RectTransform>(), new Color(0.90f, 0.18f, 0.22f), new Vector2(22f, 20f));
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(-6f, -4f);
            badgeRt.GetComponent<Image>().raycastTarget = false;
            AddOutline(badgeRt.gameObject, new Color(1.0f, 0.88f, 0.34f), 1.4f);

            _questsBadgeText = CreateText("BadgeCount", badgeRt, "1", 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(_questsBadgeText.rectTransform, 1f);
            _questsBadgePill = badgeRt.gameObject;
            _questsBadgePill.SetActive(false);
        }

        private Button CreateTopDockButton(
            string name,
            Transform parent,
            string title,
            string hotkey,
            string iconName,
            Color bgColor,
            Color borderColor,
            Action onClick)
        {
            var rt = CreateRoundedPanel(name, parent, bgColor, new Vector2(190f, 44f));
            AddOutline(rt.gameObject, borderColor, 1.6f);

            // Subtle upper highlight + bottom lip for crisp tactile card depth
            var topGlow = CreateRoundedPanel("TopGlow", rt, new Color(1f, 1f, 1f, 0.14f), Vector2.zero);
            topGlow.anchorMin = new Vector2(0.03f, 0.62f);
            topGlow.anchorMax = new Vector2(0.97f, 0.96f);
            topGlow.offsetMin = Vector2.zero;
            topGlow.offsetMax = Vector2.zero;
            topGlow.GetComponent<Image>().raycastTarget = false;

            var bottomLip = CreateRoundedPanel("BottomLip", rt, new Color(0f, 0f, 0f, 0.24f), Vector2.zero);
            bottomLip.anchorMin = new Vector2(0.02f, 0f);
            bottomLip.anchorMax = new Vector2(0.98f, 0f);
            bottomLip.offsetMin = new Vector2(0f, 0f);
            bottomLip.offsetMax = new Vector2(0f, 4f);
            bottomLip.GetComponent<Image>().raycastTarget = false;

            var btn = rt.gameObject.AddComponent<Button>();
            ApplyButtonTint(btn, bgColor);
            btn.onClick.AddListener(() =>
            {
                TeksAudioManager.Instance?.PlayUiClick();
                onClick?.Invoke();
            });

            var icon = CreateImage("Icon", rt, CardArtGenerator.GetUIIconSprite(iconName));
            icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(21f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(24f, 24f);

            var lbl = CreateText("Title", rt, title, 14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            lbl.resizeTextForBestFit = true;
            lbl.resizeTextMinSize = 11;
            lbl.resizeTextMaxSize = 15;
            lbl.rectTransform.anchorMin = new Vector2(0f, 0f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
            lbl.rectTransform.offsetMin = new Vector2(39f, 4f);
            lbl.rectTransform.offsetMax = new Vector2(-42f, -3f);
            AddTextShadow(lbl.gameObject);

            var keyBg = CreateRoundedPanel("HotkeyPill", rt, new Color(0.04f, 0.07f, 0.13f, 0.72f), new Vector2(28f, 20f));
            keyBg.anchorMin = new Vector2(1f, 0.5f);
            keyBg.anchorMax = new Vector2(1f, 0.5f);
            keyBg.anchoredPosition = new Vector2(-20f, 0f);
            keyBg.GetComponent<Image>().raycastTarget = false;

            var keyTxt = CreateText("KeyTxt", keyBg, hotkey, 11, FontStyle.Bold, new Color(0.86f, 0.94f, 1.0f), TextAnchor.MiddleCenter);
            StretchFull(keyTxt.rectTransform, 1f);

            return btn;
        }

        private void PopulateInventoryModal()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _inventoryGridContent == null) return;

            if (_inventoryHeaderCoins != null) _inventoryHeaderCoins.text = $"{inv.Coins:N0} COINS  |  {inv.Gems:N0} GEMS";

            for (int i = _inventoryGridContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_inventoryGridContent.GetChild(i).gameObject);
            }

            foreach (var owned in inv.OwnedCards)
            {
                var def = TeksDatabase.GetCard(owned.CardId);
                if (def == null) continue;

                bool isEquipped = inv.EquippedPamatoId == owned.CardId;
                Color rarityCol = TeksDatabase.GetRarityColor(def.Rarity);

                var cardBox = CreateRoundedPanel("InvItem_" + def.Id, _inventoryGridContent, new Color(0.12f, 0.17f, 0.27f, 1f), new Vector2(315f, 240f));
                AddOutline(cardBox.gameObject, isEquipped ? new Color(1.0f, 0.85f, 0.22f) : rarityCol, isEquipped ? 3f : 2f);

                // Left miniature card preview
                var artFrame = CreateRoundedPanel("ArtFrame", cardBox, rarityCol, new Vector2(104f, 152f));
                artFrame.anchorMin = new Vector2(0f, 1f);
                artFrame.anchorMax = new Vector2(0f, 1f);
                artFrame.anchoredPosition = new Vector2(64f, -92f);

                var artImg = CreateImage("Art", artFrame, CardArtGenerator.GetCardPortraitSprite(def, owned.StarRank));
                StretchFull(artImg.rectTransform, 4f);

                // Right info
                var nameTxt = CreateText("Name", cardBox, def.Name, 16, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
                nameTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                nameTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                nameTxt.rectTransform.offsetMin = new Vector2(124f, -44f);
                nameTxt.rectTransform.offsetMax = new Vector2(-10f, -10f);

                string stars = new string('★', owned.Level) + new string('☆', 5 - owned.Level);
                string fusionBadge = owned.FusionStage > 0 ? $" • Stg.{owned.FusionStage}" : string.Empty;
                var metaTxt = CreateText(
                    "Meta",
                    cardBox,
                    $"{stars} (Lv.{owned.Level}/5{fusionBadge})\n{TeksDatabase.GetRarityName(def.Rarity)} | {TeksDatabase.GetCategoryName(def.Category)}",
                    12,
                    FontStyle.Bold,
                    rarityCol,
                    TextAnchor.UpperLeft
                );
                metaTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                metaTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                metaTxt.rectTransform.offsetMin = new Vector2(124f, -84f);
                metaTxt.rectTransform.offsetMax = new Vector2(-10f, -44f);

                int sweetPct = Mathf.RoundToInt(owned.GetFlickSweetSpotBonus() * 100f);
                string stageLabel = owned.FusionStage > 0 ? $"Fusion Stage {owned.FusionStage}" : "Base Stage";
                var statsTxt = CreateText(
                    "Stats",
                    cardBox,
                    $"HP:{owned.GetHP(def)}  ATK:{owned.GetAtk(def)}  PWR:{owned.GetPower(def)}\nSweetSpot +{sweetPct}%  |  {stageLabel}\nQty: x{owned.Quantity} (Extras: x{owned.DuplicateCount})",
                    12,
                    FontStyle.Normal,
                    new Color(0.84f, 0.90f, 0.98f),
                    TextAnchor.UpperLeft
                );
                statsTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                statsTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                statsTxt.rectTransform.offsetMin = new Vector2(124f, -158f);
                statsTxt.rectTransform.offsetMax = new Vector2(-10f, -86f);

                // Action buttons row at bottom
                var equipBtn = CreateButton("EquipBtn", cardBox, isEquipped ? "EQUIPPED" : "EQUIP", isEquipped ? new Color(0.32f, 0.42f, 0.56f) : new Color(0.20f, 0.54f, 0.88f), 13, () =>
                {
                    inv.EquipPamato(owned.CardId);
                    ShowToast($"Equipped {def.Name} as My Pamato!");
                });
                var eqRt = equipBtn.GetComponent<RectTransform>();
                eqRt.anchorMin = new Vector2(0f, 0f);
                eqRt.anchorMax = new Vector2(0f, 0f);
                eqRt.anchoredPosition = new Vector2(56f, 28f);
                eqRt.sizeDelta = new Vector2(92f, 36f);
                equipBtn.interactable = !isEquipped;

                bool canUpg = inv.CanUpgradeCard(owned.CardId, out int upgCost, out _, out string upgReason);
                string upgLbl = owned.StarRank >= 5 ? "MAX Lv.5" : $"UPGRADE ({upgCost}c)";
                var upgBtn = CreateButton("UpgBtn", cardBox, upgLbl, canUpg ? new Color(0.18f, 0.72f, 0.30f) : new Color(0.26f, 0.30f, 0.38f), 12, () =>
                {
                    if (inv.TryUpgradeCard(owned.CardId, out string msg))
                    {
                        TeksAudioManager.Instance?.PlayCoinClink();
                        ShowToast(msg);
                    }
                    else
                    {
                        ShowToast(upgReason);
                    }
                });
                var upRt = upgBtn.GetComponent<RectTransform>();
                upRt.anchorMin = new Vector2(0f, 0f);
                upRt.anchorMax = new Vector2(0f, 0f);
                upRt.anchoredPosition = new Vector2(160f, 28f);
                upRt.sizeDelta = new Vector2(102f, 36f);

                bool canSell = inv.CanSellCard(owned.CardId, out string sellReason);
                string capturedCardId = owned.CardId;
                var sellBtn = CreateButton("SellBtn", cardBox, $"SELL +{def.SellPrice}", canSell ? new Color(0.76f, 0.48f, 0.14f) : new Color(0.24f, 0.24f, 0.28f), 12, () =>
                {
                    if (inv.CanSellCard(capturedCardId, out string currentReason))
                    {
                        OpenSellConfirmModal(capturedCardId);
                    }
                    else
                    {
                        ShowToast(string.IsNullOrEmpty(currentReason) ? sellReason : currentReason);
                    }
                });
                var slRt = sellBtn.GetComponent<RectTransform>();
                slRt.anchorMin = new Vector2(0f, 0f);
                slRt.anchorMax = new Vector2(0f, 0f);
                slRt.anchoredPosition = new Vector2(262f, 28f);
                slRt.sizeDelta = new Vector2(88f, 36f);
                sellBtn.interactable = canSell;
            }
        }

        private RectTransform CreateCollectibleCardSlab(
            string name,
            Transform parent,
            TeksCardDefinition card,
            int starRank,
            Vector2 size,
            out Image outerFrameImg,
            out Image artImg)
        {
            Color rarityCol = card != null ? TeksDatabase.GetRarityColor(card.Rarity) : new Color(0.45f, 0.52f, 0.64f);
            var slab = CreateRoundedPanel(name, parent, rarityCol, size);
            slab.GetComponent<Image>().raycastTarget = false;
            AddOutline(slab.gameObject, new Color(1f, 1f, 1f, 0.36f), 1.2f);
            outerFrameImg = slab.GetComponent<Image>();

            var innerMatte = CreateRoundedPanel("InnerMatte", slab, new Color(0.05f, 0.08f, 0.14f, 1f), Vector2.zero);
            StretchFull(innerMatte, 3.5f);
            innerMatte.GetComponent<Image>().raycastTarget = false;

            Sprite sprite = card != null ? CardArtGenerator.GetCardPortraitSprite(card, starRank) : null;
            artImg = CreateImage("Art", innerMatte, sprite);
            artImg.preserveAspect = false;
            StretchFull(artImg.rectTransform, 2f);

            var gloss = CreateRoundedPanel("CardGloss", innerMatte, new Color(1f, 1f, 1f, 0.08f), Vector2.zero);
            gloss.anchorMin = new Vector2(0.06f, 0.90f);
            gloss.anchorMax = new Vector2(0.94f, 0.97f);
            gloss.offsetMin = Vector2.zero;
            gloss.offsetMax = Vector2.zero;
            gloss.GetComponent<Image>().raycastTarget = false;

            return slab;
        }

        private void PopulateShopModal()
        {
            var shop = ShopManager.Instance;
            var inv = PlayerInventory.Instance;
            if (shop == null || _shopSlotsContainer == null) return;

            if (_shopModalCoinsText != null && inv != null)
            {
                _shopModalCoinsText.text = $"{inv.Coins:N0} COINS  |  {inv.Gems:N0} GEMS";
            }
            if (_shopVendorBannerText != null) _shopVendorBannerText.text = shop.LastVendorMessage;

            bool canDailyDraw = shop.CanClaimDailyFreeDraw;
            if (_shopDailyFreeBtn != null)
            {
                _shopDailyFreeBtn.interactable = canDailyDraw;
                if (_shopDailyFreeBtnText != null)
                {
                    _shopDailyFreeBtnText.text = canDailyDraw ? "DAILY FREE (3 CARDS)" : "DAILY CLAIMED ✓";
                }
            }
            if (_shopDailyPackBtn != null)
            {
                _shopDailyPackBtn.interactable = canDailyDraw;
                if (_shopDailyPackPillImg != null)
                {
                    _shopDailyPackPillImg.color = canDailyDraw
                        ? new Color(0.18f, 0.72f, 0.34f)
                        : new Color(0.28f, 0.30f, 0.36f);
                }
                if (_shopDailyPackBtnText != null)
                {
                    _shopDailyPackBtnText.text = canDailyDraw ? "CLAIM FREE" : "CLAIMED TODAY ✓";
                }
            }

            for (int i = _shopSlotsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_shopSlotsContainer.GetChild(i).gameObject);
            }

            var slots = shop.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Card == null) continue;

                Color rarityCol = TeksDatabase.GetRarityColor(slot.Card.Rarity);
                Color cabinetBg = slot.IsSuperSlot
                    ? new Color(0.17f, 0.13f, 0.08f, 1f)
                    : Color.Lerp(new Color(0.08f, 0.11f, 0.19f, 1f), rarityCol, 0.14f);
                Color borderCol = slot.IsFlashSale
                    ? new Color(0.98f, 0.30f, 0.24f)
                    : (slot.IsSuperSlot ? new Color(1.0f, 0.84f, 0.22f) : rarityCol);

                var box = new GameObject("ShopSlot_" + i, typeof(RectTransform), typeof(Image));
                box.transform.SetParent(_shopSlotsContainer, false);
                var boxImg = box.GetComponent<Image>();
                boxImg.sprite = CardArtGenerator.GetRoundedPanelSprite();
                boxImg.type = Image.Type.Sliced;
                boxImg.color = cabinetBg;
                AddOutline(box, borderCol, (slot.IsSuperSlot || slot.IsFlashSale) ? 2.6f : 2.0f);

                var boxRt = box.GetComponent<RectTransform>();

                // Top Slot Ribbon Banner
                Color ribbonBg = slot.IsFlashSale
                    ? new Color(0.82f, 0.16f, 0.16f, 0.96f)
                    : (slot.IsSuperSlot ? new Color(0.76f, 0.50f, 0.08f, 0.96f) : Color.Lerp(rarityCol, Color.black, 0.45f));
                string slotTag = slot.IsFlashSale
                    ? "-30% SULIT SALE!"
                    : (slot.IsSuperSlot ? "★ SUPER SLOT (RARE+)" : $"ROTATING SLOT {i + 1}");

                var ribbonRt = CreateRoundedPanel("SlotRibbon", boxRt, ribbonBg, Vector2.zero);
                ribbonRt.anchorMin = new Vector2(0f, 1f);
                ribbonRt.anchorMax = new Vector2(1f, 1f);
                ribbonRt.offsetMin = new Vector2(8f, -30f);
                ribbonRt.offsetMax = new Vector2(-8f, -6f);
                ribbonRt.GetComponent<Image>().raycastTarget = false;
                AddOutline(ribbonRt.gameObject, new Color(borderCol.r, borderCol.g, borderCol.b, 0.72f), 1f);

                var slotTagTxt = CreateText("SlotTag", ribbonRt, slotTag, 11, FontStyle.Bold, new Color(1.0f, 0.97f, 0.88f), TextAnchor.MiddleCenter);
                slotTagTxt.resizeTextForBestFit = true;
                slotTagTxt.resizeTextMinSize = 9;
                slotTagTxt.resizeTextMaxSize = 11;
                StretchFull(slotTagTxt.rectTransform, 2f);
                AddTextShadow(slotTagTxt.gameObject);

                // Left Framed Collectible 2:3 Card Slab (Zero sidebars!)
                var slab = CreateCollectibleCardSlab("CardPreview", boxRt, slot.Card, 1, new Vector2(106f, 159f), out _, out _);
                slab.anchorMin = new Vector2(0f, 1f);
                slab.anchorMax = new Vector2(0f, 1f);
                slab.pivot = new Vector2(0f, 1f);
                slab.anchoredPosition = new Vector2(12f, -36f);

                // Top-left Power Badge on card slab
                var pwrBadge = CreateRoundedPanel("PowerBadge", slab, new Color(0.05f, 0.08f, 0.14f, 0.92f), new Vector2(40f, 18f));
                pwrBadge.anchorMin = new Vector2(0f, 1f);
                pwrBadge.anchorMax = new Vector2(0f, 1f);
                pwrBadge.pivot = new Vector2(0f, 1f);
                pwrBadge.anchoredPosition = new Vector2(4f, -4f);
                pwrBadge.GetComponent<Image>().raycastTarget = false;
                AddOutline(pwrBadge.gameObject, new Color(1.0f, 0.84f, 0.24f), 1f);
                var pwrBadgeTxt = CreateText("PwrTxt", pwrBadge, $"★{slot.Card.BasePower}", 10, FontStyle.Bold, new Color(1.0f, 0.88f, 0.30f), TextAnchor.MiddleCenter);
                StretchFull(pwrBadgeTxt.rectTransform, 1f);

                // Optional Top-right Flash Sale Sticker
                if (slot.IsFlashSale)
                {
                    var saleStick = CreateRoundedPanel("SaleSticker", slab, new Color(0.92f, 0.16f, 0.18f, 0.96f), new Vector2(38f, 18f));
                    saleStick.anchorMin = new Vector2(1f, 1f);
                    saleStick.anchorMax = new Vector2(1f, 1f);
                    saleStick.pivot = new Vector2(1f, 1f);
                    saleStick.anchoredPosition = new Vector2(-4f, -4f);
                    saleStick.GetComponent<Image>().raycastTarget = false;
                    AddOutline(saleStick.gameObject, Color.white, 1f);
                    var ssTxt = CreateText("SaleTxt", saleStick, "-30%", 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                    StretchFull(ssTxt.rectTransform, 1f);
                }

                // Bottom miniature card footer scrim
                var footerScrim = CreateRoundedPanel("CardFooterScrim", slab, new Color(0.03f, 0.05f, 0.10f, 0.88f), Vector2.zero);
                footerScrim.anchorMin = new Vector2(0f, 0f);
                footerScrim.anchorMax = new Vector2(1f, 0f);
                footerScrim.offsetMin = new Vector2(3.5f, 3.5f);
                footerScrim.offsetMax = new Vector2(-3.5f, 20f);
                footerScrim.GetComponent<Image>().raycastTarget = false;
                var scrimTxt = CreateText("ScrimTxt", footerScrim, slot.IsSuperSlot ? "SUPER RARE+" : "PAMATO TEKS", 9, FontStyle.Bold, slot.IsSuperSlot ? new Color(1.0f, 0.88f, 0.28f) : new Color(0.84f, 0.92f, 1.0f), TextAnchor.MiddleCenter);
                scrimTxt.resizeTextForBestFit = true;
                scrimTxt.resizeTextMinSize = 8;
                scrimTxt.resizeTextMaxSize = 9;
                StretchFull(scrimTxt.rectTransform, 1f);

                if (slot.IsSoldOut)
                {
                    var soldOverlay = CreateRoundedPanel("SoldOverlay", slab, new Color(0.02f, 0.04f, 0.08f, 0.74f), Vector2.zero);
                    StretchFull(soldOverlay, 2f);
                    soldOverlay.GetComponent<Image>().raycastTarget = false;
                    var soldTxt = CreateText("SoldTxt", soldOverlay, "SOLD\nOUT", 18, FontStyle.Bold, new Color(1.0f, 0.84f, 0.30f), TextAnchor.MiddleCenter);
                    StretchFull(soldTxt.rectTransform, 4f);
                    AddTextShadow(soldTxt.gameObject);
                }

                // Right Spec Column (Name, Compact Rarity Pill, Category, Combat Stats, Ownership)
                var cardName = CreateText("Name", boxRt, slot.Card.Name, 14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                cardName.resizeTextForBestFit = true;
                cardName.resizeTextMinSize = 11;
                cardName.resizeTextMaxSize = 14;
                cardName.verticalOverflow = VerticalWrapMode.Truncate;
                cardName.rectTransform.anchorMin = new Vector2(0f, 1f);
                cardName.rectTransform.anchorMax = new Vector2(1f, 1f);
                cardName.rectTransform.offsetMin = new Vector2(126f, -74f);
                cardName.rectTransform.offsetMax = new Vector2(-10f, -36f);
                AddTextShadow(cardName.gameObject);

                var rarityPill = CreateRoundedPanel("RarityPill", boxRt, Color.Lerp(rarityCol, Color.black, 0.30f), new Vector2(96f, 20f));
                rarityPill.anchorMin = new Vector2(0f, 1f);
                rarityPill.anchorMax = new Vector2(0f, 1f);
                rarityPill.pivot = new Vector2(0f, 1f);
                rarityPill.anchoredPosition = new Vector2(126f, -78f);
                rarityPill.GetComponent<Image>().raycastTarget = false;
                AddOutline(rarityPill.gameObject, rarityCol, 1.2f);
                var rarityTxt = CreateText("RarityTxt", rarityPill, TeksDatabase.GetRarityName(slot.Card.Rarity).ToUpper(), 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                rarityTxt.resizeTextForBestFit = true;
                rarityTxt.resizeTextMinSize = 8;
                rarityTxt.resizeTextMaxSize = 10;
                StretchFull(rarityTxt.rectTransform, 2f);

                var catTxt = CreateText("Category", boxRt, TeksDatabase.GetCategoryName(slot.Card.Category).ToUpper(), 11, FontStyle.Bold, new Color(0.56f, 0.83f, 1.0f), TextAnchor.MiddleLeft);
                catTxt.resizeTextForBestFit = true;
                catTxt.resizeTextMinSize = 9;
                catTxt.resizeTextMaxSize = 11;
                catTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                catTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                catTxt.rectTransform.offsetMin = new Vector2(126f, -120f);
                catTxt.rectTransform.offsetMax = new Vector2(-10f, -101f);

                var statChip1 = CreateRoundedPanel("StatChip1", boxRt, new Color(0.05f, 0.08f, 0.14f, 0.92f), Vector2.zero);
                statChip1.anchorMin = new Vector2(0f, 1f);
                statChip1.anchorMax = new Vector2(1f, 1f);
                statChip1.offsetMin = new Vector2(126f, -148f);
                statChip1.offsetMax = new Vector2(-10f, -124f);
                statChip1.GetComponent<Image>().raycastTarget = false;
                AddOutline(statChip1.gameObject, new Color(0.24f, 0.34f, 0.50f), 1f);
                var statTxt1 = CreateText("StatTxt1", statChip1, $"<color=#6FF29D>HP {slot.Card.BaseHP}</color>  •  <color=#FF9E70>ATK {slot.Card.BaseAtk}</color>", 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                statTxt1.supportRichText = true;
                statTxt1.resizeTextForBestFit = true;
                statTxt1.resizeTextMinSize = 9;
                statTxt1.resizeTextMaxSize = 11;
                StretchFull(statTxt1.rectTransform, 4f);

                var ownedCard = inv != null ? inv.GetOwnedCard(slot.Card.Id) : null;
                string ownTag = ownedCard == null
                    ? "<color=#FFD94A>NEW!</color>"
                    : $"<color=#8FC8FF>OWN x{ownedCard.Quantity}</color>";

                var statChip2 = CreateRoundedPanel("StatChip2", boxRt, new Color(0.05f, 0.08f, 0.14f, 0.92f), Vector2.zero);
                statChip2.anchorMin = new Vector2(0f, 1f);
                statChip2.anchorMax = new Vector2(1f, 1f);
                statChip2.offsetMin = new Vector2(126f, -176f);
                statChip2.offsetMax = new Vector2(-10f, -152f);
                statChip2.GetComponent<Image>().raycastTarget = false;
                AddOutline(statChip2.gameObject, new Color(0.24f, 0.34f, 0.50f), 1f);
                var statTxt2 = CreateText("StatTxt2", statChip2, $"PWR {slot.Card.BasePower}  •  {ownTag}", 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                statTxt2.supportRichText = true;
                statTxt2.resizeTextForBestFit = true;
                statTxt2.resizeTextMinSize = 9;
                statTxt2.resizeTextMaxSize = 11;
                StretchFull(statTxt2.rectTransform, 4f);

                var tagLineTxt = CreateText("Tagline", boxRt, $"\"{slot.Card.Tagline}\"", 10, FontStyle.Italic, new Color(0.70f, 0.80f, 0.92f), TextAnchor.MiddleLeft);
                tagLineTxt.resizeTextForBestFit = true;
                tagLineTxt.resizeTextMinSize = 8;
                tagLineTxt.resizeTextMaxSize = 10;
                tagLineTxt.verticalOverflow = VerticalWrapMode.Truncate;
                tagLineTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                tagLineTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                tagLineTxt.rectTransform.offsetMin = new Vector2(126f, -198f);
                tagLineTxt.rectTransform.offsetMax = new Vector2(-10f, -179f);

                // Bottom Tactile Buy Button
                int capturedIdx = i;
                string btnLabel = slot.IsSoldOut
                    ? "SOLD OUT"
                    : (slot.IsFlashSale ? $"SALE {slot.Price}c (Was {slot.OriginalPrice}c)" : $"BUY  •  {slot.Price} COINS");
                Color btnCol = slot.IsSoldOut
                    ? new Color(0.24f, 0.26f, 0.32f)
                    : (slot.IsFlashSale ? new Color(0.88f, 0.24f, 0.20f) : new Color(0.16f, 0.68f, 0.32f));
                Color btnOutlineCol = slot.IsSoldOut
                    ? new Color(0.36f, 0.38f, 0.44f)
                    : (slot.IsFlashSale ? new Color(1.0f, 0.60f, 0.52f) : new Color(0.46f, 0.96f, 0.60f));

                var buyBtn = CreateButton("BuySlotBtn", boxRt, "", btnCol, 13, () =>
                {
                    if (ShopManager.Instance.TryBuySlot(capturedIdx, out string msg))
                    {
                        TeksAudioManager.Instance?.PlayCoinClink();
                        ShowToast(msg);
                        PopulateShopModal();
                    }
                    else
                    {
                        ShowToast(msg);
                    }
                });
                var bRt = buyBtn.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 0f);
                bRt.anchorMax = new Vector2(1f, 0f);
                bRt.offsetMin = new Vector2(10f, 8f);
                bRt.offsetMax = new Vector2(-10f, 44f);
                ApplyButtonTint(buyBtn, btnCol);
                AddOutline(buyBtn.gameObject, btnOutlineCol, 1.4f);

                if (!slot.IsSoldOut)
                {
                    var buyGloss = CreateRoundedPanel("BuyGloss", bRt, new Color(1f, 1f, 1f, 0.14f), Vector2.zero);
                    buyGloss.anchorMin = new Vector2(0.03f, 0.60f);
                    buyGloss.anchorMax = new Vector2(0.97f, 0.94f);
                    buyGloss.offsetMin = Vector2.zero;
                    buyGloss.offsetMax = Vector2.zero;
                    buyGloss.GetComponent<Image>().raycastTarget = false;

                    var coinIc = CreateImage("CoinIc", bRt, CardArtGenerator.GetUIIconSprite("coin"));
                    coinIc.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    coinIc.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    coinIc.rectTransform.anchoredPosition = new Vector2(20f, 0f);
                    coinIc.rectTransform.sizeDelta = new Vector2(17f, 17f);
                }

                var buyLbl = CreateText("Label", bRt, btnLabel, 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                buyLbl.resizeTextForBestFit = true;
                buyLbl.resizeTextMinSize = 10;
                buyLbl.resizeTextMaxSize = 13;
                buyLbl.rectTransform.anchorMin = Vector2.zero;
                buyLbl.rectTransform.anchorMax = Vector2.one;
                buyLbl.rectTransform.offsetMin = new Vector2(slot.IsSoldOut ? 8f : 32f, 2f);
                buyLbl.rectTransform.offsetMax = new Vector2(-8f, -2f);
                AddTextShadow(buyLbl.gameObject);

                buyBtn.interactable = !slot.IsSoldOut;
            }
        }

        private void OpenShopPack(ShopPackType packType)
        {
            if (ShopManager.Instance == null) return;
            if (packType == ShopPackType.DailyFree)
            {
                OnDailyFreeDrawClicked();
                return;
            }

            if (!ShopManager.Instance.TryBuyPack(packType, out var drawn, out string msg))
            {
                ShowToast(msg);
                return;
            }

            TeksAudioManager.Instance?.PlayPackOpen();
            TeksAudioManager.Instance?.PlayCoinClink();
            ShowToast(msg);

            var info = ShopManager.Instance.GetPackInfo(packType);
            RenderPackReveal($"{info.DisplayName.ToUpper()} OPENED! {drawn.Count} Cards added to your Collection:", drawn);
            PopulateShopModal();
        }

        private void OnDailyFreeDrawClicked()
        {
            if (ShopManager.Instance == null) return;
            if (ShopManager.Instance.TryClaimDailyFreeDraw(out var drawn, out int baryaBonus, out string msg))
            {
                TeksAudioManager.Instance?.PlayPackOpen();
                TeksAudioManager.Instance?.PlayCoinClink();
                ShowToast(msg);
                int count = drawn != null ? drawn.Count : 3;
                RenderPackReveal($"DAILY FREE DRAW CLAIMED! +{baryaBonus} Barya Coins & {count} Free Cards:", drawn);
                PopulateShopModal();
            }
            else
            {
                ShowToast(string.IsNullOrEmpty(msg) ? "Already claimed today's Daily Free Draw!" : msg);
            }
        }

        private void RenderPackReveal(string headerText, List<TeksCardDefinition> drawn)
        {
            if (_shopModalCoinsText != null && PlayerInventory.Instance != null)
            {
                _shopModalCoinsText.text = $"{PlayerInventory.Instance.Coins:N0} COINS  |  {PlayerInventory.Instance.Gems:N0} GEMS";
            }
            if (_shopVendorBannerText != null && ShopManager.Instance != null)
            {
                _shopVendorBannerText.text = ShopManager.Instance.LastVendorMessage;
            }
            if (_packRevealHeader != null) _packRevealHeader.text = headerText;

            for (int i = _packRevealContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_packRevealContainer.GetChild(i).gameObject);
            }

            if (drawn == null || drawn.Count == 0) return;

            int count = drawn.Count;
            float spacing = count > 4 ? 10f : 12f;
            var containerHlg = _packRevealContainer.GetComponent<HorizontalLayoutGroup>();
            if (containerHlg != null)
            {
                containerHlg.spacing = spacing;
                containerHlg.childAlignment = TextAnchor.MiddleCenter;
            }

            float availableW = 1080f - Mathf.Max(0, count - 1) * spacing;
            float tileW = Mathf.Clamp(availableW / Mathf.Max(1, count), 206f, 326f);
            bool compactTile = tileW < 232f;

            foreach (var card in drawn)
            {
                if (card == null) continue;
                Color rarityCol = TeksDatabase.GetRarityColor(card.Rarity);
                Color tileBg = Color.Lerp(new Color(0.08f, 0.11f, 0.19f, 1f), rarityCol, 0.16f);

                var cardPill = new GameObject("Drawn_" + card.Id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                cardPill.transform.SetParent(_packRevealContainer, false);

                var le = cardPill.GetComponent<LayoutElement>();
                le.preferredWidth = tileW;
                le.minWidth = tileW;

                var pillImg = cardPill.GetComponent<Image>();
                pillImg.sprite = CardArtGenerator.GetRoundedPanelSprite();
                pillImg.type = Image.Type.Sliced;
                pillImg.color = tileBg;
                AddOutline(cardPill, rarityCol, 2.2f);

                var pillRt = cardPill.GetComponent<RectTransform>();
                pillRt.sizeDelta = new Vector2(tileW, 132f);

                // Left vertical rarity accent bar & top glass rim
                var accentBar = CreateRoundedPanel("RarityAccentBar", pillRt, rarityCol, Vector2.zero);
                accentBar.anchorMin = new Vector2(0f, 0f);
                accentBar.anchorMax = new Vector2(0f, 1f);
                accentBar.offsetMin = new Vector2(4f, 8f);
                accentBar.offsetMax = new Vector2(8f, -8f);
                accentBar.GetComponent<Image>().raycastTarget = false;

                var topRim = CreateRoundedPanel("TopRim", pillRt, new Color(1f, 1f, 1f, 0.09f), Vector2.zero);
                topRim.anchorMin = new Vector2(0.02f, 0.91f);
                topRim.anchorMax = new Vector2(0.98f, 0.98f);
                topRim.offsetMin = Vector2.zero;
                topRim.offsetMax = Vector2.zero;
                topRim.GetComponent<Image>().raycastTarget = false;

                // Fixed 2:3 Collectible Card Portrait Slab (Zero sidebars!)
                var thumbFrame = CreateCollectibleCardSlab("ThumbFrame", pillRt, card, 1, new Vector2(74f, 111f), out _, out _);
                thumbFrame.anchorMin = new Vector2(0f, 0.5f);
                thumbFrame.anchorMax = new Vector2(0f, 0.5f);
                thumbFrame.anchoredPosition = new Vector2(50f, 0f);

                var miniScrim = CreateRoundedPanel("MiniScrim", thumbFrame, new Color(0.03f, 0.05f, 0.10f, 0.86f), Vector2.zero);
                miniScrim.anchorMin = new Vector2(0f, 0f);
                miniScrim.anchorMax = new Vector2(1f, 0f);
                miniScrim.offsetMin = new Vector2(3.5f, 3.5f);
                miniScrim.offsetMax = new Vector2(-3.5f, 16f);
                miniScrim.GetComponent<Image>().raycastTarget = false;
                var miniLvlTxt = CreateText("MiniLvl", miniScrim, "LV.1 CARD", 8, FontStyle.Bold, new Color(1.0f, 0.88f, 0.34f), TextAnchor.MiddleCenter);
                miniLvlTxt.resizeTextForBestFit = true;
                miniLvlTxt.resizeTextMinSize = 7;
                miniLvlTxt.resizeTextMaxSize = 8;
                StretchFull(miniLvlTxt.rectTransform, 1f);

                // Right Column: Name, Compact Rarity Tag + Power Chip, Category & Stats
                var nameTxt = CreateText("CardName", pillRt, card.Name, 14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                nameTxt.resizeTextForBestFit = true;
                nameTxt.resizeTextMinSize = 10;
                nameTxt.resizeTextMaxSize = 14;
                nameTxt.verticalOverflow = VerticalWrapMode.Truncate;
                nameTxt.rectTransform.anchorMin = new Vector2(0f, 0.65f);
                nameTxt.rectTransform.anchorMax = new Vector2(1f, 0.95f);
                nameTxt.rectTransform.offsetMin = new Vector2(95f, 0f);
                nameTxt.rectTransform.offsetMax = new Vector2(-8f, -4f);
                AddTextShadow(nameTxt.gameObject);

                float rarityPillW = compactTile ? 58f : 74f;
                var rarityPill = CreateRoundedPanel("RarityPill", pillRt, Color.Lerp(rarityCol, Color.black, 0.30f), new Vector2(rarityPillW, 20f));
                rarityPill.anchorMin = new Vector2(0f, 0.50f);
                rarityPill.anchorMax = new Vector2(0f, 0.50f);
                rarityPill.pivot = new Vector2(0f, 0.5f);
                rarityPill.anchoredPosition = new Vector2(95f, -3f);
                rarityPill.GetComponent<Image>().raycastTarget = false;
                AddOutline(rarityPill.gameObject, rarityCol, 1.2f);

                var rarityTxt = CreateText("RarityTxt", rarityPill, TeksDatabase.GetRarityName(card.Rarity).ToUpper(), 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                rarityTxt.resizeTextForBestFit = true;
                rarityTxt.resizeTextMinSize = 8;
                rarityTxt.resizeTextMaxSize = 10;
                StretchFull(rarityTxt.rectTransform, 2f);

                float pwrChipW = compactTile ? 38f : 44f;
                var pwrChip = CreateRoundedPanel("PowerChip", pillRt, new Color(0.22f, 0.16f, 0.05f, 1f), new Vector2(pwrChipW, 20f));
                pwrChip.anchorMin = new Vector2(0f, 0.50f);
                pwrChip.anchorMax = new Vector2(0f, 0.50f);
                pwrChip.pivot = new Vector2(0f, 0.5f);
                pwrChip.anchoredPosition = new Vector2(95f + rarityPillW + 4f, -3f);
                pwrChip.GetComponent<Image>().raycastTarget = false;
                AddOutline(pwrChip.gameObject, new Color(1.0f, 0.82f, 0.24f), 1.1f);

                var pwrTxt = CreateText("PwrTxt", pwrChip, $"★{card.BasePower}", 10, FontStyle.Bold, new Color(1.0f, 0.88f, 0.30f), TextAnchor.MiddleCenter);
                pwrTxt.resizeTextForBestFit = true;
                pwrTxt.resizeTextMinSize = 8;
                pwrTxt.resizeTextMaxSize = 10;
                StretchFull(pwrTxt.rectTransform, 2f);

                var catTxt = CreateText("CatTxt", pillRt, TeksDatabase.GetCategoryName(card.Category).ToUpper(), 11, FontStyle.Bold, new Color(0.56f, 0.83f, 1.0f), TextAnchor.MiddleLeft);
                catTxt.resizeTextForBestFit = true;
                catTxt.resizeTextMinSize = 9;
                catTxt.resizeTextMaxSize = 11;
                catTxt.rectTransform.anchorMin = new Vector2(0f, 0.21f);
                catTxt.rectTransform.anchorMax = new Vector2(1f, 0.37f);
                catTxt.rectTransform.offsetMin = new Vector2(95f, 0f);
                catTxt.rectTransform.offsetMax = new Vector2(-8f, 0f);

                var statsTxt = CreateText("StatsTxt", pillRt, $"<color=#6FF29D>HP {card.BaseHP}</color>  •  <color=#FF9E70>ATK {card.BaseAtk}</color>", 11, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                statsTxt.supportRichText = true;
                statsTxt.resizeTextForBestFit = true;
                statsTxt.resizeTextMinSize = 9;
                statsTxt.resizeTextMaxSize = 11;
                statsTxt.rectTransform.anchorMin = new Vector2(0f, 0.04f);
                statsTxt.rectTransform.anchorMax = new Vector2(1f, 0.21f);
                statsTxt.rectTransform.offsetMin = new Vector2(95f, 2f);
                statsTxt.rectTransform.offsetMax = new Vector2(-8f, 0f);
            }

            var rowRt = _packRevealContainer as RectTransform;
            if (rowRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRt);
            }
        }

        private void PopulateWagerModal()
        {
            var mm = TeksMatchManager.Instance;
            var inv = PlayerInventory.Instance;
            if (mm == null || inv == null || mm.ActiveOpponent == null) return;

            string playerName = GetDisplayPlayerName();
            var equipped = mm.GetActivePlayerPamato(out var pDef);
            int pChubPct = (equipped != null && pDef != null)
                ? Mathf.RoundToInt(TeksMatchManager.CalculatePlayerChubProbability(pDef.Rarity, equipped.StarRank) * 100f)
                : 50;

            if (_wagerPlayerName != null) _wagerPlayerName.text = playerName.ToUpper();
            if (_wagerPlayerOddsPillText != null) _wagerPlayerOddsPillText.text = $"{pChubPct}% CHUB";
            if (_wagerPlayerAvatarImg != null)
            {
                _wagerPlayerAvatarImg.sprite = CharacterResourceGenerator.GetCharacterPortraitSprite(inv.SelectedCharacterType);
            }
            if (_wagerPlayerBadgeText != null)
            {
                string streakStr = inv.WinStreak >= 2 ? $"HOT STREAK x{inv.WinStreak}" : $"Wins: {inv.MatchesWon}";
                _wagerPlayerBadgeText.text = $"{inv.GetBarangayRankTitle()}\n{streakStr}";
            }

            int pPwr = 0, pHp = 0, pAtk = 0;
            if (equipped != null && pDef != null)
            {
                Color pRarityCol = TeksDatabase.GetRarityColor(pDef.Rarity);
                if (_wagerPlayerCardFrame != null) _wagerPlayerCardFrame.color = pRarityCol;
                if (_wagerPlayerCardImg != null) _wagerPlayerCardImg.sprite = CardArtGenerator.GetCardPortraitSprite(pDef, equipped.StarRank);
                if (_wagerPlayerCardLevelBadge != null) _wagerPlayerCardLevelBadge.text = $"Lv.{equipped.Level}";
                if (_wagerPlayerCardStarsText != null)
                {
                    int pStars = Mathf.Clamp(equipped.StarRank, 1, 5);
                    _wagerPlayerCardStarsText.text = new string('★', pStars) + new string('☆', 5 - pStars);
                }
                if (_wagerPlayerCardText != null)
                {
                    string pRarHex = ColorUtility.ToHtmlStringRGB(pRarityCol);
                    _wagerPlayerCardText.text = $"{pDef.Name}\n<color=#{pRarHex}>{TeksDatabase.GetRarityName(pDef.Rarity)} • {TeksDatabase.GetCategoryName(pDef.Category)}</color>";
                }
                pPwr = equipped.GetPower(pDef);
                pHp = equipped.GetHP(pDef);
                pAtk = equipped.GetAtk(pDef);
            }

            var opp = mm.ActiveOpponent;
            int npcFrontPct = Mathf.RoundToInt(opp.GetAiChubProbability() * 100f);
            if (_wagerOppName != null) _wagerOppName.text = opp.NpcName.ToUpper();
            if (_wagerOppOddsPillText != null) _wagerOppOddsPillText.text = $"{npcFrontPct}% CHUB";
            if (_wagerOppAvatarImg != null)
            {
                _wagerOppAvatarImg.sprite = CharacterResourceGenerator.GetNpcPortraitSprite(opp.NpcName, opp.Difficulty, opp.IsChampion, opp.CharacterPrefabNumber);
            }
            if (_wagerOppBadgeText != null)
            {
                string areaName = BarangayCourtBuilder.GetAreaDisplayName(opp.AreaId);
                string roleTag = opp.IsChampion ? "[CHAMPION]" : (opp.IsStoryOpponent ? "[STORY]" : $"[{opp.Difficulty}]");
                string defeatedTag = inv.HasDefeatedNpc(opp.GetEffectiveNpcId()) ? "\nDEFEATED ✓" : string.Empty;
                _wagerOppBadgeText.text = $"{areaName}\n{roleTag}{defeatedTag}";
            }
            if (_wagerChallengeQuoteText != null)
            {
                _wagerChallengeQuoteText.text = $"{opp.NpcName} to {playerName}: \"{opp.ChallengeQuote}\"";
            }

            string playerCardId = equipped != null ? equipped.CardId : null;
            string resolvedOppCardId = mm.GetResolvedOpponentCardId(opp, playerCardId);
            var oDef = TeksDatabase.GetCard(resolvedOppCardId);
            int oPwr = 0, oHp = 0, oAtk = 0;
            if (oDef != null)
            {
                int oStars = Mathf.Clamp(opp.CardStarRank, 1, 5);
                var oppVirtualCard = new OwnedTeksCard(oDef.Id, oStars, 1, 0);
                Color oRarityCol = TeksDatabase.GetRarityColor(oDef.Rarity);
                if (_wagerOppCardFrame != null) _wagerOppCardFrame.color = oRarityCol;
                if (_wagerOppCardImg != null) _wagerOppCardImg.sprite = CardArtGenerator.GetCardPortraitSprite(oDef, oStars);
                if (_wagerOppCardLevelBadge != null) _wagerOppCardLevelBadge.text = $"Lv.{oStars}";
                if (_wagerOppCardStarsText != null) _wagerOppCardStarsText.text = new string('★', oStars) + new string('☆', 5 - oStars);
                if (_wagerOppCardText != null)
                {
                    string oRarHex = ColorUtility.ToHtmlStringRGB(oRarityCol);
                    _wagerOppCardText.text = $"{oDef.Name}\n<color=#{oRarHex}>{TeksDatabase.GetRarityName(oDef.Rarity)} • {TeksDatabase.GetCategoryName(oDef.Category)}</color>";
                }
                oPwr = oppVirtualCard.GetPower(oDef);
                oHp = oppVirtualCard.GetHP(oDef);
                oAtk = oppVirtualCard.GetAtk(oDef);
            }

            UpdateStatCompareLabel(_wagerPwrCompareText, "POWER", pPwr, oPwr);
            UpdateStatCompareLabel(_wagerHpCompareText, "HP", pHp, oHp);
            UpdateStatCompareLabel(_wagerAtkCompareText, "ATTACK", pAtk, oAtk);

            int maxCoins = inv.Coins;
            _selectedWagerAmount = Mathf.Clamp(_selectedWagerAmount, 0, Mathf.Max(0, maxCoins));
            RefreshWagerText();
        }

        private static void UpdateStatCompareLabel(Text label, string statName, int playerVal, int oppVal)
        {
            if (label == null) return;
            string pCol = playerVal > oppVal ? "#6FF29D" : (playerVal == oppVal ? "#FFD94A" : "#B8C8DC");
            string oCol = oppVal > playerVal ? "#FF9E70" : (oppVal == playerVal ? "#FFD94A" : "#B8C8DC");
            label.text = $"{statName}:  <color={pCol}>{playerVal}</color>  <color=#7892B2>vs</color>  <color={oCol}>{oppVal}</color>";
        }

        private void RefreshWagerText()
        {
            var mm = TeksMatchManager.Instance;
            var inv = PlayerInventory.Instance;
            var opp = mm != null ? mm.ActiveOpponent : null;
            int npcFrontPct = opp != null ? Mathf.RoundToInt(opp.GetAiChubProbability() * 100f) : 50;
            int pChubPct = GetCurrentPlayerChubPercent();
            int pChaPct = 100 - pChubPct;
            string playerName = GetDisplayPlayerName();
            int maxCoins = inv != null ? inv.Coins : 0;

            string ruleTag = (mm != null && mm.HasCardRestriction && !string.IsNullOrEmpty(mm.CardRestrictionLabel))
                ? $"[{mm.CardRestrictionLabel}]  •  "
                : string.Empty;

            if (_wagerAmountText != null)
            {
                if (_selectedWagerAmount > 0)
                {
                    _wagerAmountText.text = $"{ruleTag}MATCH WAGER: {_selectedWagerAmount:N0} COINS  (WIN +{_selectedWagerAmount:N0}c  |  LOSE -{_selectedWagerAmount:N0}c)";
                }
                else
                {
                    int freeWin = TeksMatchManager.GetBaseCoinReward(opp);
                    _wagerAmountText.text = $"{ruleTag}FREE MATCH  (WIN +{freeWin:N0}c  |  LOSE 0c)";
                }
            }

            for (int i = 0; i < WagerChipValues.Length; i++)
            {
                int val = WagerChipValues[i];
                bool isSelected = _selectedWagerAmount == val;
                bool affordable = val == 0 || val <= maxCoins;

                if (_wagerChipImgs[i] != null)
                {
                    _wagerChipImgs[i].color = isSelected
                        ? new Color(0.84f, 0.56f, 0.10f, 1f)
                        : (affordable ? new Color(0.16f, 0.25f, 0.40f, 1f) : new Color(0.11f, 0.14f, 0.21f, 0.90f));
                }
                if (_wagerChipOutlines[i] != null)
                {
                    _wagerChipOutlines[i].effectColor = isSelected
                        ? new Color(1.0f, 0.88f, 0.30f)
                        : (affordable ? new Color(0.36f, 0.56f, 0.84f) : new Color(0.24f, 0.28f, 0.36f));
                    float dist = isSelected ? 2.2f : 1.2f;
                    _wagerChipOutlines[i].effectDistance = new Vector2(dist, dist);
                }
                if (_wagerChipTexts[i] != null)
                {
                    _wagerChipTexts[i].color = isSelected
                        ? Color.white
                        : (affordable ? new Color(0.86f, 0.94f, 1.0f) : new Color(0.50f, 0.56f, 0.66f));
                }
            }

            if (_wagerBonusInfoText != null)
            {
                string diffName = opp != null ? opp.Difficulty.ToString() : "Medium";
                _wagerBonusInfoText.text = $"Odds: {playerName} {pChubPct}% CHUB / {pChaPct}% CHA vs {diffName} AI ({npcFrontPct}% CHUB)  •  Tabla = Auto-Retry  •  Both Keep Cards!";
            }
        }

        public void ShowToast(string message)
        {
            if (_toastPanel == null || _toastText == null) return;
            _toastText.text = message;
            _toastPanel.SetActive(true);
            _toastTimer = 2.8f;
        }

        private GameObject CreateModalBackdrop(string name, Vector2 windowSize, out RectTransform windowRt)
        {
            var backdrop = new GameObject(name, typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(_canvas.transform, false);
            var bgRt = backdrop.GetComponent<RectTransform>();
            StretchFull(bgRt, 0f);
            backdrop.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.72f);

            windowRt = CreateRoundedPanel("Window", bgRt, new Color(0.09f, 0.13f, 0.21f, 0.98f), windowSize);
            AddOutline(windowRt.gameObject, new Color(0.36f, 0.50f, 0.72f), 2.5f);
            return backdrop;
        }

        private RectTransform CreateScrollView(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, out Transform content)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = offsetMin;
            scrollRt.offsetMax = offsetMax;
            scrollGo.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.15f, 0.92f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollRt, false);
            var vpRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(vpRt, 6f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpRt, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.viewport = vpRt;
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 32f;

            content = contentRt;
            return scrollRt;
        }

        private RectTransform CreateRoundedPanel(string name, Transform parent, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = CardArtGenerator.GetRoundedPanelSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Button CreateButton(string name, Transform parent, string label, Color bgColor, int fontSize, Action onClick)
        {
            var rt = CreateRoundedPanel(name, parent, bgColor, new Vector2(160f, 40f));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                TeksAudioManager.Instance?.PlayUiClick();
                onClick?.Invoke();
            });

            if (!string.IsNullOrEmpty(label))
            {
                var txt = CreateText("Label", rt, label, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                StretchFull(txt.rectTransform, 4f);
            }
            return btn;
        }

        private static void StretchFull(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void AddOutline(GameObject go, Color color, float dist)
        {
            var ol = go.AddComponent<Outline>();
            ol.effectColor = color;
            ol.effectDistance = new Vector2(dist, dist);
        }

        private static void AddTextShadow(GameObject go)
        {
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.72f);
            sh.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static void ApplyButtonTint(Button btn, Color baseColor)
        {
            if (btn == null) return;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(0.60f, 0.60f, 0.64f, 0.75f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
        }
    }
}
