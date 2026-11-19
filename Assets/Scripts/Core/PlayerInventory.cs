using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeksMaster.Core
{
    [Serializable]
    public class BarangayQuest
    {
        public string Id;
        public string Title;
        public int CurrentProgress;
        public int TargetCount;
        public int CoinReward;
        public bool IsClaimed;

        public bool IsCompleted => CurrentProgress >= TargetCount;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private string playerNickname = "TeksMaster";
        [SerializeField] private int coins = 2450;
        [SerializeField] private PlayerGender selectedGender = PlayerGender.Male;
        [SerializeField] private string equippedPamatoId = "hero";
        [SerializeField] private List<OwnedTeksCard> ownedCards = new List<OwnedTeksCard>();

        public string PlayerNickname => string.IsNullOrWhiteSpace(playerNickname) ? "TeksMaster" : playerNickname;
        public bool HasStartedGame { get; private set; }
        public int Coins => coins;
        public PlayerGender SelectedGender => selectedGender;
        public string EquippedPamatoId => equippedPamatoId;
        public IReadOnlyList<OwnedTeksCard> OwnedCards => ownedCards;
        public IReadOnlyList<BarangayQuest> Quests => _quests;
        public int MatchesWon { get; private set; }
        public int MatchesPlayed { get; private set; }
        public int WinStreak { get; private set; }
        public int BestWinStreak { get; private set; }

        public event Action OnInventoryChanged;
        public event Action<PlayerGender> OnGenderChanged;

        private readonly List<BarangayQuest> _quests = new List<BarangayQuest>();
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            LoadSavedProfile();
            EnsureStarterCollection();
        }

        private void LoadSavedProfile()
        {
            if (PlayerPrefs.HasKey("Teks_Nickname"))
            {
                string saved = PlayerPrefs.GetString("Teks_Nickname", "TeksMaster");
                if (!string.IsNullOrWhiteSpace(saved)) playerNickname = saved;
            }
            if (PlayerPrefs.HasKey("Teks_Gender"))
            {
                selectedGender = (PlayerGender)Mathf.Clamp(PlayerPrefs.GetInt("Teks_Gender", 0), 0, 1);
            }
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

            if (string.IsNullOrEmpty(equippedPamatoId) || GetOwnedCard(equippedPamatoId) == null)
            {
                equippedPamatoId = ownedCards[0].CardId;
            }

            if (_quests.Count == 0)
            {
                _quests.Add(new BarangayQuest { Id = "win_matches", Title = "Win 2 Teks Matches", TargetCount = 2, CoinReward = 250 });
                _quests.Add(new BarangayQuest { Id = "fuse_card", Title = "Fuse 1 Card in Fusion Lab", TargetCount = 1, CoinReward = 300 });
                _quests.Add(new BarangayQuest { Id = "open_pack", Title = "Open 1 Booster Pack at Shop", TargetCount = 1, CoinReward = 200 });
            }
        }

        public OwnedTeksCard GetOwnedCard(string cardId)
        {
            EnsureStarterCollection();
            for (int i = 0; i < ownedCards.Count; i++)
            {
                if (ownedCards[i].CardId == cardId) return ownedCards[i];
            }
            return null;
        }

        public OwnedTeksCard GetEquippedPamato(out TeksCardDefinition definition)
        {
            EnsureStarterCollection();
            var owned = GetOwnedCard(equippedPamatoId);
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
            OnInventoryChanged?.Invoke();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0) return false;
            if (coins < amount) return false;
            coins -= amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void AddCard(string cardId, int count = 1)
        {
            EnsureStarterCollection();
            if (count <= 0 || TeksDatabase.GetCard(cardId) == null) return;

            var existing = GetOwnedCard(cardId);
            if (existing != null)
            {
                existing.DuplicateCount += count;
            }
            else
            {
                ownedCards.Add(new OwnedTeksCard(cardId, 1, count - 1));
            }

            OnInventoryChanged?.Invoke();
        }

        public void EquipPamato(string cardId)
        {
            var owned = GetOwnedCard(cardId);
            if (owned == null) return;
            equippedPamatoId = cardId;
            OnInventoryChanged?.Invoke();
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
                statusMsg = "MAX 5★ RANK";
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

            statusMsg = $"Upgrade -> {owned.StarRank + 1}★ ({coinCost} Coins + {dupesNeeded} Dup)";
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

            coins -= coinCost;
            owned.DuplicateCount -= dupesNeeded;
            owned.StarRank = Mathf.Clamp(owned.StarRank + 1, 1, 5);

            message = $"{def.Name} ranked up to {owned.StarRank}★! Foil glow & Flick Luck boosted!";
            OnInventoryChanged?.Invoke();
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
            OnInventoryChanged?.Invoke();
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
            if (ownB == null)
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

            var ownA = GetOwnedCard(recipe.IngredientAId);
            var ownB = GetOwnedCard(recipe.IngredientBId);
            resultCard = TeksDatabase.GetCard(recipe.ResultCardId);
            if (resultCard == null)
            {
                message = "Fusion target card not found.";
                return false;
            }

            coins -= recipe.CoinCost;
            if (ownA != null && ownA.DuplicateCount > 0) ownA.DuplicateCount -= 1;
            if (ownB != null && ownB.DuplicateCount > 0) ownB.DuplicateCount -= 1;

            AddCard(resultCard.Id, 1);
            AdvanceQuest("fuse_card", 1);
            message = $"FUSION SUCCESS! Forged {resultCard.Name} ({TeksDatabase.GetRarityName(resultCard.Rarity)})!";
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
                    availableDupes += ownedCards[i].DuplicateCount;
                }
            }

            if (availableDupes < 3)
            {
                reason = $"Need 3 {TeksDatabase.GetRarityName(rarity)} Dupes ({availableDupes}/3)";
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

            int toConsume = 3;
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

            coins -= coinCost;
            resultCard = TeksDatabase.RollCardByRarity(nextRarity);
            if (resultCard != null)
            {
                AddCard(resultCard.Id, 1);
                AdvanceQuest("fuse_card", 1);
                message = $"SIMILAR FUSION! 3 {TeksDatabase.GetRarityName(rarity)} Dupes -> {resultCard.Name} ({TeksDatabase.GetRarityName(resultCard.Rarity)})!";
                return true;
            }

            message = "Fusion failed.";
            return false;
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
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void SetNickname(string nickname)
        {
            string cleaned = string.IsNullOrWhiteSpace(nickname) ? "TeksMaster" : nickname.Trim();
            if (cleaned.Length > 18) cleaned = cleaned.Substring(0, 18);
            playerNickname = cleaned;
            PlayerPrefs.SetString("Teks_Nickname", playerNickname);
            PlayerPrefs.Save();
            OnInventoryChanged?.Invoke();
        }

        public void StartGameWithProfile(string nickname, PlayerGender gender)
        {
            SetNickname(nickname);
            SetGender(gender);
            HasStartedGame = true;
            OnInventoryChanged?.Invoke();
        }

        public void RecordBoosterOpened()
        {
            AdvanceQuest("open_pack", 1);
        }

        private void AdvanceQuest(string questId, int amount)
        {
            EnsureStarterCollection();
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Id == questId && !_quests[i].IsClaimed)
                {
                    _quests[i].CurrentProgress = Mathf.Min(_quests[i].TargetCount, _quests[i].CurrentProgress + amount);
                }
            }
            OnInventoryChanged?.Invoke();
        }

        public bool TryClaimQuest(int index, out string message)
        {
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
                message = $"Complete quest first ({q.CurrentProgress}/{q.TargetCount}).";
                return false;
            }

            q.IsClaimed = true;
            coins += q.CoinReward;
            message = $"Quest Claimed! +{q.CoinReward} Barya Coins!";
            OnInventoryChanged?.Invoke();
            return true;
        }

        public int GetUnclaimedCompletedQuestCount()
        {
            int c = 0;
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].IsCompleted && !_quests[i].IsClaimed) c++;
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

        public void RecordMatchResult(bool won)
        {
            MatchesPlayed++;
            if (won)
            {
                MatchesWon++;
                WinStreak++;
                if (WinStreak > BestWinStreak) BestWinStreak = WinStreak;
                AdvanceQuest("win_matches", 1);
            }
            else
            {
                WinStreak = 0;
            }
            OnInventoryChanged?.Invoke();
        }

        public void ToggleGender()
        {
            SetGender(selectedGender == PlayerGender.Male ? PlayerGender.Female : PlayerGender.Male);
        }

        public void SetGender(PlayerGender gender)
        {
            bool changed = selectedGender != gender;
            selectedGender = gender;
            PlayerPrefs.SetInt("Teks_Gender", (int)selectedGender);
            PlayerPrefs.Save();
            if (changed)
            {
                OnGenderChanged?.Invoke(selectedGender);
                OnInventoryChanged?.Invoke();
            }
        }

        public void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
