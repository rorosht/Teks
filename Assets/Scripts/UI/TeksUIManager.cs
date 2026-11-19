using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TeksMaster.Core;
using TeksMaster.Characters;
using TeksMaster.Gameplay;
using TeksMaster.Shop;
using TeksMaster.Visuals;
using TeksMaster.World;

namespace TeksMaster.UI
{
    public class TeksUIManager : MonoBehaviour
    {
        public static TeksUIManager Instance { get; private set; }

        private Font _font;
        private Canvas _canvas;

        // Top Left HUD
        private Text _coinsText;
        private Text _genderButtonText;
        private Image _hudAvatarPortrait;
        private Text _hudProfileNameRank;
        private Text _hudStreakBadge;
        private Text _questsBtnLabel;
        private Text _proximityPromptText;
        private GameObject _proximityPromptPanel;

        // Start Screen (Title / Nickname / Gender)
        private GameObject _startScreenRoot;
        private InputField _nicknameInput;
        private Image _startPreviewPortrait;
        private Image _startMaleBtnImg;
        private Image _startFemaleBtnImg;
        private PlayerGender _pendingStartGender = PlayerGender.Male;

        // Quests Modal
        private GameObject _questsModal;
        private Transform _questsListContent;

        // Barangay Map / Fast Travel Modal
        private GameObject _mapModal;
        private Text _zonePillText;
        private string _lastDisplayedZone = string.Empty;

        // Top Right HUD
        private Text _shopTimerText;

        // Bottom Right "MY PAMATO" HUD Deck
        private GameObject _pamatoPanel;
        private GameObject _pamatoMinimizedBtn;
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
        private CurvedFlickGauge _flickGauge;
        private Text _matchBannerTitle;
        private Text _matchBannerSub;
        private GameObject _roundResultPanel;
        private Text _roundResultTitle;
        private Text _roundResultSubtitle;
        private Text _roundResultCardsState;
        private GameObject _roundButtonsRow;
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
        private Transform _shopSlotsContainer;
        private Transform _packRevealContainer;
        private Text _packRevealHeader;

        private GameObject _wagerModal;
        private Text _wagerChallengeQuoteText;
        private Text _wagerOppName;
        private Text _wagerOppCardText;
        private Image _wagerOppCardImg;
        private Image _wagerPlayerCardImg;
        private Text _wagerPlayerCardText;
        private Text _wagerAmountText;
        private Text _wagerBonusInfoText;
        private int _selectedWagerAmount = 100;
        private int _wagerCardCycleIndex = 0;

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
            CreateTopCenterZoneBar();
            CreateProximityPrompt();
            CreateBottomRightPamatoPanel();
            CreateMatchHud();
            CreateInventoryModal();
            CreateShopModal();
            CreateQuestsModal();
            CreateMapModal();
            CreateWagerModal();
            CreateStartScreen();
            CreateToastBanner();

            HookEvents();
            RefreshAllUI();
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
                var pill = CreateRoundedPanel("Tag_" + npc.NpcName, _npcTagsLayer, new Color(0.08f, 0.11f, 0.17f, 0.85f), new Vector2(185f, 34f));
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
        }

        private void CreateTopLeftHud()
        {
            // Player Profile & Inventory Card (Top-Left)
            var panel = CreateRoundedPanel("TopLeftHUD", _canvas.transform, new Color(0.11f, 0.16f, 0.24f, 0.94f), new Vector2(455f, 114f));
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(22f, -18f);
            AddOutline(panel.gameObject, new Color(0.32f, 0.42f, 0.58f), 2f);

            // Avatar Portrait Badge
            var avatarBox = CreateRoundedPanel("AvatarPortraitBox", panel, new Color(0.18f, 0.25f, 0.36f, 1f), new Vector2(88f, 88f));
            avatarBox.anchorMin = new Vector2(0f, 0.5f);
            avatarBox.anchorMax = new Vector2(0f, 0.5f);
            avatarBox.anchoredPosition = new Vector2(54f, 0f);
            _hudAvatarPortrait = CreateImage("AvatarPortrait", avatarBox, CharacterResourceGenerator.GetAvatarPortraitSprite(PlayerGender.Male));
            StretchFull(_hudAvatarPortrait.rectTransform, 3f);

            var invBtn = panel.gameObject.AddComponent<Button>();
            invBtn.onClick.AddListener(ToggleInventoryModal);

            _hudProfileNameRank = CreateText("ProfileNameRank", panel, "TEKSMASTER  •  Baguhan (Rookie)", 16, FontStyle.Bold, new Color(0.76f, 0.90f, 1.0f), TextAnchor.MiddleLeft);
            _hudProfileNameRank.rectTransform.anchorMin = new Vector2(0f, 1f);
            _hudProfileNameRank.rectTransform.anchorMax = new Vector2(1f, 1f);
            _hudProfileNameRank.rectTransform.offsetMin = new Vector2(108f, -34f);
            _hudProfileNameRank.rectTransform.offsetMax = new Vector2(-10f, -6f);

            var title = CreateText("InvTitle", panel, "INVENTORY [I]", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.65f, 1f);
            title.rectTransform.offsetMin = new Vector2(108f, -66f);
            title.rectTransform.offsetMax = new Vector2(0f, -34f);

            _hudStreakBadge = CreateText("StreakBadge", panel, "", 14, FontStyle.Bold, new Color(1.0f, 0.52f, 0.16f), TextAnchor.MiddleRight);
            _hudStreakBadge.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            _hudStreakBadge.rectTransform.anchorMax = new Vector2(1f, 1f);
            _hudStreakBadge.rectTransform.offsetMin = new Vector2(0f, -66f);
            _hudStreakBadge.rectTransform.offsetMax = new Vector2(-12f, -34f);

            var coinIcon = CreateImage("CoinIcon", panel, CardArtGenerator.GetUIIconSprite("coin"));
            coinIcon.rectTransform.anchorMin = new Vector2(0f, 0f);
            coinIcon.rectTransform.anchorMax = new Vector2(0f, 0f);
            coinIcon.rectTransform.anchoredPosition = new Vector2(124f, 26f);
            coinIcon.rectTransform.sizeDelta = new Vector2(28f, 28f);

            _coinsText = CreateText("CoinsValue", panel, "2,450 COINS", 22, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            _coinsText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _coinsText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _coinsText.rectTransform.offsetMin = new Vector2(146f, 8f);
            _coinsText.rectTransform.offsetMax = new Vector2(-10f, 44f);

            // Controls pill beneath TopLeftHUD (Avatar Gender, Fusion Lab, Quests, Quick Challenge)
            var subBar = CreateRoundedPanel("ControlsBar", _canvas.transform, new Color(0.09f, 0.13f, 0.20f, 0.88f), new Vector2(680f, 44f));
            subBar.anchorMin = new Vector2(0f, 1f);
            subBar.anchorMax = new Vector2(0f, 1f);
            subBar.pivot = new Vector2(0f, 1f);
            subBar.anchoredPosition = new Vector2(22f, -140f);

            var genderBtn = CreateButton("GenderToggleBtn", subBar, "AVATAR: MALE [C]", new Color(0.20f, 0.34f, 0.56f), 13, () =>
            {
                PlayerInventory.Instance?.ToggleGender();
            });
            var gRt = genderBtn.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0f, 0.5f);
            gRt.anchorMax = new Vector2(0f, 0.5f);
            gRt.anchoredPosition = new Vector2(90f, 0f);
            gRt.sizeDelta = new Vector2(162f, 32f);
            _genderButtonText = genderBtn.GetComponentInChildren<Text>();

            var fusionQuickBtn = CreateButton("FusionQuickBtn", subBar, "FUSION LAB [F]", new Color(0.58f, 0.22f, 0.84f), 13, OpenFusionLabDirect);
            var fRt = fusionQuickBtn.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0f, 0.5f);
            fRt.anchorMax = new Vector2(0f, 0.5f);
            fRt.anchoredPosition = new Vector2(252f, 0f);
            fRt.sizeDelta = new Vector2(142f, 32f);

            var questsBtn = CreateButton("QuestsBtn", subBar, "QUESTS [Q]", new Color(0.84f, 0.50f, 0.12f), 13, ToggleQuestsModal);
            var qrRt = questsBtn.GetComponent<RectTransform>();
            qrRt.anchorMin = new Vector2(0f, 0.5f);
            qrRt.anchorMax = new Vector2(0f, 0.5f);
            qrRt.anchoredPosition = new Vector2(396f, 0f);
            qrRt.sizeDelta = new Vector2(128f, 32f);
            _questsBtnLabel = questsBtn.GetComponentInChildren<Text>();

