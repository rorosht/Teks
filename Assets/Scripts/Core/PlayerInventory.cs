using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeksMaster.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private string playerNickname = "TeksMaster";
        [SerializeField] private int coins = 2450;
        [SerializeField] private int gems = 25;
        [SerializeField] private PlayerCharacterType selectedCharacterType = PlayerCharacterType.StreetKid;
        [SerializeField] private string equippedPamatoId = "hero";
        [SerializeField] private List<OwnedTeksCard> ownedCards = new List<OwnedTeksCard>();

        public string PlayerNickname => string.IsNullOrWhiteSpace(playerNickname) ? "TeksMaster" : playerNickname;
        public bool HasStartedGame { get; private set; }
        public int Coins => coins;
        public int Gems => gems;
        public PlayerCharacterType SelectedCharacterType => selectedCharacterType;
        public string EquippedPamatoId => equippedPamatoId;
        public IReadOnlyList<OwnedTeksCard> OwnedCards => ownedCards;
        public IReadOnlyList<BarangayQuest> Quests => _quests;
        public IReadOnlyList<BarangayAreaId> UnlockedAreas => _unlockedAreas;
        public IReadOnlyList<string> DefeatedNpcIds => _defeatedNpcIds;
        public IReadOnlyList<string> DiscoveredHiddenCardIds => _discoveredHiddenCardIds;
        public AudioSettingsData AudioSettings => _audioSettings;

        public int MatchesWon { get; private set; }
        public int MatchesPlayed { get; private set; }
        public int WinStreak { get; private set; }
        public int BestWinStreak { get; private set; }
        public int TournamentsWon { get; private set; }
        public int ChallengesCompleted { get; private set; }

        public bool FreePlayUnlockAll { get; private set; }
        public string LastDailyMissionDate { get; private set; } = string.Empty;
        public string LastDailyFreeDrawDate { get; private set; } = string.Empty;

        public event Action OnInventoryChanged;
        public event Action<PlayerCharacterType> OnCharacterTypeChanged;
        public event Action<BarangayAreaId> OnAreaUnlocked;
        public event Action OnAreasChanged;
        public event Action<AudioSettingsData> OnAudioSettingsChanged;

        private readonly List<BarangayQuest> _quests = new List<BarangayQuest>();
        private readonly List<BarangayAreaId> _unlockedAreas = new List<BarangayAreaId>();
        private readonly List<AreaWinRecord> _areaWins = new List<AreaWinRecord>();
        private readonly List<string> _defeatedNpcIds = new List<string>();
        private readonly List<string> _discoveredHiddenCardIds = new List<string>();
        private AudioSettingsData _audioSettings = new AudioSettingsData();

        private bool _initialized;
        private bool _suppressAutoSave;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _suppressAutoSave = true;
            try
            {
                LoadSavedProfile();
                EnsureStarterCollection();
            }
            finally
            {
                _suppressAutoSave = false;
            }

            if (Application.isPlaying)
            {
                if (!LoadFromDisk())
                {
                    CheckAndResetDailyMissionsIfNeeded();
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && Application.isPlaying)
            {
                SaveToDisk();
            }
        }

        private void OnApplicationQuit()
        {
            if (Application.isPlaying)
            {
                SaveToDisk();
            }
        }

        private void LoadSavedProfile()
        {
            if (PlayerPrefs.HasKey(TeksSaveSystem.PrefKeyNickname))
            {
                string saved = PlayerPrefs.GetString(TeksSaveSystem.PrefKeyNickname, "TeksMaster");
                if (!string.IsNullOrWhiteSpace(saved)) playerNickname = saved;
            }
            if (PlayerPrefs.HasKey(TeksSaveSystem.PrefKeyCharacterType))
            {
                selectedCharacterType = (PlayerCharacterType)Mathf.Clamp(
                    PlayerPrefs.GetInt(TeksSaveSystem.PrefKeyCharacterType, 0),
                    0,
                    TeksCharacterPresets.Count - 1);
            }
            else if (PlayerPrefs.HasKey(TeksSaveSystem.PrefKeyGender))
            {
                int legacyGender = PlayerPrefs.GetInt(TeksSaveSystem.PrefKeyGender, 0);
                selectedCharacterType = legacyGender == 1
                    ? PlayerCharacterType.SportyAce
                    : PlayerCharacterType.StreetKid;
            }
            _audioSettings = TeksSaveSystem.LoadAudioSettingsFromPrefs();
        }

        public void EnsureStarterCollection()
        {
            if (_initialized) return;
            _initialized = true;

            if (ownedCards.Count == 0)
            {
                ownedCards.Add(new OwnedTeksCard("hero", 5, 1));
                ownedCards.Add(new OwnedTeksCard("bakunawa", 5, 1));
                ownedCards.Add(new OwnedTeksCard("mecha_bathala", 4, 2));
                ownedCards.Add(new OwnedTeksCard("kapitan_barangay", 2, 1));
                ownedCards.Add(new OwnedTeksCard("tikbalang_striker", 2, 3));
                ownedCards.Add(new OwnedTeksCard("kid_kidlat", 1, 2));
                ownedCards.Add(new OwnedTeksCard("bantay_kalye", 1, 3));
                ownedCards.Add(new OwnedTeksCard("nuno_guard", 1, 2));
            }

            if (string.IsNullOrEmpty(equippedPamatoId) || GetOwnedCardInternal(equippedPamatoId) == null)
            {
                equippedPamatoId = ownedCards[0].CardId;
            }

            EnsureDefaultUnlockedAreas();
            EnsureAllMissionTemplates();
            SyncPassiveMissionProgress();
        }

        private void EnsureDefaultUnlockedAreas()
        {
            AddUnlockedAreaInternal(BarangayAreaId.PlayersNeighborhood);
            AddUnlockedAreaInternal(BarangayAreaId.SariSariAndKalye);
            AddUnlockedAreaInternal(BarangayAreaId.TournamentArena);
        }

        private bool AddUnlockedAreaInternal(BarangayAreaId area)
        {
            for (int i = 0; i < _unlockedAreas.Count; i++)
            {
                if (_unlockedAreas[i] == area) return false;
            }
            _unlockedAreas.Add(area);
            _unlockedAreas.Sort();
            return true;
        }

        private void EnsureAllMissionTemplates()
        {
            var templates = BuildMissionTemplates();
            for (int i = 0; i < templates.Count; i++)
            {
                var tpl = templates[i];
                var existing = FindQuestById(tpl.Id);
                if (existing == null)
                {
                    _quests.Add(tpl);
                }
                else
                {
                    // Keep saved progress & claimed state; refresh metadata in case of patch updates
                    existing.Title = tpl.Title;
                    existing.Description = tpl.Description;
                    existing.Category = tpl.Category;
                    existing.TargetCount = tpl.TargetCount;
                    existing.CoinReward = tpl.CoinReward;
                    existing.GemReward = tpl.GemReward;
                    existing.HasPackReward = tpl.HasPackReward;
                    existing.PackRewardType = tpl.PackRewardType;
                    existing.SpecialCardRewardId = tpl.SpecialCardRewardId;
                    existing.IsDaily = tpl.IsDaily;
                }
            }
        }

        private static List<BarangayQuest> BuildMissionTemplates()
        {
            return new List<BarangayQuest>
            {
                // Main Story (6)
                new BarangayQuest
                {
                    Id = "story_defeat_dodong",
                    Title = "Beat Dodong in Neighborhood",
                    Description = "Challenge and defeat Dodong in Player's Neighborhood.",
                    Category = MissionCategory.MainStory,
                    TargetCount = 1,
                    CoinReward = 100,
                    GemReward = 5
                },
                new BarangayQuest
                {
                    Id = "story_defeat_mark",
                    Title = "Beat Mark at Sari-Sari & Kalye",
                    Description = "Challenge and defeat Mark along Kalye Street.",
                    Category = MissionCategory.MainStory,
                    TargetCount = 1,
                    CoinReward = 120,
                    GemReward = 5
                },
                new BarangayQuest
                {
                    Id = "story_unlock_court",
                    Title = "Unlock the Basketball Court",
                    Description = "Defeat Dodong & Mark (or win 2 starter matches) to open the Basketball Court.",
                    Category = MissionCategory.MainStory,
                    TargetCount = 1,
                    CoinReward = 180,
                    GemReward = 10,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Basic
                },
                new BarangayQuest
                {
                    Id = "story_defeat_carlo",
                    Title = "Beat Carlo on the Court",
                    Description = "Challenge and defeat Carlo at the Barangay Basketball Court.",
                    Category = MissionCategory.MainStory,
                    TargetCount = 1,
                    CoinReward = 220,
                    GemReward = 10
                },
                new BarangayQuest
                {
                    Id = "story_defeat_joshua",
                    Title = "Beat Joshua in Old Neighborhood",
                    Description = "Unlock Old Neighborhood and defeat Joshua.",
                    Category = MissionCategory.MainStory,
                    TargetCount = 1,
                    CoinReward = 320,
                    GemReward = 15,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Rare
                },
                new BarangayQuest
                {
                    Id = "story_defeat_champion",
                    Title = "Defeat the Teks Master",
                    Description = "Conquer Fiesta Arena and defeat the Teks Master Champion!",
                    Category = MissionCategory.MainStory,
                    TargetCount = 1,
                    CoinReward = 600,
                    GemReward = 40,
                    SpecialCardRewardId = "sun_dragon_hero"
                },

                // Daily (3)
                new BarangayQuest
                {
                    Id = "win_matches",
                    Title = "Daily: Win 2 Teks Matches",
                    Description = "Win any 2 Teks battles today.",
                    Category = MissionCategory.Daily,
                    TargetCount = 2,
                    CoinReward = 250,
                    GemReward = 5,
                    IsDaily = true
                },
                new BarangayQuest
                {
                    Id = "open_pack",
                    Title = "Daily: Open 1 Card Pack",
                    Description = "Open any pack or Daily Free Draw at the Sari-Sari Store.",
                    Category = MissionCategory.Daily,
                    TargetCount = 1,
                    CoinReward = 200,
                    GemReward = 5,
                    IsDaily = true
                },
                new BarangayQuest
                {
                    Id = "daily_upgrade_card",
                    Title = "Daily: Upgrade 1 Card",
                    Description = "Upgrade any card in your Collection today.",
                    Category = MissionCategory.Daily,
                    TargetCount = 1,
                    CoinReward = 180,
                    GemReward = 5,
                    IsDaily = true
                },

                // Collection (4)
                new BarangayQuest
                {
                    Id = "col_own_12",
                    Title = "Collect 12 Unique Cards",
                    Description = "Own at least 12 distinct Teks cards in your Collection.",
                    Category = MissionCategory.Collection,
                    TargetCount = 12,
                    CoinReward = 250,
                    GemReward = 10,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Basic
                },
                new BarangayQuest
                {
                    Id = "col_own_25",
                    Title = "Collect 25 Unique Cards",
                    Description = "Build an elite deck of 25 distinct Teks cards.",
                    Category = MissionCategory.Collection,
                    TargetCount = 25,
                    CoinReward = 500,
                    GemReward = 25,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Premium
                },
                new BarangayQuest
                {
                    Id = "col_upgrade_lv5",
                    Title = "Upgrade a Card to Level 5",
                    Description = "Upgrade any Teks card all the way to Max Level 5.",
                    Category = MissionCategory.Collection,
                    TargetCount = 1,
                    CoinReward = 300,
                    GemReward = 15
                },
                new BarangayQuest
                {
                    Id = "fuse_card",
                    Title = "Fuse 1 Card in Fusion Lab",
                    Description = "Forge a card in the Fusion Lab using a recipe or 3 duplicates.",
                    Category = MissionCategory.Collection,
                    TargetCount = 1,
                    CoinReward = 300,
                    GemReward = 10
                },

                // Exploration (3)
                new BarangayQuest
                {
                    Id = "exp_hidden_1",
                    Title = "Find 1 Hidden Card",
                    Description = "Explore the barangay and discover your first hidden card.",
                    Category = MissionCategory.Exploration,
                    TargetCount = 1,
                    CoinReward = 150,
                    GemReward = 10
                },
                new BarangayQuest
                {
                    Id = "exp_hidden_6",
                    Title = "Barangay Scout: 6 Hidden Cards",
                    Description = "Discover 6 hidden collectible cards across the barangay.",
                    Category = MissionCategory.Exploration,
                    TargetCount = 6,
                    CoinReward = 350,
                    GemReward = 20,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Rare
                },
                new BarangayQuest
                {
                    Id = "exp_hidden_12",
                    Title = "Secret Hunter: All 12 Hidden Cards",
                    Description = "Find all 12 hidden collectible cards across all 5 areas!",
                    Category = MissionCategory.Exploration,
                    TargetCount = 12,
                    CoinReward = 750,
                    GemReward = 50,
                    SpecialCardRewardId = "adarna_phoenix"
                },

                // Area-Clear Exploration Missions (5)
                new BarangayQuest
                {
                    Id = "area_clear_neighborhood",
                    Title = "Clear Player's Neighborhood",
                    Description = "Defeat every Teks rival in Player's Neighborhood.",
                    Category = MissionCategory.Exploration,
                    TargetCount = 4,
                    CoinReward = 220,
                    GemReward = 10
                },
                new BarangayQuest
                {
                    Id = "area_clear_kalye",
                    Title = "Clear Sari-Sari & Kalye",
                    Description = "Defeat every Teks rival along Sari-Sari & Kalye Street.",
                    Category = MissionCategory.Exploration,
                    TargetCount = 5,
                    CoinReward = 260,
                    GemReward = 12
                },
                new BarangayQuest
                {
                    Id = "area_clear_court",
                    Title = "Clear Barangay Basketball Court",
                    Description = "Defeat every Teks rival on the Barangay Basketball Court.",
                    Category = MissionCategory.Exploration,
                    TargetCount = 9,
                    CoinReward = 420,
                    GemReward = 20,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Rare
                },
                new BarangayQuest
                {
                    Id = "area_clear_old_neighborhood",
                    Title = "Clear Old Neighborhood",
                    Description = "Defeat every Teks rival in the Old Neighborhood.",
                    Category = MissionCategory.Exploration,
                    TargetCount = 6,
                    CoinReward = 380,
                    GemReward = 18
                },
                new BarangayQuest
                {
                    Id = "area_clear_fiesta",
                    Title = "Clear Fiesta Teks Arena",
                    Description = "Defeat every elite rival in the Fiesta Teks Arena!",
                    Category = MissionCategory.Exploration,
                    TargetCount = 4,
                    CoinReward = 650,
                    GemReward = 35,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Premium
                },

                // Achievement (4)
                new BarangayQuest
                {
                    Id = "ach_wins_10",
                    Title = "Barangay Legend: 10 Total Wins",
                    Description = "Win 10 Teks matches across any mode.",
                    Category = MissionCategory.Achievement,
                    TargetCount = 10,
                    CoinReward = 400,
                    GemReward = 20
                },
                new BarangayQuest
                {
                    Id = "ach_streak_3",
                    Title = "Hot Hand: 3 Win Streak",
                    Description = "Reach a 3-match winning streak without losing.",
                    Category = MissionCategory.Achievement,
                    TargetCount = 3,
                    CoinReward = 300,
                    GemReward = 15
                },
                new BarangayQuest
                {
                    Id = "ach_tournament_1",
                    Title = "Barangay Tournament Champion",
                    Description = "Win a 3-round Barangay Tournament bracket.",
                    Category = MissionCategory.Achievement,
                    TargetCount = 1,
                    CoinReward = 500,
                    GemReward = 30,
                    HasPackReward = true,
                    PackRewardType = ShopPackType.Premium
                },
                new BarangayQuest
                {
                    Id = "ach_challenge_1",
                    Title = "Challenge Mode Conqueror",
                    Description = "Complete any special-rule Challenge Mode trial.",
                    Category = MissionCategory.Achievement,
                    TargetCount = 1,
                    CoinReward = 400,
                    GemReward = 25
                }
            };
        }

        private OwnedTeksCard GetOwnedCardInternal(string cardId)
        {
            for (int i = 0; i < ownedCards.Count; i++)
            {
                if (ownedCards[i].CardId == cardId) return ownedCards[i];
            }
            return null;
        }

        public OwnedTeksCard GetOwnedCard(string cardId)
        {
            EnsureStarterCollection();
            return GetOwnedCardInternal(cardId);
        }

        public OwnedTeksCard GetEquippedPamato(out TeksCardDefinition definition)
        {
            EnsureStarterCollection();
            var owned = GetOwnedCardInternal(equippedPamatoId);
            if (owned == null && ownedCards.Count > 0)
            {
                owned = ownedCards[0];
                equippedPamatoId = owned.CardId;
            }
            definition = owned != null ? TeksDatabase.GetCard(owned.CardId) : null;
            return owned;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            coins += amount;
            NotifyAndAutoSave();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0) return false;
            if (coins < amount) return false;
            coins -= amount;
            NotifyAndAutoSave();
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            gems += amount;
            NotifyAndAutoSave();
        }

        public bool TrySpendGems(int amount)
        {
            if (amount < 0) return false;
            if (gems < amount) return false;
            gems -= amount;
            NotifyAndAutoSave();
            return true;
        }

        public void AddCard(string cardId, int count = 1)
        {
            EnsureStarterCollection();
            if (count <= 0 || TeksDatabase.GetCard(cardId) == null) return;

            var existing = GetOwnedCardInternal(cardId);
            if (existing != null)
            {
                existing.DuplicateCount += count;
            }
            else
            {
                ownedCards.Add(new OwnedTeksCard(cardId, 1, count - 1));
            }

            SyncCollectionMissionProgress();
            NotifyAndAutoSave();
        }

        public void EquipPamato(string cardId)
        {
            var owned = GetOwnedCard(cardId);
            if (owned == null) return;
            equippedPamatoId = cardId;
            NotifyAndAutoSave();
        }

        public bool CanUpgradeCard(string cardId, out int coinCost, out int dupesNeeded, out string statusMsg)
        {
            coinCost = 0;
            dupesNeeded = 0;

            var owned = GetOwnedCard(cardId);
            var def = TeksDatabase.GetCard(cardId);
            if (owned == null || def == null)
            {
                statusMsg = "Card not owned";
                return false;
            }

            if (owned.StarRank >= 5)
            {
                statusMsg = "MAX LV.5 (5★)";
                return false;
            }

            coinCost = owned.GetUpgradeCoinCost(def);
            dupesNeeded = owned.GetDuplicatesNeededForUpgrade();

            if (owned.DuplicateCount < dupesNeeded)
            {
                statusMsg = $"Need {dupesNeeded} Duplicate (Have {owned.DuplicateCount})";
                return false;
            }

            if (coins < coinCost)
            {
                statusMsg = $"Need {coinCost} Coins";
                return false;
            }

            statusMsg = $"Upgrade -> Lv.{owned.StarRank + 1} ({coinCost} Coins + {dupesNeeded} Dup)";
            return true;
        }

        public bool TryUpgradeCard(string cardId, out string message)
        {
            if (!CanUpgradeCard(cardId, out int coinCost, out int dupesNeeded, out string statusMsg))
            {
                message = statusMsg;
                return false;
            }

            var owned = GetOwnedCard(cardId);
            var def = TeksDatabase.GetCard(cardId);

            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                coins -= coinCost;
                owned.DuplicateCount -= dupesNeeded;
                owned.StarRank = Mathf.Clamp(owned.StarRank + 1, 1, 5);
                ReportCardUpgraded(owned.CardId, owned.StarRank);
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            message = $"{def.Name} upgraded to Level {owned.StarRank} ({owned.StarRank}★)! Card stats & foil glow enhanced!";
            NotifyAndAutoSave();
            return true;
        }

        public int GetTotalOwnedCardCopies()
        {
            EnsureStarterCollection();
            int total = 0;
            for (int i = 0; i < ownedCards.Count; i++)
            {
                if (ownedCards[i] != null)
                {
                    total += ownedCards[i].Quantity;
                }
            }
            return total;
        }

        public bool CanSellCard(string cardId, out string reason)
        {
            EnsureStarterCollection();
            var owned = GetOwnedCardInternal(cardId);
            var def = TeksDatabase.GetCard(cardId);
            if (owned == null || def == null)
            {
                reason = "Card not owned";
                return false;
            }

            if (GetTotalOwnedCardCopies() <= 1)
            {
                reason = "Cannot sell your last remaining card!";
                return false;
            }

            reason = owned.DuplicateCount > 0
                ? $"Sell 1 copy for +{def.SellPrice} Coins"
                : $"Sell last copy for +{def.SellPrice} Coins";
            return true;
        }

        public bool TrySellCard(string cardId, out int coinsEarned, out bool removedLastCopy, out string autoEquippedCardName)
        {
            coinsEarned = 0;
            removedLastCopy = false;
            autoEquippedCardName = string.Empty;

            if (!CanSellCard(cardId, out _))
            {
                return false;
            }

            var owned = GetOwnedCardInternal(cardId);
            var def = TeksDatabase.GetCard(cardId);
            if (owned == null || def == null)
            {
                return false;
            }

            if (owned.DuplicateCount > 0)
            {
                owned.DuplicateCount -= 1;
                removedLastCopy = false;
            }
            else
            {
                ownedCards.Remove(owned);
                removedLastCopy = true;
                if (equippedPamatoId == cardId && ownedCards.Count > 0)
                {
                    equippedPamatoId = ownedCards[0].CardId;
                    var fallbackDef = TeksDatabase.GetCard(equippedPamatoId);
                    autoEquippedCardName = fallbackDef != null ? fallbackDef.Name : equippedPamatoId;
                }
            }

            coinsEarned = def.SellPrice;
            coins += coinsEarned;
            NotifyAndAutoSave();
            return true;
        }

        public bool TrySellDuplicate(string cardId, out int coinsEarned)
        {
            coinsEarned = 0;
            var owned = GetOwnedCard(cardId);
            var def = TeksDatabase.GetCard(cardId);
            if (owned == null || def == null || owned.DuplicateCount <= 0)
            {
                return false;
            }

            owned.DuplicateCount -= 1;
            coinsEarned = def.SellPrice;
            coins += coinsEarned;
            NotifyAndAutoSave();
            return true;
        }

        public bool CanFuseRecipe(CardFusionRecipe recipe, out string reason)
        {
            if (recipe == null)
            {
                reason = "Invalid recipe";
                return false;
            }

            var ownA = GetOwnedCard(recipe.IngredientAId);
            var ownB = GetOwnedCard(recipe.IngredientBId);
            var defA = TeksDatabase.GetCard(recipe.IngredientAId);
            var defB = TeksDatabase.GetCard(recipe.IngredientBId);

            if (ownA == null)
            {
                reason = $"Need {(defA != null ? defA.Name : recipe.IngredientAId)}";
                return false;
            }
            if (recipe.IngredientAId == recipe.IngredientBId)
            {
                if (ownA.Quantity < 2)
                {
                    reason = $"Need 2x {(defA != null ? defA.Name : recipe.IngredientAId)}";
                    return false;
                }
            }
            else if (ownB == null)
            {
                reason = $"Need {(defB != null ? defB.Name : recipe.IngredientBId)}";
                return false;
            }
            if (coins < recipe.CoinCost)
            {
                reason = $"Need {recipe.CoinCost} Coins";
                return false;
            }

            reason = $"FUSE ({recipe.CoinCost} Coins)";
            return true;
        }

        public bool TryFuseRecipe(CardFusionRecipe recipe, out TeksCardDefinition resultCard, out string message)
        {
            resultCard = null;
            if (!CanFuseRecipe(recipe, out string reason))
            {
                message = reason;
                return false;
            }

            var ownA = GetOwnedCardInternal(recipe.IngredientAId);
            var ownB = GetOwnedCardInternal(recipe.IngredientBId);
            resultCard = TeksDatabase.GetCard(recipe.ResultCardId);
            if (resultCard == null)
            {
                message = "Fusion target card not found.";
                return false;
            }

            int newFusionStage = 1;
            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                coins -= recipe.CoinCost;
                if (recipe.IngredientAId == recipe.IngredientBId)
                {
                    if (ownA != null)
                    {
                        if (ownA.DuplicateCount >= 2)
                        {
                            ownA.DuplicateCount -= 2;
                        }
                        else
                        {
                            ownedCards.Remove(ownA);
                        }
                    }
                }
                else
                {
                    if (ownA != null)
                    {
                        if (ownA.DuplicateCount > 0) ownA.DuplicateCount -= 1;
                        else ownedCards.Remove(ownA);
                    }
                    if (ownB != null)
                    {
                        if (ownB.DuplicateCount > 0) ownB.DuplicateCount -= 1;
                        else ownedCards.Remove(ownB);
                    }
                }

                AddCard(resultCard.Id, 1);

                // Ensure equipped Pamato is still owned (auto-equip forged card if equipped singleton was consumed)
                if (GetOwnedCardInternal(equippedPamatoId) == null)
                {
                    if (GetOwnedCardInternal(resultCard.Id) != null)
                    {
                        equippedPamatoId = resultCard.Id;
                    }
                    else if (ownedCards.Count > 0)
                    {
                        equippedPamatoId = ownedCards[0].CardId;
                    }
                }

                var ownedRes = GetOwnedCardInternal(resultCard.Id);
                if (ownedRes != null)
                {
                    ownedRes.FusionStage = Mathf.Max(1, ownedRes.FusionStage + 1);
                    newFusionStage = ownedRes.FusionStage;
                }
                ReportFusion(resultCard.Id, newFusionStage);
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            NotifyAndAutoSave();
            message = $"FUSION SUCCESS! Forged {resultCard.Name} (Stage {newFusionStage}, {TeksDatabase.GetRarityName(resultCard.Rarity)})!";
            return true;
        }

        public static int GetSimilarRarityFusionCost(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => 50,
                CardRarity.Uncommon => 100,
                CardRarity.Rare => 180,
                CardRarity.Epic => 300,
                _ => 0
            };
        }

        public bool CanFuseSimilarRarity(CardRarity rarity, out int availableDupes, out int coinCost, out string reason)
        {
            EnsureStarterCollection();
            availableDupes = 0;
            coinCost = GetSimilarRarityFusionCost(rarity);

            if (!TeksDatabase.TryGetNextRarity(rarity, out var nextRarity))
            {
                reason = "Already max rarity";
                return false;
            }

            for (int i = 0; i < ownedCards.Count; i++)
            {
                var def = TeksDatabase.GetCard(ownedCards[i].CardId);
                if (def != null && def.Rarity == rarity)
                {
                    availableDupes += ownedCards[i].Quantity;
                }
            }

            if (availableDupes < 3)
            {
                reason = $"Need 3 {TeksDatabase.GetRarityName(rarity)} Cards ({availableDupes}/3)";
                return false;
            }

            if (GetTotalOwnedCardCopies() - 3 < 1)
            {
                reason = "Must keep at least 1 remaining card!";
                return false;
            }

            if (coins < coinCost)
            {
                reason = $"Need {coinCost} Coins";
                return false;
            }

            reason = $"FUSE 3 -> {TeksDatabase.GetRarityName(nextRarity)} ({coinCost}c)";
            return true;
        }

        public bool TryFuseSimilarRarity(CardRarity rarity, out TeksCardDefinition resultCard, out string message)
        {
            resultCard = null;
            if (!CanFuseSimilarRarity(rarity, out _, out int coinCost, out string reason))
            {
                message = reason;
                return false;
            }

            TeksDatabase.TryGetNextRarity(rarity, out var nextRarity);
            resultCard = TeksDatabase.RollCardByRarity(nextRarity);
            if (resultCard == null)
            {
                message = "Fusion failed.";
                return false;
            }

            int newFusionStage = 1;
            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                int toConsume = 3;

                // Pass 1: Consume duplicates first
                for (int i = 0; i < ownedCards.Count && toConsume > 0; i++)
                {
                    var def = TeksDatabase.GetCard(ownedCards[i].CardId);
                    if (def != null && def.Rarity == rarity && ownedCards[i].DuplicateCount > 0)
                    {
                        int take = Mathf.Min(ownedCards[i].DuplicateCount, toConsume);
                        ownedCards[i].DuplicateCount -= take;
                        toConsume -= take;
                    }
                }

                // Pass 2: Consume non-equipped singletons next
                for (int i = ownedCards.Count - 1; i >= 0 && toConsume > 0; i--)
                {
                    var owned = ownedCards[i];
                    var def = TeksDatabase.GetCard(owned.CardId);
                    if (def != null && def.Rarity == rarity && owned.DuplicateCount == 0 && owned.CardId != equippedPamatoId)
                    {
                        ownedCards.RemoveAt(i);
                        toConsume--;
                    }
                }

                // Pass 3: Consume equipped singleton last if still needed
                for (int i = ownedCards.Count - 1; i >= 0 && toConsume > 0; i--)
                {
                    var owned = ownedCards[i];
                    var def = TeksDatabase.GetCard(owned.CardId);
                    if (def != null && def.Rarity == rarity && owned.DuplicateCount == 0)
                    {
                        ownedCards.RemoveAt(i);
                        toConsume--;
                    }
                }

                coins -= coinCost;
                AddCard(resultCard.Id, 1);

                // Ensure equipped Pamato is still valid (auto-equip if equipped singleton was consumed)
                if (GetOwnedCardInternal(equippedPamatoId) == null && ownedCards.Count > 0)
                {
                    equippedPamatoId = ownedCards[0].CardId;
                }

                var ownedRes = GetOwnedCardInternal(resultCard.Id);
                if (ownedRes != null)
                {
                    ownedRes.FusionStage = Mathf.Max(1, ownedRes.FusionStage + 1);
                    newFusionStage = ownedRes.FusionStage;
                }
                ReportFusion(resultCard.Id, newFusionStage);
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            NotifyAndAutoSave();
            message = $"SIMILAR FUSION! 3 {TeksDatabase.GetRarityName(rarity)} Cards -> {resultCard.Name} (Stage {newFusionStage}, {TeksDatabase.GetRarityName(resultCard.Rarity)})!";
            return true;
        }

        public bool TrySellAllExtraDuplicates(out int totalSold, out int totalCoins)
        {
            totalSold = 0;
            totalCoins = 0;
            for (int i = 0; i < ownedCards.Count; i++)
            {
                var owned = ownedCards[i];
                var def = TeksDatabase.GetCard(owned.CardId);
                if (def != null && owned.DuplicateCount > 0)
                {
                    totalSold += owned.DuplicateCount;
                    totalCoins += owned.DuplicateCount * def.SellPrice;
                    owned.DuplicateCount = 0;
                }
            }

            if (totalSold <= 0) return false;
            coins += totalCoins;
            NotifyAndAutoSave();
            return true;
        }

        // --- Area Progression & Unlocking ---

        public bool IsAreaStoryUnlocked(BarangayAreaId area)
        {
            EnsureStarterCollection();
            if (area == BarangayAreaId.PlayersNeighborhood
                || area == BarangayAreaId.SariSariAndKalye
                || area == BarangayAreaId.TournamentArena)
                return true;

            for (int i = 0; i < _unlockedAreas.Count; i++)
            {
                if (_unlockedAreas[i] == area) return true;
            }
            return false;
        }

        public bool IsAreaUnlocked(BarangayAreaId area)
        {
            return FreePlayUnlockAll || IsAreaStoryUnlocked(area);
        }

        public bool UnlockArea(BarangayAreaId area)
        {
            EnsureStarterCollection();
            if (!AddUnlockedAreaInternal(area)) return false;

            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                ReportAreaUnlocked(area);
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            OnAreaUnlocked?.Invoke(area);
            OnAreasChanged?.Invoke();
            NotifyAndAutoSave();
            return true;
        }

        public void SetFreePlayUnlockAll(bool enabled)
        {
            if (FreePlayUnlockAll == enabled) return;
            FreePlayUnlockAll = enabled;
            OnAreasChanged?.Invoke();
            NotifyAndAutoSave();
        }

        public void ToggleFreePlayUnlockAll()
        {
            SetFreePlayUnlockAll(!FreePlayUnlockAll);
        }

        public int GetAreaWins(BarangayAreaId area)
        {
            int target = (int)area;
            for (int i = 0; i < _areaWins.Count; i++)
            {
                if (_areaWins[i].AreaId == target) return _areaWins[i].WinCount;
            }
            return 0;
        }

        public int GetStarterAreaWins()
        {
            return GetAreaWins(BarangayAreaId.PlayersNeighborhood) + GetAreaWins(BarangayAreaId.SariSariAndKalye);
        }

        public List<string> GetAreaNpcIds(BarangayAreaId area)
        {
            var result = new List<string>();
            var court = TeksMaster.World.BarangayCourtBuilder.Instance;
            if (court != null && court.SpawnedOpponents != null && court.SpawnedOpponents.Count > 0)
            {
                for (int i = 0; i < court.SpawnedOpponents.Count; i++)
                {
                    var npc = court.SpawnedOpponents[i];
                    if (npc == null || npc.AreaId != area) continue;
                    string id = NormalizeId(npc.GetEffectiveNpcId());
                    if (!string.IsNullOrEmpty(id) && !result.Contains(id))
                    {
                        result.Add(id);
                    }
                }
                if (result.Count > 0) return result;
            }

            // Static fallback IDs matching BarangayCourtBuilder.SpawnAllOpponents()
            switch (area)
            {
                case BarangayAreaId.PlayersNeighborhood:
                    result.Add("dodong");
                    result.Add("ate_lorna");
                    result.Add("master_panday");
                    result.Add("ai_nica");
                    break;
                case BarangayAreaId.SariSariAndKalye:
                    result.Add("mark");
                    result.Add("ai_junjun");
                    result.Add("ai_bea");
                    result.Add("manong_driver");
                    result.Add("ai_tonton");
                    break;
                case BarangayAreaId.BasketballCourt:
                    result.Add("carlo");
                    result.Add("ai_gelo");
                    result.Add("ai_kiko");
                    result.Add("ai_may");
                    result.Add("ai_sarah");
                    result.Add("ai_galo");
                    result.Add("ai_mia");
                    result.Add("boss_tisoy");
                    result.Add("kapitan_kid");
                    break;
                case BarangayAreaId.OldNeighborhood:
                    result.Add("joshua");
                    result.Add("ai_toto");
                    result.Add("kuya_bert");
                    result.Add("lolo_teks");
                    result.Add("ai_marco");
                    result.Add("ai_ella");
                    break;
                case BarangayAreaId.FiestaArena:
                    result.Add("teks_master");
                    result.Add("kuya_ronnie");
                    result.Add("boss_macky");
                    result.Add("fiesta_champ_rico");
                    break;
            }

            return result;
        }

        public int GetDefeatedNpcCountInArea(BarangayAreaId area, out int totalInArea)
        {
            var ids = GetAreaNpcIds(area);
            totalInArea = ids.Count;
            int defeated = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (HasDefeatedNpc(ids[i]))
                {
                    defeated++;
                }
            }
            return defeated;
        }

        public bool AreAllNpcsDefeatedInArea(BarangayAreaId area)
        {
            int defeated = GetDefeatedNpcCountInArea(area, out int total);
            return total > 0 && defeated >= total;
        }

        public List<BarangayAreaId> EvaluateAreaUnlocks()
        {
            EnsureStarterCollection();
            var newlyUnlocked = new List<BarangayAreaId>();

            if (!IsAreaStoryUnlocked(BarangayAreaId.BasketballCourt))
            {
                bool beatStarters = HasDefeatedNpc("dodong") && HasDefeatedNpc("mark");
                bool clearedStarterArea = AreAllNpcsDefeatedInArea(BarangayAreaId.PlayersNeighborhood)
                    || AreAllNpcsDefeatedInArea(BarangayAreaId.SariSariAndKalye);
                if (beatStarters || clearedStarterArea || GetStarterAreaWins() >= 2)
                {
                    if (UnlockArea(BarangayAreaId.BasketballCourt))
                    {
                        newlyUnlocked.Add(BarangayAreaId.BasketballCourt);
                    }
                }
            }

            if (!IsAreaStoryUnlocked(BarangayAreaId.OldNeighborhood))
            {
                if (HasDefeatedNpc("carlo")
                    || AreAllNpcsDefeatedInArea(BarangayAreaId.BasketballCourt)
                    || GetAreaWins(BarangayAreaId.BasketballCourt) >= 2)
                {
                    if (UnlockArea(BarangayAreaId.OldNeighborhood))
                    {
                        newlyUnlocked.Add(BarangayAreaId.OldNeighborhood);
                    }
                }
            }

            if (!IsAreaStoryUnlocked(BarangayAreaId.FiestaArena))
            {
                if (HasDefeatedNpc("joshua")
                    || AreAllNpcsDefeatedInArea(BarangayAreaId.OldNeighborhood)
                    || GetAreaWins(BarangayAreaId.OldNeighborhood) >= 2)
                {
                    if (UnlockArea(BarangayAreaId.FiestaArena))
                    {
                        newlyUnlocked.Add(BarangayAreaId.FiestaArena);
                    }
                }
            }

            return newlyUnlocked;
        }

        public string GetAreaUnlockRequirementText(BarangayAreaId area)
        {
            return area switch
            {
                BarangayAreaId.PlayersNeighborhood => "Starting Area (Unlocked)",
                BarangayAreaId.SariSariAndKalye => "Starting Area (Unlocked)",
                BarangayAreaId.BasketballCourt => "Defeat Dodong & Mark (or win 2 Starter Area matches)",
                BarangayAreaId.OldNeighborhood => "Defeat Carlo (or win 2 Basketball Court matches)",
                BarangayAreaId.FiestaArena => "Defeat Joshua (or win 2 Old Neighborhood matches)",
                BarangayAreaId.TournamentArena => "Open Arena (Unlocked — Pay Entry Fee to Compete)",
                _ => "Explore the Barangay"
            };
        }

        // --- Defeated NPCs & Hidden Collectibles ---

        public bool HasDefeatedNpc(string npcId)
        {
            string key = NormalizeId(npcId);
            if (string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < _defeatedNpcIds.Count; i++)
            {
                if (_defeatedNpcIds[i] == key) return true;
            }
            return false;
        }

        public bool MarkNpcDefeated(string npcId)
        {
            return MarkNpcDefeated(npcId, out _);
        }

        public bool MarkNpcDefeated(string npcId, out List<BarangayAreaId> newlyUnlockedAreas)
        {
            EnsureStarterCollection();
            newlyUnlockedAreas = new List<BarangayAreaId>();
            string key = NormalizeId(npcId);
            if (string.IsNullOrEmpty(key)) return false;

            bool isNew = !HasDefeatedNpc(key);
            if (isNew)
            {
                _defeatedNpcIds.Add(key);
            }

            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                AdvanceStoryNpcMission(key);
                SyncAreaClearMissionProgress();
                newlyUnlockedAreas = EvaluateAreaUnlocks();
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            NotifyAndAutoSave();
            return isNew;
        }

        public bool IsHiddenCardDiscovered(string hiddenId)
        {
            string key = NormalizeId(hiddenId);
            if (string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < _discoveredHiddenCardIds.Count; i++)
            {
                if (_discoveredHiddenCardIds[i] == key) return true;
            }
            return false;
        }

        public int CountDiscoveredHiddenCards()
        {
            return _discoveredHiddenCardIds.Count;
        }

        public int CountDiscoveredHiddenCards(IReadOnlyList<string> hiddenIds)
        {
            if (hiddenIds == null) return 0;
            int found = 0;
            for (int i = 0; i < hiddenIds.Count; i++)
            {
                if (IsHiddenCardDiscovered(hiddenIds[i])) found++;
            }
            return found;
        }

        public bool DiscoverHiddenCard(
            string hiddenId,
            string cardId,
            BarangayAreaId areaId,
            int coinBonus,
            int gemBonus,
            out TeksCardDefinition grantedCard)
        {
            EnsureStarterCollection();
            grantedCard = null;
            string key = NormalizeId(hiddenId);
            if (string.IsNullOrEmpty(key) || IsHiddenCardDiscovered(key)) return false;

            grantedCard = TeksDatabase.GetCard(cardId) ?? TeksDatabase.RollCardBySpecRates();
            _discoveredHiddenCardIds.Add(key);

            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                if (grantedCard != null)
                {
                    AddCard(grantedCard.Id, 1);
                }
                if (coinBonus > 0) coins += coinBonus;
                if (gemBonus > 0) gems += gemBonus;
                ReportHiddenCardFound(key, areaId);
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            NotifyAndAutoSave();
            return true;
        }

        // --- Profile, Settings & Daily Trackers ---

        public void SetNickname(string nickname)
        {
            string cleaned = string.IsNullOrWhiteSpace(nickname) ? "TeksMaster" : nickname.Trim();
            if (cleaned.Length > 18) cleaned = cleaned.Substring(0, 18);
            playerNickname = cleaned;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetString(TeksSaveSystem.PrefKeyNickname, playerNickname);
                PlayerPrefs.Save();
            }
            NotifyAndAutoSave();
        }

        public void StartGameWithProfile(string nickname, PlayerCharacterType characterType)
        {
            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                SetNickname(nickname);
                SetCharacterType(characterType);
                HasStartedGame = true;
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }
            NotifyAndAutoSave();
        }

        public static string GetTodayDateKey()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        public bool CanClaimDailyFreeDrawToday()
        {
            return LastDailyFreeDrawDate != GetTodayDateKey();
        }

        public void MarkDailyFreeDrawClaimedToday()
        {
            LastDailyFreeDrawDate = GetTodayDateKey();
            NotifyAndAutoSave();
        }

        public bool CheckAndResetDailyMissionsIfNeeded()
        {
            EnsureStarterCollection();
            string today = GetTodayDateKey();
            if (LastDailyMissionDate == today) return false;

            if (!string.IsNullOrEmpty(LastDailyMissionDate))
            {
                for (int i = 0; i < _quests.Count; i++)
                {
                    if (_quests[i].IsDaily)
                    {
                        _quests[i].CurrentProgress = 0;
                        _quests[i].IsClaimed = false;
                    }
                }
            }

            LastDailyMissionDate = today;
            NotifyAndAutoSave();
            return true;
        }

        public void UpdateAudioSettings(AudioSettingsData newSettings)
        {
            if (newSettings == null) return;
            _audioSettings = newSettings.Clone();
            _audioSettings.Clamp();
            TeksSaveSystem.SaveAudioSettingsToPrefs(_audioSettings);
            OnAudioSettingsChanged?.Invoke(_audioSettings);
            if (!_suppressAutoSave && Application.isPlaying)
            {
                SaveToDisk();
            }
        }

        // --- Mission Progress Reporters (Disjoint: no double-counting) ---

        public void RecordMatchResult(bool won)
        {
            EnsureStarterCollection();
            MatchesPlayed++;
            if (won)
            {
                MatchesWon++;
                WinStreak++;
                if (WinStreak > BestWinStreak) BestWinStreak = WinStreak;

                bool prevSuppress = _suppressAutoSave;
                _suppressAutoSave = true;
                try
                {
                    AdvanceQuestInternal("win_matches", 1);
                    SetQuestProgressMaxInternal("ach_wins_10", MatchesWon);
                    SetQuestProgressMaxInternal("ach_streak_3", BestWinStreak);
                }
                finally
                {
                    _suppressAutoSave = prevSuppress;
                }
            }
            else
            {
                WinStreak = 0;
            }
            NotifyAndAutoSave();
        }

        public List<BarangayAreaId> ReportMatchWin(
            string npcId,
            AIDifficulty difficulty,
            BarangayAreaId areaId,
            bool isChampion = false)
        {
            EnsureStarterCollection();
            IncrementAreaWin(areaId);

            List<BarangayAreaId> newlyUnlocked;
            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                if (!string.IsNullOrEmpty(npcId))
                {
                    string key = NormalizeId(npcId);
                    if (!HasDefeatedNpc(key))
                    {
                        _defeatedNpcIds.Add(key);
                    }
                    AdvanceStoryNpcMission(key);
                }

                if (isChampion)
                {
                    SetQuestProgressMaxInternal("story_defeat_champion", 1);
                }

                SyncAreaClearMissionProgress();
                newlyUnlocked = EvaluateAreaUnlocks();
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            NotifyAndAutoSave();
            return newlyUnlocked;
        }

        public void ReportHiddenCardFound(string hiddenId, BarangayAreaId areaId)
        {
            EnsureStarterCollection();
            int totalFound = _discoveredHiddenCardIds.Count;
            SetQuestProgressMaxInternal("exp_hidden_1", totalFound);
            SetQuestProgressMaxInternal("exp_hidden_6", totalFound);
            SetQuestProgressMaxInternal("exp_hidden_12", totalFound);
            NotifyAndAutoSave();
        }

        public void ReportAreaUnlocked(BarangayAreaId areaId)
        {
            EnsureStarterCollection();
            if (areaId == BarangayAreaId.BasketballCourt)
            {
                SetQuestProgressMaxInternal("story_unlock_court", 1);
            }
            NotifyAndAutoSave();
        }

        public void ReportCardUpgraded(string cardId, int newLevel)
        {
            EnsureStarterCollection();
            AdvanceQuestInternal("daily_upgrade_card", 1);
            if (newLevel >= 5)
            {
                SetQuestProgressMaxInternal("col_upgrade_lv5", 1);
            }
            NotifyAndAutoSave();
        }

        public void ReportFusion(string resultCardId, int fusionStage)
        {
            EnsureStarterCollection();
            AdvanceQuestInternal("fuse_card", 1);
            NotifyAndAutoSave();
        }

        public void ReportPackOpened(ShopPackType packType, int cardCount)
        {
            EnsureStarterCollection();
            AdvanceQuestInternal("open_pack", 1);
            NotifyAndAutoSave();
        }

        public void RecordBoosterOpened()
        {
            ReportPackOpened(ShopPackType.Basic, 3);
        }

        public void ReportTournamentWon()
        {
            EnsureStarterCollection();
            TournamentsWon++;
            SetQuestProgressMaxInternal("ach_tournament_1", TournamentsWon);
            NotifyAndAutoSave();
        }

        public void ReportChallengeCompleted(ChallengeRuleType rule)
        {
            EnsureStarterCollection();
            ChallengesCompleted++;
            SetQuestProgressMaxInternal("ach_challenge_1", ChallengesCompleted);
            NotifyAndAutoSave();
        }

        // --- Mission Queries & Reward Claiming ---

        public List<BarangayQuest> GetQuestsByCategory(MissionCategory category)
        {
            EnsureStarterCollection();
            var list = new List<BarangayQuest>();
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Category == category)
                {
                    list.Add(_quests[i]);
                }
            }
            return list;
        }

        public BarangayQuest GetActiveTrackerMission()
        {
            EnsureStarterCollection();

            // 1. Prioritize any completed, unclaimed mission so the player knows to claim it
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].IsCompleted && !_quests[i].IsClaimed)
                {
                    return _quests[i];
                }
            }

            // 2. Otherwise prioritize incomplete MainStory -> Daily -> Exploration -> Collection -> Achievement
            MissionCategory[] order = new MissionCategory[]
            {
                MissionCategory.MainStory,
                MissionCategory.Daily,
                MissionCategory.Exploration,
                MissionCategory.Collection,
                MissionCategory.Achievement
            };

            for (int c = 0; c < order.Length; c++)
            {
                for (int i = 0; i < _quests.Count; i++)
                {
                    if (_quests[i].Category == order[c] && !_quests[i].IsClaimed)
                    {
                        return _quests[i];
                    }
                }
            }

            return null;
        }

        public bool TryClaimQuest(int index, out string message)
        {
            return TryClaimQuest(index, out message, out _);
        }

        public bool TryClaimQuestById(string questId, out string message, out List<TeksCardDefinition> bonusCards)
        {
            EnsureStarterCollection();
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Id == questId)
                {
                    return TryClaimQuest(i, out message, out bonusCards);
                }
            }

            bonusCards = new List<TeksCardDefinition>();
            message = "Mission not found.";
            return false;
        }

        public bool TryClaimQuest(int index, out string message, out List<TeksCardDefinition> bonusCards)
        {
            EnsureStarterCollection();
            bonusCards = new List<TeksCardDefinition>();

            if (index < 0 || index >= _quests.Count)
            {
                message = "Invalid quest.";
                return false;
            }

            var q = _quests[index];
            if (q.IsClaimed)
            {
                message = "Already claimed!";
                return false;
            }
            if (!q.IsCompleted)
            {
                message = $"Complete mission first ({q.CurrentProgress}/{q.TargetCount}).";
                return false;
            }

            var rewardParts = new List<string>();
            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                q.IsClaimed = true;

                if (q.CoinReward > 0)
                {
                    coins += q.CoinReward;
                    rewardParts.Add($"+{q.CoinReward} Coins");
                }

                if (q.GemReward > 0)
                {
                    gems += q.GemReward;
                    rewardParts.Add($"+{q.GemReward} Gems");
                }

                if (q.HasPackReward)
                {
                    var rolled = TeksDatabase.RollPackCards(q.PackRewardType);
                    for (int i = 0; i < rolled.Count; i++)
                    {
                        if (rolled[i] != null)
                        {
                            AddCard(rolled[i].Id, 1);
                            bonusCards.Add(rolled[i]);
                        }
                    }
                    rewardParts.Add($"+{q.PackRewardType} Pack ({rolled.Count} cards)");
                }

                if (!string.IsNullOrEmpty(q.SpecialCardRewardId))
                {
                    var special = TeksDatabase.GetCard(q.SpecialCardRewardId);
                    if (special != null)
                    {
                        AddCard(special.Id, 1);
                        bonusCards.Add(special);
                        rewardParts.Add($"+{special.Name}");
                    }
                }
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            message = rewardParts.Count > 0
                ? $"Mission Claimed! {string.Join(", ", rewardParts)}!"
                : "Mission Claimed!";
            NotifyAndAutoSave();
            return true;
        }

        public int GetUnclaimedCompletedQuestCount()
        {
            EnsureStarterCollection();
            int c = 0;
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].IsCompleted && !_quests[i].IsClaimed) c++;
            }
            return c;
        }

        public int GetUnclaimedCompletedQuestCount(MissionCategory category)
        {
            EnsureStarterCollection();
            int c = 0;
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Category == category && _quests[i].IsCompleted && !_quests[i].IsClaimed) c++;
            }
            return c;
        }

        public string GetBarangayRankTitle()
        {
            if (MatchesWon >= 15) return "GRAND TEKS MASTER";
            if (MatchesWon >= 10) return "Barangay Legend";
            if (MatchesWon >= 6) return "Court Champion";
            if (MatchesWon >= 3) return "Kanto Striker";
            if (MatchesWon >= 1) return "Kalye Kid";
            return "Baguhan (Rookie)";
        }

        public float GetStreakCoinMultiplier()
        {
            if (WinStreak < 2) return 1.0f;
            return Mathf.Min(2.0f, 1.0f + (WinStreak - 1) * 0.25f);
        }

        public float GetStreakSweetSpotBonus()
        {
            return WinStreak >= 2 ? 0.03f : 0f;
        }

        public void CycleCharacterType(int delta = 1)
        {
            SetCharacterType(TeksCharacterPresets.FromIndex((int)selectedCharacterType + delta));
        }

        public void SetCharacterType(PlayerCharacterType characterType)
        {
            PlayerCharacterType normalized = TeksCharacterPresets.FromIndex((int)characterType);
            bool changed = selectedCharacterType != normalized;
            selectedCharacterType = normalized;
            if (Application.isPlaying)
            {
                int idx = (int)selectedCharacterType;
                PlayerPrefs.SetInt(TeksSaveSystem.PrefKeyCharacterType, idx);
                PlayerPrefs.SetInt(TeksSaveSystem.PrefKeyGender, idx);
                PlayerPrefs.Save();
            }
            if (changed)
            {
                OnCharacterTypeChanged?.Invoke(selectedCharacterType);
                NotifyAndAutoSave();
            }
        }

        public void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        // --- Persistence (JSON + PlayerPrefs) ---

        public bool SaveToDisk()
        {
            if (!Application.isPlaying) return false;
            EnsureStarterCollection();
            var data = ExportSaveData();
            return TeksSaveSystem.Save(data);
        }

        public bool LoadFromDisk()
        {
            if (!Application.isPlaying) return false;
            EnsureStarterCollection();

            if (!TeksSaveSystem.TryLoad(out var data) || data == null)
            {
                return false;
            }

            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                ApplySaveData(data);
                CheckAndResetDailyMissionsIfNeeded();
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            OnCharacterTypeChanged?.Invoke(selectedCharacterType);
            OnAreasChanged?.Invoke();
            OnAudioSettingsChanged?.Invoke(_audioSettings);
            OnInventoryChanged?.Invoke();
            return true;
        }

        private TeksSaveData ExportSaveData()
        {
            var data = new TeksSaveData
            {
                SaveVersion = 1,
                PlayerNickname = PlayerNickname,
                SelectedGender = (int)selectedCharacterType,
                SelectedCharacterType = (int)selectedCharacterType,
                HasStartedGame = HasStartedGame,
                Coins = coins,
                Gems = gems,
                EquippedPamatoId = equippedPamatoId,
                MatchesWon = MatchesWon,
                MatchesPlayed = MatchesPlayed,
                WinStreak = WinStreak,
                BestWinStreak = BestWinStreak,
                TournamentsWon = TournamentsWon,
                ChallengesCompleted = ChallengesCompleted,
                FreePlayUnlockAll = FreePlayUnlockAll,
                LastDailyMissionDate = LastDailyMissionDate ?? string.Empty,
                LastDailyFreeDrawDate = LastDailyFreeDrawDate ?? string.Empty,
                AudioSettings = _audioSettings != null ? _audioSettings.Clone() : new AudioSettingsData()
            };

            for (int i = 0; i < ownedCards.Count; i++)
            {
                var c = ownedCards[i];
                data.OwnedCards.Add(new OwnedTeksCard(c.CardId, c.StarRank, c.DuplicateCount, c.FusionStage));
            }

            for (int i = 0; i < _unlockedAreas.Count; i++)
            {
                data.UnlockedAreas.Add((int)_unlockedAreas[i]);
            }

            for (int i = 0; i < _areaWins.Count; i++)
            {
                data.AreaWins.Add(new AreaWinRecord((BarangayAreaId)_areaWins[i].AreaId, _areaWins[i].WinCount));
            }

            for (int i = 0; i < _defeatedNpcIds.Count; i++)
            {
                data.DefeatedNpcIds.Add(_defeatedNpcIds[i]);
            }

            for (int i = 0; i < _discoveredHiddenCardIds.Count; i++)
            {
                data.DiscoveredHiddenCardIds.Add(_discoveredHiddenCardIds[i]);
            }

            for (int i = 0; i < _quests.Count; i++)
            {
                var q = _quests[i];
                data.Quests.Add(new BarangayQuest
                {
                    Id = q.Id,
                    Title = q.Title,
                    Description = q.Description,
                    Category = q.Category,
                    CurrentProgress = q.CurrentProgress,
                    TargetCount = q.TargetCount,
                    CoinReward = q.CoinReward,
                    GemReward = q.GemReward,
                    HasPackReward = q.HasPackReward,
                    PackRewardType = q.PackRewardType,
                    SpecialCardRewardId = q.SpecialCardRewardId,
                    IsDaily = q.IsDaily,
                    IsClaimed = q.IsClaimed
                });
            }

            return data;
        }

        private void ApplySaveData(TeksSaveData data)
        {
            playerNickname = data.PlayerNickname;
            int resolvedChar = data.SelectedCharacterType >= 0
                ? data.SelectedCharacterType
                : (data.SelectedGender == 1 ? (int)PlayerCharacterType.SportyAce : (int)PlayerCharacterType.StreetKid);
            selectedCharacterType = (PlayerCharacterType)Mathf.Clamp(resolvedChar, 0, TeksCharacterPresets.Count - 1);
            HasStartedGame = data.HasStartedGame;
            coins = Mathf.Max(0, data.Coins);
            gems = Mathf.Max(0, data.Gems);
            MatchesWon = data.MatchesWon;
            MatchesPlayed = data.MatchesPlayed;
            WinStreak = data.WinStreak;
            BestWinStreak = data.BestWinStreak;
            TournamentsWon = data.TournamentsWon;
            ChallengesCompleted = data.ChallengesCompleted;
            FreePlayUnlockAll = data.FreePlayUnlockAll;
            LastDailyMissionDate = data.LastDailyMissionDate ?? string.Empty;
            LastDailyFreeDrawDate = data.LastDailyFreeDrawDate ?? string.Empty;
            _audioSettings = data.AudioSettings != null ? data.AudioSettings.Clone() : new AudioSettingsData();
            _audioSettings.Clamp();

            if (data.OwnedCards != null && data.OwnedCards.Count > 0)
            {
                ownedCards.Clear();
                for (int i = 0; i < data.OwnedCards.Count; i++)
                {
                    var c = data.OwnedCards[i];
                    ownedCards.Add(new OwnedTeksCard(c.CardId, c.StarRank, c.DuplicateCount, c.FusionStage));
                }
            }

            equippedPamatoId = data.EquippedPamatoId;
            if (string.IsNullOrEmpty(equippedPamatoId) || GetOwnedCardInternal(equippedPamatoId) == null)
            {
                if (ownedCards.Count > 0) equippedPamatoId = ownedCards[0].CardId;
            }

            _unlockedAreas.Clear();
            EnsureDefaultUnlockedAreas();
            if (data.UnlockedAreas != null)
            {
                for (int i = 0; i < data.UnlockedAreas.Count; i++)
                {
                    int raw = data.UnlockedAreas[i];
                    if (raw >= 0 && raw <= 5) AddUnlockedAreaInternal((BarangayAreaId)raw);
                }
            }

            _areaWins.Clear();
            if (data.AreaWins != null)
            {
                for (int i = 0; i < data.AreaWins.Count; i++)
                {
                    var rec = data.AreaWins[i];
                    if (rec != null && rec.AreaId >= 0 && rec.AreaId <= 5)
                    {
                        _areaWins.Add(new AreaWinRecord((BarangayAreaId)rec.AreaId, rec.WinCount));
                    }
                }
            }

            _defeatedNpcIds.Clear();
            if (data.DefeatedNpcIds != null)
            {
                for (int i = 0; i < data.DefeatedNpcIds.Count; i++)
                {
                    string key = NormalizeId(data.DefeatedNpcIds[i]);
                    if (!string.IsNullOrEmpty(key) && !HasDefeatedNpc(key))
                    {
                        _defeatedNpcIds.Add(key);
                    }
                }
            }

            _discoveredHiddenCardIds.Clear();
            if (data.DiscoveredHiddenCardIds != null)
            {
                for (int i = 0; i < data.DiscoveredHiddenCardIds.Count; i++)
                {
                    string key = NormalizeId(data.DiscoveredHiddenCardIds[i]);
                    if (!string.IsNullOrEmpty(key) && !IsHiddenCardDiscovered(key))
                    {
                        _discoveredHiddenCardIds.Add(key);
                    }
                }
            }

            _quests.Clear();
            if (data.Quests != null)
            {
                for (int i = 0; i < data.Quests.Count; i++)
                {
                    var q = data.Quests[i];
                    if (q == null || string.IsNullOrEmpty(q.Id)) continue;
                    _quests.Add(q);
                }
            }

            EnsureAllMissionTemplates();
            EvaluateAreaUnlocks();
            SyncPassiveMissionProgress();
        }

        // --- Internal Helpers ---

        private void NotifyAndAutoSave()
        {
            if (!_suppressAutoSave && Application.isPlaying)
            {
                SaveToDisk();
            }
            OnInventoryChanged?.Invoke();
        }

        private BarangayQuest FindQuestById(string questId)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Id == questId) return _quests[i];
            }
            return null;
        }

        private void AdvanceQuestInternal(string questId, int amount)
        {
            if (amount <= 0) return;
            var q = FindQuestById(questId);
            if (q != null && !q.IsClaimed)
            {
                q.CurrentProgress = Mathf.Min(q.TargetCount, q.CurrentProgress + amount);
            }
        }

        private void SetQuestProgressMaxInternal(string questId, int value)
        {
            var q = FindQuestById(questId);
            if (q != null && !q.IsClaimed)
            {
                int clamped = Mathf.Clamp(value, 0, q.TargetCount);
                if (clamped > q.CurrentProgress)
                {
                    q.CurrentProgress = clamped;
                }
            }
        }

        private void AdvanceStoryNpcMission(string normalizedNpcId)
        {
            switch (normalizedNpcId)
            {
                case "dodong":
                    SetQuestProgressMaxInternal("story_defeat_dodong", 1);
                    break;
                case "mark":
                    SetQuestProgressMaxInternal("story_defeat_mark", 1);
                    break;
                case "carlo":
                    SetQuestProgressMaxInternal("story_defeat_carlo", 1);
                    break;
                case "joshua":
                    SetQuestProgressMaxInternal("story_defeat_joshua", 1);
                    break;
                case "teks_master":
                    SetQuestProgressMaxInternal("story_defeat_champion", 1);
                    break;
            }
        }

        private void IncrementAreaWin(BarangayAreaId area)
        {
            int target = (int)area;
            for (int i = 0; i < _areaWins.Count; i++)
            {
                if (_areaWins[i].AreaId == target)
                {
                    _areaWins[i].WinCount += 1;
                    return;
                }
            }
            _areaWins.Add(new AreaWinRecord(area, 1));
        }

        private void SyncCollectionMissionProgress()
        {
            int uniqueOwned = ownedCards.Count;
            SetQuestProgressMaxInternal("col_own_12", uniqueOwned);
            SetQuestProgressMaxInternal("col_own_25", uniqueOwned);
        }

        public void SyncAreaClearMissionProgress()
        {
            SyncSingleAreaClearMission("area_clear_neighborhood", BarangayAreaId.PlayersNeighborhood);
            SyncSingleAreaClearMission("area_clear_kalye", BarangayAreaId.SariSariAndKalye);
            SyncSingleAreaClearMission("area_clear_court", BarangayAreaId.BasketballCourt);
            SyncSingleAreaClearMission("area_clear_old_neighborhood", BarangayAreaId.OldNeighborhood);
            SyncSingleAreaClearMission("area_clear_fiesta", BarangayAreaId.FiestaArena);
        }

        private void SyncSingleAreaClearMission(string questId, BarangayAreaId area)
        {
            int defeated = GetDefeatedNpcCountInArea(area, out int totalInArea);
            var q = FindQuestById(questId);
            if (q != null)
            {
                if (totalInArea > 0)
                {
                    q.TargetCount = totalInArea;
                }
                SetQuestProgressMaxInternal(questId, defeated);
            }
        }

        private void SyncPassiveMissionProgress()
        {
            SyncCollectionMissionProgress();
            SetQuestProgressMaxInternal("exp_hidden_1", _discoveredHiddenCardIds.Count);
            SetQuestProgressMaxInternal("exp_hidden_6", _discoveredHiddenCardIds.Count);
            SetQuestProgressMaxInternal("exp_hidden_12", _discoveredHiddenCardIds.Count);
            SetQuestProgressMaxInternal("ach_wins_10", MatchesWon);
            SetQuestProgressMaxInternal("ach_streak_3", BestWinStreak);
            SetQuestProgressMaxInternal("ach_tournament_1", TournamentsWon);
            SetQuestProgressMaxInternal("ach_challenge_1", ChallengesCompleted);
            if (IsAreaStoryUnlocked(BarangayAreaId.BasketballCourt))
            {
                SetQuestProgressMaxInternal("story_unlock_court", 1);
            }
            for (int i = 0; i < _defeatedNpcIds.Count; i++)
            {
                AdvanceStoryNpcMission(_defeatedNpcIds[i]);
            }
            SyncAreaClearMissionProgress();
        }

        public void ResetProgressToZero()
        {
            bool prevSuppress = _suppressAutoSave;
            _suppressAutoSave = true;
            try
            {
                coins = 2450;
                gems = 25;
                equippedPamatoId = "hero";

                MatchesWon = 0;
                MatchesPlayed = 0;
                WinStreak = 0;
                BestWinStreak = 0;
                TournamentsWon = 0;
                ChallengesCompleted = 0;
                FreePlayUnlockAll = false;
                LastDailyMissionDate = string.Empty;
                LastDailyFreeDrawDate = string.Empty;

                ownedCards.Clear();
                _unlockedAreas.Clear();
                _areaWins.Clear();
                _defeatedNpcIds.Clear();
                _discoveredHiddenCardIds.Clear();
                _quests.Clear();

                _initialized = false;
                EnsureStarterCollection();
                CheckAndResetDailyMissionsIfNeeded();

                TeksSaveSystem.DeleteSaveFilesForce();
            }
            finally
            {
                _suppressAutoSave = prevSuppress;
            }

            if (Application.isPlaying)
            {
                SaveToDisk();
            }

            OnAreasChanged?.Invoke();
            OnInventoryChanged?.Invoke();
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim().ToLowerInvariant();
        }
    }
}