            var quickBtn = CreateButton("QuickMatchBtn", subBar, "QUICK CHALLENGE", new Color(0.18f, 0.62f, 0.32f), 13, () =>
            {
                if (TeksMatchManager.Instance == null || TeksMatchManager.Instance.State != MatchState.Idle) return;
                NpcOpponent target = PlayerController.Instance != null ? PlayerController.Instance.NearestOpponent : null;
                if (target == null && BarangayCourtBuilder.Instance != null && BarangayCourtBuilder.Instance.SpawnedOpponents.Count > 0)
                {
                    target = BarangayCourtBuilder.Instance.SpawnedOpponents[0];
                }
                if (target != null)
                {
                    CloseAllModals();
                    TeksMatchManager.Instance.StartChallenge(target);
                }
            });
            var qRt = quickBtn.GetComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0.5f);
            qRt.anchorMax = new Vector2(1f, 0.5f);
            qRt.anchoredPosition = new Vector2(-94f, 0f);
            qRt.sizeDelta = new Vector2(172f, 32f);
        }

        private void CreateTopRightHud()
        {
            var container = new GameObject("TopRightHUD", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(_canvas.transform, false);
            container.anchorMin = new Vector2(1f, 1f);
            container.anchorMax = new Vector2(1f, 1f);
            container.pivot = new Vector2(1f, 1f);
            container.anchoredPosition = new Vector2(-22f, -20f);
            container.sizeDelta = new Vector2(360f, 132f);

            var shopPanel = CreateRoundedPanel("ShopBtnPanel", container, new Color(0.11f, 0.16f, 0.24f, 0.94f), new Vector2(310f, 88f));
            shopPanel.anchorMin = new Vector2(1f, 1f);
            shopPanel.anchorMax = new Vector2(1f, 1f);
            shopPanel.pivot = new Vector2(1f, 1f);
            shopPanel.anchoredPosition = Vector2.zero;
            AddOutline(shopPanel.gameObject, new Color(0.32f, 0.42f, 0.58f), 2f);

            var shopBtn = shopPanel.gameObject.AddComponent<Button>();
            shopBtn.onClick.AddListener(ToggleShopModal);

            var shopLabel = CreateText("ShopTitle", shopPanel, "SHOP [P]", 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            shopLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            shopLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            shopLabel.rectTransform.offsetMin = new Vector2(12f, 6f);
            shopLabel.rectTransform.offsetMax = new Vector2(192f, -6f);

            var iconBg = CreateRoundedPanel("ShopIconBox", shopPanel, new Color(0.18f, 0.25f, 0.36f, 1f), new Vector2(84f, 74f));
            iconBg.anchorMin = new Vector2(1f, 0.5f);
            iconBg.anchorMax = new Vector2(1f, 0.5f);
            iconBg.anchoredPosition = new Vector2(-50f, 0f);

            var shopIcon = CreateImage("ShopIcon", iconBg, CardArtGenerator.GetUIIconSprite("shop"));
            StretchFull(shopIcon.rectTransform, 6f);

            // Live 5M Refresh + Timer badge directly under Shop button
            var timerPill = CreateRoundedPanel("ShopTimerPill", container, new Color(0.08f, 0.12f, 0.19f, 0.94f), new Vector2(280f, 38f));
            timerPill.anchorMin = new Vector2(1f, 1f);
            timerPill.anchorMax = new Vector2(1f, 1f);
            timerPill.pivot = new Vector2(1f, 1f);
            timerPill.anchoredPosition = new Vector2(0f, -94f);
            AddOutline(timerPill.gameObject, new Color(0.28f, 0.78f, 0.44f), 1.5f);

            _shopTimerText = CreateText("ShopTimerLabel", timerPill, "5M REFRESH  |  04:59", 17, FontStyle.Bold, new Color(0.58f, 1.0f, 0.52f), TextAnchor.MiddleCenter);
            StretchFull(_shopTimerText.rectTransform, 4f);
        }

        private void CreateProximityPrompt()
        {
            _proximityPromptPanel = CreateRoundedPanel("ProximityPrompt", _canvas.transform, new Color(0.07f, 0.10f, 0.16f, 0.90f), new Vector2(460f, 44f)).gameObject;
            var rt = _proximityPromptPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 44f);
            AddOutline(_proximityPromptPanel, new Color(0.98f, 0.80f, 0.24f), 2f);

            _proximityPromptText = CreateText("PromptTxt", rt, "[E] CHALLENGE", 20, FontStyle.Bold, new Color(1.0f, 0.92f, 0.45f), TextAnchor.MiddleCenter);
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

            // Right half: "MY PAMATO" title + 4 stat bars + Green UPGRADE button
            var header = CreateText("MyPamatoHeader", rt, "MY PAMATO", 22, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.offsetMin = new Vector2(278f, -38f);
            header.rectTransform.offsetMax = new Vector2(-38f, -8f);

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
            var banner = CreateRoundedPanel("MatchTopBanner", rootRt, new Color(0.08f, 0.12f, 0.20f, 0.90f), new Vector2(580f, 74f));
            banner.anchorMin = new Vector2(0.5f, 1f);
            banner.anchorMax = new Vector2(0.5f, 1f);
            banner.anchoredPosition = new Vector2(0f, -54f);
            AddOutline(banner.gameObject, new Color(0.95f, 0.78f, 0.22f), 2f);

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
                TeksMatchManager.Instance?.Rematch();
            });
            var rmRt = rematchBtn.GetComponent<RectTransform>();
            rmRt.anchoredPosition = new Vector2(-120f, 0f);
            rmRt.sizeDelta = new Vector2(200f, 44f);

            var backBtn = CreateButton("BackToCourtBtn", rowRt, "BACK TO COURT", new Color(0.28f, 0.38f, 0.56f), 18, () =>
            {
                TeksMatchManager.Instance?.CancelChallenge();
            });
            var bkRt = backBtn.GetComponent<RectTransform>();
            bkRt.anchoredPosition = new Vector2(120f, 0f);
            bkRt.sizeDelta = new Vector2(210f, 44f);

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

            var closeBtn = CreateButton("CloseInvBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
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

            var fusionTabBtn = CreateButton("TabFusionBtn", window, "CARD FUSION LAB (SIMILAR & RECIPES)", new Color(0.45f, 0.22f, 0.68f), 15, () => SwitchInventoryTab(true));
            var ftRt = fusionTabBtn.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0f, 1f);
            ftRt.anchorMax = new Vector2(0f, 1f);
            ftRt.anchoredPosition = new Vector2(512f, -84f);
            ftRt.sizeDelta = new Vector2(334f, 36f);
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
            _shopModal = CreateModalBackdrop("ShopModal", new Vector2(1080f, 745f), out RectTransform window);

            var header = CreateText("ShopModalHeader", window, "SARI-SARI STORE (ALING NENA'S TEKS)", 25, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.58f, 1f);
            header.rectTransform.offsetMin = new Vector2(28f, -52f);
            header.rectTransform.offsetMax = new Vector2(0f, -10f);

            _shopModalTimerText = CreateText("ShopModalTimer", window, "NEW CARDS IN 04:59", 20, FontStyle.Bold, new Color(0.55f, 1.0f, 0.45f), TextAnchor.MiddleRight);
            _shopModalTimerText.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            _shopModalTimerText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _shopModalTimerText.rectTransform.offsetMin = new Vector2(0f, -44f);
            _shopModalTimerText.rectTransform.offsetMax = new Vector2(-84f, -10f);

            _shopModalCoinsText = CreateText("ShopModalCoins", window, "2,450 COINS", 17, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleRight);
            _shopModalCoinsText.rectTransform.anchorMin = new Vector2(0.55f, 1f);
            _shopModalCoinsText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _shopModalCoinsText.rectTransform.offsetMin = new Vector2(0f, -68f);
            _shopModalCoinsText.rectTransform.offsetMax = new Vector2(-84f, -40f);

            var closeBtn = CreateButton("CloseShopBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            // Aling Nena's Live Greeting & Utility Row
            var vendorBox = CreateRoundedPanel("VendorGreetingBox", window, new Color(0.14f, 0.22f, 0.28f, 0.95f), new Vector2(560f, 36f));
            vendorBox.anchorMin = new Vector2(0f, 1f);
            vendorBox.anchorMax = new Vector2(0f, 1f);
            vendorBox.anchoredPosition = new Vector2(308f, -84f);
            _shopVendorBannerText = CreateText("VendorTxt", vendorBox, "Aling Nena: \"Bili na! May -30% Sulit Sale ngayon!\"", 14, FontStyle.Italic, new Color(1.0f, 0.94f, 0.72f), TextAnchor.MiddleLeft);
            StretchFull(_shopVendorBannerText.rectTransform, 10f);

            _shopDailyFreeBtn = CreateButton("DailyBaryaBtn", window, "FREE +200 BARYA", new Color(0.86f, 0.56f, 0.10f), 13, () =>
            {
                if (ShopManager.Instance != null && ShopManager.Instance.TryClaimDailyBarya(out string msg))
                {
                    ShowToast(msg);
                    PopulateShopModal();
                }
                else
                {
                    ShowToast("Already claimed Aling Nena's Free Barya today!");
                }
            });
            var dfRt = _shopDailyFreeBtn.GetComponent<RectTransform>();
            dfRt.anchorMin = new Vector2(1f, 1f);
            dfRt.anchorMax = new Vector2(1f, 1f);
            dfRt.anchoredPosition = new Vector2(-386f, -84f);
            dfRt.sizeDelta = new Vector2(162f, 34f);
            _shopDailyFreeBtnText = _shopDailyFreeBtn.GetComponentInChildren<Text>();

            var sellAllBtn = CreateButton("SellAllDupesBtn", window, "SELL EXTRA DUPES", new Color(0.62f, 0.34f, 0.16f), 13, () =>
            {
                if (PlayerInventory.Instance != null && PlayerInventory.Instance.TrySellAllExtraDuplicates(out int sold, out int earned))
                {
                    ShowToast($"Sold {sold} duplicate cards for +{earned} Barya Coins!");
                    PopulateShopModal();
                }
                else
                {
                    ShowToast("No extra duplicate cards to sell!");
                }
            });
            var saRt = sellAllBtn.GetComponent<RectTransform>();
            saRt.anchorMin = new Vector2(1f, 1f);
            saRt.anchorMax = new Vector2(1f, 1f);
            saRt.anchoredPosition = new Vector2(-212f, -84f);
            saRt.sizeDelta = new Vector2(168f, 34f);

            var refreshBtn = CreateButton("ManualRefreshBtn", window, "REFRESH (50c)", new Color(0.24f, 0.44f, 0.76f), 13, () =>
            {
                if (ShopManager.Instance != null && ShopManager.Instance.TryManualRefresh(out string msg))
                {
                    ShowToast(msg);
                    PopulateShopModal();
                }
                else
                {
                    ShowToast("Need 50 Coins to refresh early!");
                }
            });
            var rfRt = refreshBtn.GetComponent<RectTransform>();
            rfRt.anchorMin = new Vector2(1f, 1f);
            rfRt.anchorMax = new Vector2(1f, 1f);
            rfRt.anchoredPosition = new Vector2(-72f, -84f);
            rfRt.sizeDelta = new Vector2(118f, 34f);

            // Rotating Singles Row Header
            var singlesTitle = CreateText("SinglesTitle", window, "5-MINUTE ROTATING SINGLES (Includes a -30% SULIT SALE & Slot 4 SUPER SLOT Rare+)", 15, FontStyle.Bold, new Color(0.82f, 0.90f, 1.0f), TextAnchor.MiddleLeft);
            singlesTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            singlesTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            singlesTitle.rectTransform.offsetMin = new Vector2(28f, -132f);
            singlesTitle.rectTransform.offsetMax = new Vector2(-28f, -106f);

            // 4 Single Slots Container
            var slotsGo = new GameObject("SlotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slotsGo.transform.SetParent(window, false);
            var slotsRt = slotsGo.GetComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0f, 1f);
            slotsRt.anchorMax = new Vector2(1f, 1f);
            slotsRt.offsetMin = new Vector2(26f, -396f);
            slotsRt.offsetMax = new Vector2(-26f, -136f);
            var hlg = slotsGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            _shopSlotsContainer = slotsGo.transform;

            // Booster Packs Section (3 Tiers — Always Available)
            var boosterTitle = CreateText("BoosterTitle", window, "BOOSTER PACKS & MYTHIC BOX (Always Available - No Timer!)", 16, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            boosterTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            boosterTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            boosterTitle.rectTransform.offsetMin = new Vector2(28f, -432f);
            boosterTitle.rectTransform.offsetMax = new Vector2(-28f, -404f);

            var packsRow = new GameObject("PacksRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            packsRow.transform.SetParent(window, false);
            var prRt = packsRow.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f);
            prRt.anchorMax = new Vector2(1f, 1f);
            prRt.offsetMin = new Vector2(26f, -504f);
            prRt.offsetMax = new Vector2(-26f, -436f);
            var pHlg = packsRow.GetComponent<HorizontalLayoutGroup>();
            pHlg.spacing = 14f;
            pHlg.childControlWidth = true;
            pHlg.childControlHeight = true;
            pHlg.childForceExpandWidth = true;
            pHlg.childForceExpandHeight = true;

            CreateButton("KalyePackBtn", packsRow.transform, "KALYE PACK (150 Coins)\n3 Random Teks Cards", new Color(0.18f, 0.56f, 0.86f), 15, () => OpenBoosterPack(false));
            CreateButton("AlamatPackBtn", packsRow.transform, "ALAMAT SUPER PACK (400 Coins)\n3 Cards (Guaranteed Rare+!)", new Color(0.62f, 0.24f, 0.86f), 15, () => OpenBoosterPack(true));
            CreateButton("MythicBoxBtn", packsRow.transform, "BARANGAY MYTHIC BOX (850 Coins)\n5 Cards (Guaranteed Epic/Legend!)", new Color(0.86f, 0.52f, 0.08f), 14, OpenMythicBox);

            // Pack Reveal Tray
            _packRevealHeader = CreateText("PackRevealHeader", window, "Open a Booster Pack or Mythic Box to reveal your new Teks cards here!", 15, FontStyle.Italic, new Color(0.82f, 0.86f, 0.94f), TextAnchor.MiddleLeft);
            _packRevealHeader.rectTransform.anchorMin = new Vector2(0f, 0f);
            _packRevealHeader.rectTransform.anchorMax = new Vector2(1f, 0f);
            _packRevealHeader.rectTransform.offsetMin = new Vector2(28f, 154f);
            _packRevealHeader.rectTransform.offsetMax = new Vector2(-28f, 184f);

            var revealGo = new GameObject("PackRevealRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            revealGo.transform.SetParent(window, false);
            var revRt = revealGo.GetComponent<RectTransform>();
            revRt.anchorMin = new Vector2(0f, 0f);
            revRt.anchorMax = new Vector2(1f, 0f);
            revRt.offsetMin = new Vector2(28f, 18f);
            revRt.offsetMax = new Vector2(-28f, 148f);
            var revHlg = revealGo.GetComponent<HorizontalLayoutGroup>();
            revHlg.spacing = 10f;
            revHlg.childControlWidth = true;
            revHlg.childControlHeight = true;
            revHlg.childForceExpandWidth = true;
            revHlg.childForceExpandHeight = true;
            _packRevealContainer = revealGo.transform;

            _shopModal.SetActive(false);
        }

        private void CreateQuestsModal()
        {
            _questsModal = CreateModalBackdrop("QuestsModal", new Vector2(680f, 430f), out RectTransform window);

            var header = CreateText("QuestsHeader", window, "BARANGAY MISSIONS & DAILY REWARDS", 24, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.85f, 1f);
            header.rectTransform.offsetMin = new Vector2(26f, -56f);
            header.rectTransform.offsetMax = new Vector2(0f, -12f);

            var closeBtn = CreateButton("CloseQuestsBtn", window, "X", new Color(0.84f, 0.20f, 0.20f), 18, CloseAllModals);
            var closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-34f, -34f);
            closeRt.sizeDelta = new Vector2(40f, 40f);

            var listGo = new GameObject("QuestsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(window, false);
            var lRt = listGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(26f, 24f);
            lRt.offsetMax = new Vector2(-26f, -74f);
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            _questsListContent = listGo.transform;

            _questsModal.SetActive(false);
        }

        private void CreateStartScreen()
        {
            _startScreenRoot = CreateModalBackdrop("StartGameScreen", new Vector2(680f, 540f), out RectTransform window);
            _startScreenRoot.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.10f, 0.55f);

            var title = CreateText("GameTitle", window, "3D TEKS MASTER", 42, FontStyle.Bold, new Color(1.0f, 0.85f, 0.22f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(20f, -72f);
            title.rectTransform.offsetMax = new Vector2(-20f, -14f);

            var sub = CreateText("GameSub", window, "BARANGAY SHOWDOWN  •  Flick, Fuse & Rule the Court!", 17, FontStyle.Bold, new Color(0.76f, 0.90f, 1.0f), TextAnchor.MiddleCenter);
            sub.rectTransform.anchorMin = new Vector2(0f, 1f);
            sub.rectTransform.anchorMax = new Vector2(1f, 1f);
            sub.rectTransform.offsetMin = new Vector2(20f, -104f);
            sub.rectTransform.offsetMax = new Vector2(-20f, -72f);

            // Avatar Preview Box
            var previewBox = CreateRoundedPanel("StartAvatarBox", window, new Color(0.14f, 0.20f, 0.30f), new Vector2(150f, 150f));
            previewBox.anchoredPosition = new Vector2(0f, 80f);
            _startPreviewPortrait = CreateImage("StartAvatarImg", previewBox, CharacterResourceGenerator.GetAvatarPortraitSprite(PlayerGender.Male));
            StretchFull(_startPreviewPortrait.rectTransform, 6f);

            // Nickname Label + InputField
            var nickLbl = CreateText("NickLbl", window, "SET YOUR NICKNAME:", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            nickLbl.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            nickLbl.rectTransform.sizeDelta = new Vector2(360f, 26f);

            var inputBg = CreateRoundedPanel("NicknameInputBg", window, new Color(0.06f, 0.09f, 0.16f, 1f), new Vector2(360f, 44f));
            inputBg.anchoredPosition = new Vector2(0f, -54f);
            AddOutline(inputBg.gameObject, new Color(0.42f, 0.62f, 0.92f), 2f);

            _nicknameInput = inputBg.gameObject.AddComponent<InputField>();
            _nicknameInput.characterLimit = 18;
            var placeholderTxt = CreateText("Placeholder", inputBg, "Enter nickname...", 18, FontStyle.Italic, new Color(0.55f, 0.60f, 0.70f), TextAnchor.MiddleCenter);
            StretchFull(placeholderTxt.rectTransform, 8f);
            var inputText = CreateText("Text", inputBg, "", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(inputText.rectTransform, 8f);
            _nicknameInput.textComponent = inputText;
            _nicknameInput.placeholder = placeholderTxt;
            _nicknameInput.text = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerNickname : "TeksMaster";

            // Gender Picker Buttons
            var genLbl = CreateText("GenderLbl", window, "CHOOSE YOUR AVATAR:", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            genLbl.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            genLbl.rectTransform.sizeDelta = new Vector2(360f, 26f);

            var maleBtn = CreateButton("StartMaleBtn", window, "MALE (TOTOY)", new Color(0.18f, 0.50f, 0.88f), 16, () => SelectStartGender(PlayerGender.Male));
            var mRt = maleBtn.GetComponent<RectTransform>();
            mRt.anchoredPosition = new Vector2(-106f, -138f);
            mRt.sizeDelta = new Vector2(196f, 42f);
            _startMaleBtnImg = maleBtn.GetComponent<Image>();

            var femaleBtn = CreateButton("StartFemaleBtn", window, "FEMALE (NENE)", new Color(0.32f, 0.22f, 0.36f), 16, () => SelectStartGender(PlayerGender.Female));
            var fRt = femaleBtn.GetComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(106f, -138f);
            fRt.sizeDelta = new Vector2(196f, 42f);
            _startFemaleBtnImg = femaleBtn.GetComponent<Image>();

            // Play Button
            var playBtn = CreateButton("StartPlayBtn", window, "PLAY BARANGAY SHOWDOWN!", new Color(0.18f, 0.76f, 0.34f), 22, OnStartPlayClicked);
            var pRt = playBtn.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0f, -210f);
            pRt.sizeDelta = new Vector2(390f, 56f);
            AddOutline(playBtn.gameObject, new Color(0.75f, 1.0f, 0.65f), 2f);

            var initialGender = PlayerInventory.Instance != null ? PlayerInventory.Instance.SelectedGender : PlayerGender.Male;
            SelectStartGender(initialGender);

            // Lock player & orbit title camera until user presses PLAY
            if (PlayerController.Instance != null) PlayerController.Instance.MovementLocked = true;
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow != null) camFollow.IsTitleCameraActive = true;
            }
            _startScreenRoot.SetActive(true);
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

            if (_startScreenRoot != null) _startScreenRoot.SetActive(false);
            if (PlayerController.Instance != null) PlayerController.Instance.MovementLocked = false;
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow != null) camFollow.IsTitleCameraActive = false;
            }

            string finalNick = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerNickname : "TeksMaster";
            ShowToast($"Maligayang pagdating, {finalNick}! Walk up to any kid & press [E] to play Teks!");
            RefreshAllUI();
        }

        private void CreateWagerModal()
        {
            _wagerModal = CreateModalBackdrop("WagerModal", new Vector2(760f, 525f), out RectTransform window);

            var title = CreateText("WagerTitle", window, "CHALLENGE & WAGER SETUP", 26, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(20f, -50f);
            title.rectTransform.offsetMax = new Vector2(-20f, -10f);

            var quoteBox = CreateRoundedPanel("WagerQuoteBox", window, new Color(0.14f, 0.22f, 0.34f, 0.96f), new Vector2(690f, 40f));
            quoteBox.anchorMin = new Vector2(0.5f, 1f);
            quoteBox.anchorMax = new Vector2(0.5f, 1f);
            quoteBox.anchoredPosition = new Vector2(0f, -72f);
            AddOutline(quoteBox.gameObject, new Color(1.0f, 0.80f, 0.26f), 1.5f);
            _wagerChallengeQuoteText = CreateText("WagerQuoteTxt", quoteBox, "AI Gelo: \"Hoy! Teks tayo!\"", 16, FontStyle.Italic, new Color(1.0f, 0.95f, 0.72f), TextAnchor.MiddleCenter);
            StretchFull(_wagerChallengeQuoteText.rectTransform, 6f);

            // Player side
            var playerBox = CreateRoundedPanel("WagerPlayerBox", window, new Color(0.14f, 0.20f, 0.30f), new Vector2(260f, 240f));
            playerBox.anchoredPosition = new Vector2(-190f, 36f);
            CreateText("PlayerPamatoLbl", playerBox, "YOUR PAMATO", 16, FontStyle.Bold, new Color(0.45f, 0.95f, 0.55f), TextAnchor.MiddleCenter)
                .rectTransform.anchoredPosition = new Vector2(0f, 96f);

            _wagerPlayerCardImg = CreateImage("WagerPlayerImg", playerBox, null);
            _wagerPlayerCardImg.rectTransform.sizeDelta = new Vector2(105f, 140f);
            _wagerPlayerCardImg.rectTransform.anchoredPosition = new Vector2(0f, 10f);

            _wagerPlayerCardText = CreateText("WagerPlayerCardTxt", playerBox, "HERO 5★", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _wagerPlayerCardText.rectTransform.anchoredPosition = new Vector2(0f, -84f);
            _wagerPlayerCardText.rectTransform.sizeDelta = new Vector2(240f, 44f);

            var cycleBtn = CreateButton("CyclePamatoBtn", playerBox, "< SWITCH CARD >", new Color(0.24f, 0.42f, 0.68f), 13, () =>
            {
                var inv = PlayerInventory.Instance;
                if (inv == null || inv.OwnedCards.Count == 0) return;
                _wagerCardCycleIndex = (_wagerCardCycleIndex + 1) % inv.OwnedCards.Count;
                inv.EquipPamato(inv.OwnedCards[_wagerCardCycleIndex].CardId);
                PopulateWagerModal();
            });
            var cyRt = cycleBtn.GetComponent<RectTransform>();
            cyRt.anchoredPosition = new Vector2(0f, -112f);
            cyRt.sizeDelta = new Vector2(170f, 28f);

            CreateText("VsText", window, "VS", 36, FontStyle.Bold, new Color(1.0f, 0.80f, 0.20f), TextAnchor.MiddleCenter)
                .rectTransform.anchoredPosition = new Vector2(0f, 42f);

            // Opponent side
            var oppBox = CreateRoundedPanel("WagerOppBox", window, new Color(0.14f, 0.20f, 0.30f), new Vector2(260f, 240f));
            oppBox.anchoredPosition = new Vector2(190f, 36f);
            _wagerOppName = CreateText("WagerOppLbl", oppBox, "AI GELO (Med)", 16, FontStyle.Bold, new Color(1.0f, 0.66f, 0.34f), TextAnchor.MiddleCenter);
            _wagerOppName.rectTransform.anchoredPosition = new Vector2(0f, 96f);
            _wagerOppName.rectTransform.sizeDelta = new Vector2(240f, 30f);

            _wagerOppCardImg = CreateImage("WagerOppImg", oppBox, null);
            _wagerOppCardImg.rectTransform.sizeDelta = new Vector2(105f, 140f);
            _wagerOppCardImg.rectTransform.anchoredPosition = new Vector2(0f, 10f);

            _wagerOppCardText = CreateText("WagerOppCardTxt", oppBox, "KID KIDLAT 3★", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _wagerOppCardText.rectTransform.anchoredPosition = new Vector2(0f, -84f);
            _wagerOppCardText.rectTransform.sizeDelta = new Vector2(240f, 44f);

            // Coin wager selector row
            _wagerAmountText = CreateText("WagerAmountTxt", window, "WAGER: 100 BARYA COINS", 20, FontStyle.Bold, new Color(1.0f, 0.88f, 0.32f), TextAnchor.MiddleCenter);
            _wagerAmountText.rectTransform.anchoredPosition = new Vector2(0f, -108f);
            _wagerAmountText.rectTransform.sizeDelta = new Vector2(540f, 34f);

            int[] wagerOptions = { 50, 100, 200, 500 };
            for (int i = 0; i < wagerOptions.Length; i++)
            {
                int val = wagerOptions[i];
                var b = CreateButton("Wager_" + val, window, val + " COINS", new Color(0.22f, 0.32f, 0.48f), 14, () =>
                {
                    int maxCoins = PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : 100;
                    _selectedWagerAmount = Mathf.Clamp(val, 25, Mathf.Max(25, maxCoins));
                    RefreshWagerText();
                });
                var bRt = b.GetComponent<RectTransform>();
                bRt.anchoredPosition = new Vector2(-180f + i * 120f, -142f);
                bRt.sizeDelta = new Vector2(108f, 32f);
            }

            _wagerBonusInfoText = CreateText("WagerBonusTxt", window, "", 14, FontStyle.Italic, new Color(0.68f, 0.94f, 0.72f), TextAnchor.MiddleCenter);
            _wagerBonusInfoText.rectTransform.anchoredPosition = new Vector2(0f, -174f);
            _wagerBonusInfoText.rectTransform.sizeDelta = new Vector2(680f, 26f);

            var startBtn = CreateButton("StartMatchBtn", window, "START FLICK MATCH!", new Color(0.18f, 0.74f, 0.32f), 20, () =>
            {
                _wagerModal.SetActive(false);
                TeksMatchManager.Instance?.BeginFlickMatch(_selectedWagerAmount);
            });
            var stRt = startBtn.GetComponent<RectTransform>();
            stRt.anchoredPosition = new Vector2(-95f, -210f);
            stRt.sizeDelta = new Vector2(240f, 44f);

            var cancelBtn = CreateButton("CancelWagerBtn", window, "CANCEL", new Color(0.55f, 0.24f, 0.26f), 18, () =>
            {
                _wagerModal.SetActive(false);
                TeksMatchManager.Instance?.CancelChallenge();
            });
            var caRt = cancelBtn.GetComponent<RectTransform>();
            caRt.anchoredPosition = new Vector2(140f, -210f);
            caRt.sizeDelta = new Vector2(160f, 44f);

            _wagerModal.SetActive(false);
        }

        private void CreateToastBanner()
        {
            _toastPanel = CreateRoundedPanel("ToastBanner", _canvas.transform, new Color(0.06f, 0.10f, 0.16f, 0.95f), new Vector2(620f, 46f)).gameObject;
            var rt = _toastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -120f);
            AddOutline(_toastPanel, new Color(0.95f, 0.78f, 0.24f), 2f);

            _toastText = CreateText("ToastLabel", rt, "", 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(_toastText.rectTransform, 8f);
            _toastPanel.SetActive(false);
        }

        private void HookEvents()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshAllUI;
                PlayerInventory.Instance.OnInventoryChanged += RefreshAllUI;
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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshAllUI;
            if (ShopManager.Instance != null) ShopManager.Instance.OnShopUpdated -= RefreshShopDisplay;
            if (TeksMatchManager.Instance != null) TeksMatchManager.Instance.OnMatchStateChanged -= HandleMatchStateChanged;
        }

        private void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
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
                string currentZone = BarangayCourtBuilder.GetZoneNameAtPosition(PlayerController.Instance.transform.position);
                if (currentZone != _lastDisplayedZone)
                {
                    _lastDisplayedZone = currentZone;
                    _zonePillText.text = $"AREA: {currentZone}   |   [M] BARANGAY MAP";
                }
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
            if (_startScreenRoot != null && _startScreenRoot.activeSelf) return;

            var matchState = TeksMatchManager.Instance != null ? TeksMatchManager.Instance.State : MatchState.Idle;
            if (matchState != MatchState.Idle)
            {
                if (kb.escapeKey.wasPressedThisFrame && matchState == MatchState.WagerScreen)
                {
                    _wagerModal.SetActive(false);
                    TeksMatchManager.Instance.CancelChallenge();
                }
                return;
            }

            if (kb.iKey.wasPressedThisFrame) ToggleInventoryModal();
            if (kb.fKey.wasPressedThisFrame) OpenFusionLabDirect();
            if (kb.qKey.wasPressedThisFrame) ToggleQuestsModal();
            if (kb.mKey.wasPressedThisFrame) ToggleMapModal();
            if (kb.pKey.wasPressedThisFrame) ToggleShopModal();
            if (kb.escapeKey.wasPressedThisFrame) CloseAllModals();

            if (kb.eKey.wasPressedThisFrame && !IsAnyModalOpen())
            {
                var pc = PlayerController.Instance;
                if (pc != null)
                {
                    if (pc.NearestOpponent != null)
                    {
                        TeksMatchManager.Instance?.StartChallenge(pc.NearestOpponent);
                    }
                    else if (pc.IsNearSariSariStore)
                    {
                        ToggleShopModal();
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

        private void PrepareFlipShowcase(TeksMatchManager mm)
        {
            if (_cardFlipShowcaseRoot == null) return;

            var inv = PlayerInventory.Instance;
            TeksCardDefinition pDef = null;
            var pOwned = inv != null ? inv.GetEquippedPamato(out pDef) : null;
            int pStars = pOwned != null ? pOwned.StarRank : 5;

            string oppCardId = mm.ActiveOpponent != null ? mm.ActiveOpponent.FavoriteCardId : "bakunawa";
            int oStars = mm.ActiveOpponent != null ? mm.ActiveOpponent.CardStarRank : 3;
            var oDef = TeksDatabase.GetCard(oppCardId);

            SetupFlipWidget(0, "YOUR PAMATO", pDef, pStars, 16.8f);
            SetupFlipWidget(1, mm.ActiveOpponent != null ? mm.ActiveOpponent.NpcName.ToUpper() : "OPPONENT", oDef, oStars, 18.2f);

            var showcaseImg = _cardFlipShowcaseRoot.GetComponent<Image>();
            if (showcaseImg != null)
            {
                showcaseImg.color = new Color(0.06f, 0.09f, 0.16f, 0.56f);
            }
            _cardFlipShowcaseRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 105f);
            _cardFlipShowcaseRoot.SetActive(true);
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
                if (entry.Npc == null || entry.Root == null) continue;

                if (!showTags)
                {
                    entry.Root.gameObject.SetActive(false);
                    continue;
                }

                Vector3 worldPos = entry.Npc.GetTagWorldPosition();
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                bool visible = screenPos.z > 0.2f && screenPos.x > -50f && screenPos.x < Screen.width + 50f && screenPos.y > -50f && screenPos.y < Screen.height + 50f;
                entry.Root.gameObject.SetActive(visible);

                if (visible)
                {
                    entry.Root.position = new Vector3(screenPos.x, screenPos.y, 0f);
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
            }
        }

        private void UpdateProximityPrompt()
        {
            if (_proximityPromptPanel == null) return;
            var pc = PlayerController.Instance;
            bool idle = TeksMatchManager.Instance == null || TeksMatchManager.Instance.State == MatchState.Idle;

            if (!idle || pc == null || IsAnyModalOpen())
            {
                _proximityPromptPanel.SetActive(false);
                return;
            }

            if (pc.NearestOpponent != null)
            {
                _proximityPromptPanel.SetActive(true);
                _proximityPromptText.text = $"[E] CHALLENGE {pc.NearestOpponent.GetDisplayTag().ToUpper()} ({pc.NearestOpponent.BaseCoinWager} COINS)";
            }
            else if (pc.IsNearSariSariStore)
            {
                _proximityPromptPanel.SetActive(true);
                _proximityPromptText.text = "[E] OPEN SARI-SARI STORE (BUY TEKS & PACKS)";
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
                _wagerModal.SetActive(false);
                _matchHudRoot.SetActive(false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);
                RefreshAllUI();
                return;
            }

            if (mm.State == MatchState.WagerScreen)
            {
                _matchHudRoot.SetActive(false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);
                _selectedWagerAmount = mm.CurrentCoinWager;
                PopulateWagerModal();
                _wagerModal.SetActive(true);
                return;
            }

            _wagerModal.SetActive(false);
            _matchHudRoot.SetActive(true);

            string oppLabel = mm.ActiveOpponent != null ? mm.ActiveOpponent.GetDisplayTag() : "Opponent";

            if (mm.State == MatchState.ReadyToFlick)
            {
                _flickGauge.gameObject.SetActive(true);
                _flickGauge.SetInteractive(true, "TAP [SPACE] TO FLICK!");
                _flickGauge.SetSweetSpotHalfWidth(mm.SweetSpotHalfWidth);
                _roundResultPanel.SetActive(false);
                if (_cardFlipShowcaseRoot != null) _cardFlipShowcaseRoot.SetActive(false);
                _matchBannerTitle.text = $"VS {oppLabel.ToUpper()}  |  POT: {mm.CurrentCoinWager} COINS";
                _matchBannerSub.text = "Press SPACE or click the gauge inside the GREEN Sweet Spot!";
            }
            else if (mm.State == MatchState.FlickInAir)
            {
                _flickGauge.gameObject.SetActive(false);
                _roundResultPanel.SetActive(false);
                _matchBannerTitle.text = mm.LastFlickRating;
                _matchBannerSub.text = "Cards flipping onto the concrete... (Chub = Face-Up, Cha = Face-Down)";
                PrepareFlipShowcase(mm);
            }
            else if (mm.State == MatchState.RoundComplete)
            {
                _flickGauge.gameObject.SetActive(false);
                _roundResultPanel.SetActive(true);

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

                string pSide = mm.PlayerCardFaceUp ? "CHUB (Face-Up)" : "CHA (Face-Down)";
                string oSide = mm.OpponentCardFaceUp ? "CHUB (Face-Up)" : "CHA (Face-Down)";
                _roundResultCardsState.text = $"Your Pamato: {pSide}   |   {oppLabel}: {oSide}";

                string banter = string.Empty;
                if (mm.ActiveOpponent != null)
                {
                    string line = mm.WasTablaLastRound
                        ? mm.ActiveOpponent.TablaBanter
                        : (mm.PlayerWonLastRound ? mm.ActiveOpponent.LoseBanter : mm.ActiveOpponent.WinBanter);
                    banter = $"\n{mm.ActiveOpponent.NpcName}: \"{line}\"";
                }
                _roundResultSubtitle.text = mm.RoundResultSubtitle + banter;
                _roundButtonsRow.SetActive(!mm.WasTablaLastRound);

                RefreshAllUI();
            }
        }

        private void RefreshAllUI()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;

            if (_coinsText != null) _coinsText.text = $"{inv.Coins:N0} COINS";
            if (_hudAvatarPortrait != null) _hudAvatarPortrait.sprite = CharacterResourceGenerator.GetAvatarPortraitSprite(inv.SelectedGender);
            if (_hudProfileNameRank != null) _hudProfileNameRank.text = $"{inv.PlayerNickname.ToUpper()}  •  {inv.GetBarangayRankTitle()}";
            if (_hudStreakBadge != null)
            {
                _hudStreakBadge.text = inv.WinStreak >= 2 ? $"HOT HAND x{inv.WinStreak}!" : (inv.MatchesWon > 0 ? $"WINS: {inv.MatchesWon}" : "");
            }
            if (_questsBtnLabel != null)
            {
                int claimable = inv.GetUnclaimedCompletedQuestCount();
                _questsBtnLabel.text = claimable > 0 ? $"QUESTS [Q] ({claimable}!)" : "QUESTS [Q]";
            }
            if (_genderButtonText != null)
            {
                _genderButtonText.text = inv.SelectedGender == PlayerGender.Male ? "AVATAR: MALE [C]" : "AVATAR: FEMALE [C]";
            }

            RefreshPamatoPanel();

            if (_inventoryModal != null && _inventoryModal.activeSelf)
            {
                if (_inventoryHeaderCoins != null) _inventoryHeaderCoins.text = $"{inv.Coins:N0} COINS";
                if (!_isFusionTabOpen) PopulateInventoryModal();
            }
            if (_shopModal != null && _shopModal.activeSelf) PopulateShopModal();
            if (_questsModal != null && _questsModal.activeSelf) PopulateQuestsModal();
        }

        private void RefreshPamatoPanel()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;

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
            CloseAllModals();
            if (next)
            {
                _inventoryModal.SetActive(true);
                SwitchInventoryTab(false);
            }
        }

        private void OpenFusionLabDirect()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            if (_inventoryModal.activeSelf && _isFusionTabOpen)
            {
                CloseAllModals();
                return;
            }
            CloseAllModals();
            _inventoryModal.SetActive(true);
            SwitchInventoryTab(true);
        }

        private void SwitchInventoryTab(bool fusionTab)
        {
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

            if (_inventoryHeaderCoins != null) _inventoryHeaderCoins.text = $"{inv.Coins:N0} COINS";

            for (int i = _fusionListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_fusionListContent.GetChild(i).gameObject);
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

            // Section 1: Similar-Card Rarity Fusion (3 Same-Rarity Dupes -> Next Rarity)
            AddFusionSectionHeader("1. SIMILAR-CARD FUSION (Combine 3 Duplicate Cards of Same Rarity -> Next Rarity!)");

            var simRow = new GameObject("SimilarFusionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            simRow.transform.SetParent(_fusionListContent, false);
            simRow.GetComponent<LayoutElement>().preferredHeight = 112f;
            var simHlg = simRow.GetComponent<HorizontalLayoutGroup>();
            simHlg.spacing = 12f;
            simHlg.childControlWidth = true;
            simHlg.childControlHeight = true;
            simHlg.childForceExpandWidth = true;
            simHlg.childForceExpandHeight = true;

            CardRarity[] sourceRarities = { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare, CardRarity.Epic };
            foreach (var srcRarity in sourceRarities)
            {
                TeksDatabase.TryGetNextRarity(srcRarity, out var nextRarity);
                bool canFuse = inv.CanFuseSimilarRarity(srcRarity, out int availDupes, out int coinCost, out string reason);
                Color srcCol = TeksDatabase.GetRarityColor(srcRarity);
                Color dstCol = TeksDatabase.GetRarityColor(nextRarity);

                var cardObj = new GameObject("SimFuse_" + srcRarity, typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(simRow.transform, false);
                var bgImg = cardObj.GetComponent<Image>();
                bgImg.sprite = CardArtGenerator.GetRoundedPanelSprite();
                bgImg.type = Image.Type.Sliced;
                bgImg.color = new Color(0.12f, 0.17f, 0.27f, 1f);
                AddOutline(cardObj, canFuse ? dstCol : srcCol, 2f);

                var rt = cardObj.GetComponent<RectTransform>();
                var topLbl = CreateText("Title", rt, $"3x {TeksDatabase.GetRarityName(srcRarity).ToUpper()} -> {TeksDatabase.GetRarityName(nextRarity).ToUpper()}", 13, FontStyle.Bold, dstCol, TextAnchor.MiddleCenter);
                topLbl.rectTransform.anchorMin = new Vector2(0f, 0.64f);
                topLbl.rectTransform.anchorMax = new Vector2(1f, 0.96f);
                topLbl.rectTransform.offsetMin = Vector2.zero;
                topLbl.rectTransform.offsetMax = Vector2.zero;

                var countLbl = CreateText("Count", rt, $"Duplicates: {availDupes} / 3", 13, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
                countLbl.rectTransform.anchorMin = new Vector2(0f, 0.40f);
                countLbl.rectTransform.anchorMax = new Vector2(1f, 0.66f);
                countLbl.rectTransform.offsetMin = Vector2.zero;
                countLbl.rectTransform.offsetMax = Vector2.zero;

                var capturedRarity = srcRarity;
                var fuseBtn = CreateButton("SimBtn", rt, canFuse ? $"FUSE ({coinCost}c)" : reason, canFuse ? new Color(0.18f, 0.72f, 0.32f) : new Color(0.25f, 0.28f, 0.35f), 12, () =>
                {
                    if (inv.TryFuseSimilarRarity(capturedRarity, out var forged, out string msg))
                    {
                        ShowToast(msg);
                        PopulateFusionLab(forged);
                    }
                    else
                    {
                        ShowToast(msg);
                    }
                });
                var fbRt = fuseBtn.GetComponent<RectTransform>();
                fbRt.anchorMin = new Vector2(0.08f, 0.07f);
                fbRt.anchorMax = new Vector2(0.92f, 0.37f);
                fbRt.offsetMin = Vector2.zero;
                fbRt.offsetMax = Vector2.zero;
            }

            // Section 2: Special Cross-Card Fusion Recipes
            AddFusionSectionHeader("2. SPECIAL CARD FUSION RECIPES (Combine 2 Cards + Coins -> Exclusive Fusion Card!)");

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
            }
        }

        private void ToggleQuestsModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = !_questsModal.activeSelf;
            CloseAllModals();
            if (next)
            {
                PopulateQuestsModal();
                _questsModal.SetActive(true);
            }
        }

        private void PopulateQuestsModal()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _questsListContent == null) return;

            for (int i = _questsListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_questsListContent.GetChild(i).gameObject);
            }

            for (int i = 0; i < inv.Quests.Count; i++)
            {
                var q = inv.Quests[i];
                var row = CreateRoundedPanel("QuestRow_" + i, _questsListContent, new Color(0.13f, 0.18f, 0.28f, 1f), new Vector2(620f, 92f));
                AddOutline(row.gameObject, q.IsCompleted && !q.IsClaimed ? new Color(1.0f, 0.84f, 0.22f) : new Color(0.30f, 0.38f, 0.52f), 2f);

                var qTxt = CreateText(
                    "QuestInfo",
                    row,
                    $"{q.Title}\nProgress: {q.CurrentProgress} / {q.TargetCount}   |   Reward: +{q.CoinReward} Barya Coins",
                    16,
                    FontStyle.Bold,
                    Color.white,
                    TextAnchor.MiddleLeft
                );
                qTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                qTxt.rectTransform.anchorMax = new Vector2(0.72f, 1f);
                qTxt.rectTransform.offsetMin = new Vector2(18f, 8f);
                qTxt.rectTransform.offsetMax = new Vector2(-8f, -8f);

                int capturedIdx = i;
                string lbl = q.IsClaimed ? "CLAIMED" : (q.IsCompleted ? $"CLAIM +{q.CoinReward}c" : "IN PROGRESS");
                Color col = q.IsClaimed ? new Color(0.24f, 0.28f, 0.34f) : (q.IsCompleted ? new Color(0.18f, 0.76f, 0.34f) : new Color(0.30f, 0.36f, 0.48f));
                var btn = CreateButton("ClaimQuestBtn", row, lbl, col, 14, () =>
                {
                    if (inv.TryClaimQuest(capturedIdx, out string msg))
                    {
                        ShowToast(msg);
                        PopulateQuestsModal();
                    }
                    else
                    {
                        ShowToast(msg);
                    }
                });
                var bRt = btn.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0.5f);
                bRt.anchorMax = new Vector2(1f, 0.5f);
                bRt.anchoredPosition = new Vector2(-92f, 0f);
                bRt.sizeDelta = new Vector2(158f, 44f);
                btn.interactable = q.IsCompleted && !q.IsClaimed;
            }
        }

        private void CreateTopCenterZoneBar()
        {
            var pill = CreateRoundedPanel("TopCenterZonePill", _canvas.transform, new Color(0.09f, 0.14f, 0.22f, 0.90f), new Vector2(470f, 40f));
            pill.anchorMin = new Vector2(0.5f, 1f);
            pill.anchorMax = new Vector2(0.5f, 1f);
            pill.pivot = new Vector2(0.5f, 1f);
            pill.anchoredPosition = new Vector2(0f, -20f);
            AddOutline(pill.gameObject, new Color(0.32f, 0.72f, 0.94f), 1.5f);

            var btn = pill.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(ToggleMapModal);

            _zonePillText = CreateText("ZonePillTxt", pill, "AREA: BASKETBALL COURT   |   [M] BARANGAY MAP", 15, FontStyle.Bold, new Color(0.86f, 0.96f, 1.0f), TextAnchor.MiddleCenter);
            StretchFull(_zonePillText.rectTransform, 6f);
        }

        private void CreateMapModal()
        {
            _mapModal = CreateModalBackdrop("BarangayMapModal", new Vector2(720f, 540f), out RectTransform window);

            var header = CreateText("MapHeader", window, "BARANGAY AREAS & FAST TRAVEL MAP [M]", 24, FontStyle.Bold, new Color(1.0f, 0.85f, 0.28f), TextAnchor.MiddleLeft);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.85f, 1f);
            header.rectTransform.offsetMin = new Vector2(26f, -56f);
            header.rectTransform.offsetMax = new Vector2(0f, -12f);

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
            lRt.offsetMax = new Vector2(-26f, -74f);
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            foreach (var zone in BarangayCourtBuilder.WorldZones)
            {
                var captured = zone;
                var row = CreateRoundedPanel("ZoneRow_" + zone.Name, listGo.transform, new Color(0.13f, 0.19f, 0.30f, 1f), new Vector2(650f, 76f));
                AddOutline(row.gameObject, new Color(0.32f, 0.56f, 0.86f), 2f);

                var txt = CreateText("ZoneTxt", row, $"{zone.Name}\n{zone.Subtitle}", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                txt.rectTransform.anchorMin = new Vector2(0f, 0f);
                txt.rectTransform.anchorMax = new Vector2(0.74f, 1f);
                txt.rectTransform.offsetMin = new Vector2(18f, 6f);
                txt.rectTransform.offsetMax = new Vector2(-8f, -6f);

                var goBtn = CreateButton("GoZoneBtn", row, "TRAVEL HERE", new Color(0.18f, 0.72f, 0.36f), 14, () =>
                {
                    CloseAllModals();
                    if (PlayerController.Instance != null)
                    {
                        PlayerController.Instance.TeleportForMatch(captured.SpawnPosition, Quaternion.Euler(0f, captured.SpawnYaw, 0f));
                        ShowToast($"Traveled to {captured.Name}!");
                    }
                });
                var gRt = goBtn.GetComponent<RectTransform>();
                gRt.anchorMin = new Vector2(1f, 0.5f);
                gRt.anchorMax = new Vector2(1f, 0.5f);
                gRt.anchoredPosition = new Vector2(-90f, 0f);
                gRt.sizeDelta = new Vector2(150f, 42f);
            }

            _mapModal.SetActive(false);
        }

        private void ToggleMapModal()
        {
            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle) return;
            bool next = !_mapModal.activeSelf;
            CloseAllModals();
            if (next) _mapModal.SetActive(true);
        }

        private void CloseAllModals()
        {
            if (_inventoryModal != null) _inventoryModal.SetActive(false);
            if (_shopModal != null) _shopModal.SetActive(false);
            if (_questsModal != null) _questsModal.SetActive(false);
            if (_mapModal != null) _mapModal.SetActive(false);
        }

        private bool IsAnyModalOpen()
        {
            return (_startScreenRoot != null && _startScreenRoot.activeSelf) ||
                   (_inventoryModal != null && _inventoryModal.activeSelf) ||
                   (_shopModal != null && _shopModal.activeSelf) ||
                   (_questsModal != null && _questsModal.activeSelf) ||
                   (_mapModal != null && _mapModal.activeSelf) ||
                   (_wagerModal != null && _wagerModal.activeSelf);
        }

        private void PopulateInventoryModal()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _inventoryGridContent == null) return;

            if (_inventoryHeaderCoins != null) _inventoryHeaderCoins.text = $"{inv.Coins:N0} COINS";

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

                string stars = new string('★', owned.StarRank) + new string('☆', 5 - owned.StarRank);
                var metaTxt = CreateText("Meta", cardBox, $"{stars}\n{TeksDatabase.GetRarityName(def.Rarity)} | {TeksDatabase.GetThemeName(def.Theme)}", 13, FontStyle.Bold, rarityCol, TextAnchor.UpperLeft);
                metaTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                metaTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                metaTxt.rectTransform.offsetMin = new Vector2(124f, -84f);
                metaTxt.rectTransform.offsetMax = new Vector2(-10f, -44f);

                int sweetPct = Mathf.RoundToInt(owned.GetFlickSweetSpotBonus() * 100f);
                int luckPct = Mathf.RoundToInt(owned.GetLuckRatingBonus() * 100f);
                var statsTxt = CreateText(
                    "Stats",
                    cardBox,
                    $"HP:{owned.GetHP(def)}  ATK:{owned.GetAtk(def)}  PWR:{owned.GetPower(def)}\nSweetSpot +{sweetPct}%  Luck +{luckPct}%\nDuplicates: x{owned.DuplicateCount}",
                    13,
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
                string upgLbl = owned.StarRank >= 5 ? "MAX 5★" : $"UPGRADE ({upgCost}c)";
                var upgBtn = CreateButton("UpgBtn", cardBox, upgLbl, canUpg ? new Color(0.18f, 0.72f, 0.30f) : new Color(0.26f, 0.30f, 0.38f), 12, () =>
                {
                    if (inv.TryUpgradeCard(owned.CardId, out string msg))
                    {
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

                var sellBtn = CreateButton("SellBtn", cardBox, $"SELL +{def.SellPrice}", owned.DuplicateCount > 0 ? new Color(0.76f, 0.48f, 0.14f) : new Color(0.24f, 0.24f, 0.28f), 12, () =>
                {
                    if (inv.TrySellDuplicate(owned.CardId, out int earned))
                    {
                        ShowToast($"Sold 1 duplicate {def.Name} for +{earned} Coins!");
                    }
                    else
                    {
                        ShowToast("No duplicate copy to sell!");
                    }
                });
                var slRt = sellBtn.GetComponent<RectTransform>();
                slRt.anchorMin = new Vector2(0f, 0f);
                slRt.anchorMax = new Vector2(0f, 0f);
                slRt.anchoredPosition = new Vector2(262f, 28f);
                slRt.sizeDelta = new Vector2(88f, 36f);
                sellBtn.interactable = owned.DuplicateCount > 0;
            }
        }

        private void PopulateShopModal()
        {
            var shop = ShopManager.Instance;
            var inv = PlayerInventory.Instance;
            if (shop == null || _shopSlotsContainer == null) return;

            if (_shopModalCoinsText != null && inv != null) _shopModalCoinsText.text = $"{inv.Coins:N0} COINS";
            if (_shopVendorBannerText != null) _shopVendorBannerText.text = shop.LastVendorMessage;
            if (_shopDailyFreeBtn != null)
            {
                _shopDailyFreeBtn.interactable = !shop.HasClaimedDailyBarya;
                if (_shopDailyFreeBtnText != null)
                {
                    _shopDailyFreeBtnText.text = shop.HasClaimedDailyBarya ? "DAILY CLAIMED" : "FREE +200 BARYA";
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
                Color bgCol = slot.IsSuperSlot ? new Color(0.20f, 0.16f, 0.09f, 1f) : new Color(0.13f, 0.18f, 0.28f, 1f);

                var box = new GameObject("ShopSlot_" + i, typeof(RectTransform), typeof(Image));
                box.transform.SetParent(_shopSlotsContainer, false);
                var boxImg = box.GetComponent<Image>();
                boxImg.sprite = CardArtGenerator.GetRoundedPanelSprite();
                boxImg.type = Image.Type.Sliced;
                boxImg.color = bgCol;
                AddOutline(box, slot.IsFlashSale ? new Color(0.98f, 0.28f, 0.24f) : (slot.IsSuperSlot ? new Color(1.0f, 0.82f, 0.20f) : rarityCol), (slot.IsSuperSlot || slot.IsFlashSale) ? 3f : 2f);

                var boxRt = box.GetComponent<RectTransform>();

                string slotTag = slot.IsFlashSale
                    ? "-30% SULIT SALE!"
                    : (slot.IsSuperSlot ? "SLOT 4: SUPER (RARE+)" : $"SLOT {i + 1}");
                Color tagCol = slot.IsFlashSale
                    ? new Color(1.0f, 0.42f, 0.36f)
                    : (slot.IsSuperSlot ? new Color(1.0f, 0.86f, 0.24f) : new Color(0.75f, 0.84f, 0.96f));
                var slotTagTxt = CreateText("SlotTag", boxRt, slotTag, 13, FontStyle.Bold, tagCol, TextAnchor.MiddleCenter);
                slotTagTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
                slotTagTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                slotTagTxt.rectTransform.offsetMin = new Vector2(4f, -26f);
                slotTagTxt.rectTransform.offsetMax = new Vector2(-4f, -4f);

                var artFrame = CreateRoundedPanel("CardPreview", boxRt, rarityCol, Vector2.zero);
                artFrame.anchorMin = new Vector2(0.18f, 0.34f);
                artFrame.anchorMax = new Vector2(0.82f, 0.88f);
                artFrame.offsetMin = Vector2.zero;
                artFrame.offsetMax = Vector2.zero;
                var artImg = CreateImage("Art", artFrame, CardArtGenerator.GetCardPortraitSprite(slot.Card, slot.IsSuperSlot ? 3 : 1));
                StretchFull(artImg.rectTransform, 4f);

                var cardName = CreateText("Name", boxRt, $"{slot.Card.Name}\n[{TeksDatabase.GetRarityName(slot.Card.Rarity)}]", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                cardName.rectTransform.anchorMin = new Vector2(0f, 0.16f);
                cardName.rectTransform.anchorMax = new Vector2(1f, 0.34f);
                cardName.rectTransform.offsetMin = new Vector2(6f, 0f);
                cardName.rectTransform.offsetMax = new Vector2(-6f, 0f);

                int capturedIdx = i;
                string btnLabel = slot.IsSoldOut
                    ? "SOLD OUT"
                    : (slot.IsFlashSale ? $"SALE {slot.Price}c (Was {slot.OriginalPrice})" : $"BUY ({slot.Price} COINS)");
                Color btnCol = slot.IsSoldOut
                    ? new Color(0.28f, 0.28f, 0.32f)
                    : (slot.IsFlashSale ? new Color(0.90f, 0.26f, 0.22f) : new Color(0.18f, 0.72f, 0.32f));
                var buyBtn = CreateButton("BuySlotBtn", boxRt, btnLabel, btnCol, 13, () =>
                {
                    if (ShopManager.Instance.TryBuySlot(capturedIdx, out string msg))
                    {
                        ShowToast(msg);
                        PopulateShopModal();
                    }
                    else
                    {
                        ShowToast(msg);
                    }
                });
                var bRt = buyBtn.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.06f, 0.03f);
                bRt.anchorMax = new Vector2(0.94f, 0.15f);
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                buyBtn.interactable = !slot.IsSoldOut;
            }
        }

        private void OpenBoosterPack(bool isSuperPack)
        {
            if (ShopManager.Instance == null) return;
            if (!ShopManager.Instance.TryBuyBoosterPack(isSuperPack, out var drawn, out string msg))
            {
                ShowToast(msg);
                return;
            }

            ShowToast(msg);
            RenderPackReveal(
                isSuperPack ? "ALAMAT SUPER PACK OPENED! Cards added to your Album:" : "KALYE BOOSTER PACK OPENED! Cards added to your Album:",
                drawn
            );
        }

        private void OpenMythicBox()
        {
            if (ShopManager.Instance == null) return;
            if (!ShopManager.Instance.TryBuyMythicBox(out var drawn, out string msg))
            {
                ShowToast(msg);
                return;
            }

            ShowToast(msg);
            RenderPackReveal("BARANGAY MYTHIC BOX OPENED! 5 Cards added to your Album:", drawn);
        }

        private void RenderPackReveal(string headerText, List<TeksCardDefinition> drawn)
        {
            if (_shopModalCoinsText != null && PlayerInventory.Instance != null)
            {
                _shopModalCoinsText.text = $"{PlayerInventory.Instance.Coins:N0} COINS";
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

            if (drawn == null) return;
            foreach (var card in drawn)
            {
                if (card == null) continue;
                Color rarityCol = TeksDatabase.GetRarityColor(card.Rarity);
                var cardPill = new GameObject("Drawn_" + card.Id, typeof(RectTransform), typeof(Image));
                cardPill.transform.SetParent(_packRevealContainer, false);
                var pillImg = cardPill.GetComponent<Image>();
                pillImg.sprite = CardArtGenerator.GetRoundedPanelSprite();
                pillImg.type = Image.Type.Sliced;
                pillImg.color = new Color(0.13f, 0.19f, 0.30f, 1f);
                AddOutline(cardPill, rarityCol, 2f);

                var pillRt = cardPill.GetComponent<RectTransform>();
                var thumb = CreateImage("Thumb", pillRt, CardArtGenerator.GetCardPortraitSprite(card, 1));
                thumb.rectTransform.anchorMin = new Vector2(0.05f, 0.10f);
                thumb.rectTransform.anchorMax = new Vector2(0.40f, 0.90f);
                thumb.rectTransform.offsetMin = Vector2.zero;
                thumb.rectTransform.offsetMax = Vector2.zero;

                var info = CreateText("Info", pillRt, $"{card.Name}\n{TeksDatabase.GetRarityName(card.Rarity)}", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                info.rectTransform.anchorMin = new Vector2(0.44f, 0.08f);
                info.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
                info.rectTransform.offsetMin = Vector2.zero;
                info.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private void PopulateWagerModal()
        {
            var mm = TeksMatchManager.Instance;
            var inv = PlayerInventory.Instance;
            if (mm == null || inv == null || mm.ActiveOpponent == null) return;

            var equipped = inv.GetEquippedPamato(out var pDef);
            if (equipped != null && pDef != null)
            {
                _wagerPlayerCardImg.sprite = CardArtGenerator.GetCardPortraitSprite(pDef, equipped.StarRank);
                _wagerPlayerCardText.text = $"{pDef.Name} ({equipped.StarRank}★)";
                int sweetPct = Mathf.RoundToInt((mm.SweetSpotHalfWidth + equipped.GetFlickSweetSpotBonus()) * 100f);
                int luckPct = Mathf.RoundToInt(equipped.GetLuckRatingBonus() * 100f);
                _wagerBonusInfoText.text = $"Pamato Bonus: Sweet Spot +{Mathf.RoundToInt(equipped.GetFlickSweetSpotBonus() * 100f)}%  |  Chub Luck +{luckPct}%  |  Win: +{ _selectedWagerAmount } Coins & Opponent Card";
            }

            var opp = mm.ActiveOpponent;
            _wagerOppName.text = opp.GetDisplayTag().ToUpper();
            if (_wagerChallengeQuoteText != null)
            {
                string nick = inv != null ? inv.PlayerNickname : "TeksMaster";
                _wagerChallengeQuoteText.text = $"{opp.NpcName} to {nick}: \"{opp.ChallengeQuote}\"";
            }
            var oDef = TeksDatabase.GetCard(opp.FavoriteCardId);
            if (oDef != null)
            {
                _wagerOppCardImg.sprite = CardArtGenerator.GetCardPortraitSprite(oDef, opp.CardStarRank);
                _wagerOppCardText.text = $"{oDef.Name} ({opp.CardStarRank}★)";
            }

            RefreshWagerText();
        }

        private void RefreshWagerText()
        {
            if (_wagerAmountText != null)
            {
                _wagerAmountText.text = $"WAGER: {_selectedWagerAmount} BARYA COINS";
            }
            var inv = PlayerInventory.Instance;
            if (_wagerBonusInfoText != null && inv != null)
            {
                var eq = inv.GetEquippedPamato(out _);
                int sweet = eq != null ? Mathf.RoundToInt(eq.GetFlickSweetSpotBonus() * 100f) : 0;
                int luck = eq != null ? Mathf.RoundToInt(eq.GetLuckRatingBonus() * 100f) : 0;
                _wagerBonusInfoText.text = $"Pamato Bonus: Sweet Spot +{sweet}%  |  Chub Luck +{luck}%  |  Win: +{_selectedWagerAmount} Coins + Card!";
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
            btn.onClick.AddListener(() => onClick?.Invoke());

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
    }
}
