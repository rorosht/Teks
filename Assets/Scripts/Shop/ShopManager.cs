using System;
using System.Collections.Generic;
using UnityEngine;
using TeksMaster.Core;

namespace TeksMaster.Shop
{
    [Serializable]
    public class ShopSlot
    {
        public int SlotIndex;
        public TeksCardDefinition Card;
        public int OriginalPrice;
        public int Price;
        public bool IsFlashSale;
        public bool IsSuperSlot;
        public bool IsSoldOut;
    }

    public readonly struct ShopPackInfo
    {
        public ShopPackType PackType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string OddsSummary { get; }
        public int CardCount { get; }
        public int Cost { get; }
        public bool UsesGems { get; }
        public bool IsDailyFree { get; }
        public int BonusBarya { get; }

        public ShopPackInfo(
            ShopPackType packType,
            string displayName,
            string description,
            string oddsSummary,
            int cardCount,
            int cost,
            bool usesGems,
            bool isDailyFree,
            int bonusBarya = 0)
        {
            PackType = packType;
            DisplayName = displayName;
            Description = description;
            OddsSummary = oddsSummary;
            CardCount = cardCount;
            Cost = cost;
            UsesGems = usesGems;
            IsDailyFree = isDailyFree;
            BonusBarya = bonusBarya;
        }

        public string GetFormattedCost()
        {
            if (IsDailyFree) return "FREE DAILY";
            return UsesGems ? $"{Cost} Gems" : $"{Cost} Coins";
        }
    }

    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public const float RotationIntervalSeconds = 300f;

        // SPEC Pack & Dual-Currency Constants
        public const int BasicPackCost = 150;
        public const int PremiumPackCost = 300;
        public const int RarePackCost = 400;
        public const int SpecialGemPackCost = 30;
        public const int DailyFreeBaryaAmount = 200;

        // Preserved Legacy Constants
        public const int KalyeBoosterCost = BasicPackCost;
        public const int AlamatSuperPackCost = RarePackCost;
        public const int MythicBoxCost = 850;
        public const int ManualRefreshCost = 50;

        private static readonly ShopPackInfo[] PackCatalog = new ShopPackInfo[]
        {
            new ShopPackInfo(
                ShopPackType.Basic,
                "Basic Card Pack",
                "3 Teks cards using classic Barangay drop rates.",
                "55% Common / 25% Uncommon / 12% Rare / 6% Epic / 2% Legendary",
                3,
                BasicPackCost,
                usesGems: false,
                isDailyFree: false,
                bonusBarya: 0),
            new ShopPackInfo(
                ShopPackType.Premium,
                "Premium Card Pack",
                "5 Teks cards with a guaranteed Uncommon+ 5th card.",
                "4 Classic SPEC Rolls + 1 Guaranteed Uncommon+",
                5,
                PremiumPackCost,
                usesGems: false,
                isDailyFree: false,
                bonusBarya: 0),
            new ShopPackInfo(
                ShopPackType.Rare,
                "Rare Card Pack",
                "4 boosted Teks cards with high Rare, Epic & Legendary odds.",
                "Boosted Rare+ Pool & Guaranteed Rare+ 4th Card",
                4,
                RarePackCost,
                usesGems: false,
                isDailyFree: false,
                bonusBarya: 0),
            new ShopPackInfo(
                ShopPackType.SpecialGem,
                "Special Mythic Gem Pack",
                "5 elite Rare+ cards with a guaranteed Epic or Legendary!",
                "All Rare+ & Guaranteed Epic/Legendary 5th Card",
                5,
                SpecialGemPackCost,
                usesGems: true,
                isDailyFree: false,
                bonusBarya: 0),
            new ShopPackInfo(
                ShopPackType.DailyFree,
                "Daily Free Draw",
                "1 free Teks card + 200 Barya Coins once every day!",
                "1 Classic SPEC Card + 200 Barya Coins",
                1,
                0,
                usesGems: false,
                isDailyFree: true,
                bonusBarya: DailyFreeBaryaAmount)
        };

        [SerializeField] private float secondsUntilRefresh = 299.9f;
        private readonly ShopSlot[] _slots = new ShopSlot[4];
        private bool _initialized;

        public bool CanClaimDailyFreeDraw =>
            PlayerInventory.Instance != null && PlayerInventory.Instance.CanClaimDailyFreeDrawToday();

        public bool HasClaimedDailyBarya => !CanClaimDailyFreeDraw;

        public string LastVendorMessage { get; private set; } =
            "Aling Nena: \"Bili na! May -30% Sulit Sale, Gem Packs, at Daily Free Draw ngayon!\"";

        public float SecondsUntilRefresh => secondsUntilRefresh;

        public IReadOnlyList<ShopSlot> Slots
        {
            get
            {
                EnsureInitialized();
                return _slots;
            }
        }

        public event Action OnShopUpdated;
        public event Action<ShopPackType, List<TeksCardDefinition>> OnPackOpened;
        public event Action OnPurchaseSucceeded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            secondsUntilRefresh = 299.9f;
            RefreshShopSlots(resetTimer: false);
        }

        private void Update()
        {
            EnsureInitialized();
            secondsUntilRefresh -= Time.deltaTime;
            if (secondsUntilRefresh <= 0f)
            {
                RefreshShopSlots(resetTimer: true);
            }
        }

        public ShopPackInfo GetPackInfo(ShopPackType packType)
        {
            for (int i = 0; i < PackCatalog.Length; i++)
            {
                if (PackCatalog[i].PackType == packType)
                {
                    return PackCatalog[i];
                }
            }
            return PackCatalog[0];
        }

        public IReadOnlyList<ShopPackInfo> GetAllPackInfos()
        {
            return PackCatalog;
        }

        public bool CanAffordPack(ShopPackType packType)
        {
            if (packType == ShopPackType.DailyFree)
            {
                return CanClaimDailyFreeDraw;
            }

            var inv = PlayerInventory.Instance;
            if (inv == null) return false;

            ShopPackInfo info = GetPackInfo(packType);
            return info.UsesGems ? inv.Gems >= info.Cost : inv.Coins >= info.Cost;
        }

        public void RefreshShopSlots(bool resetTimer = true)
        {
            if (resetTimer)
            {
                secondsUntilRefresh = RotationIntervalSeconds;
            }

            var usedIds = new HashSet<string>();
            int saleSlotIdx = UnityEngine.Random.Range(0, 4);

            for (int i = 0; i < 4; i++)
            {
                bool isSuper = (i == 3);
                TeksCardDefinition picked = null;

                for (int attempt = 0; attempt < 12; attempt++)
                {
                    var candidate = isSuper
                        ? TeksDatabase.RollSuperSlotCard()
                        : TeksDatabase.RollStandardShopCard();

                    if (candidate != null && !usedIds.Contains(candidate.Id))
                    {
                        picked = candidate;
                        break;
                    }
                }

                if (picked == null)
                {
                    picked = isSuper ? TeksDatabase.RollSuperSlotCard() : TeksDatabase.RollStandardShopCard();
                }

                if (picked != null)
                {
                    usedIds.Add(picked.Id);
                }

                int origPrice = picked != null ? picked.BuyPrice : 100;
                bool isSale = (i == saleSlotIdx);
                int finalPrice = isSale ? Mathf.Max(20, Mathf.RoundToInt(origPrice * 0.70f)) : origPrice;

                _slots[i] = new ShopSlot
                {
                    SlotIndex = i,
                    Card = picked,
                    OriginalPrice = origPrice,
                    Price = finalPrice,
                    IsFlashSale = isSale,
                    IsSuperSlot = isSuper,
                    IsSoldOut = false
                };
            }

            LastVendorMessage = "Aling Nena: \"Bagong deliver ang mga Teks! Check mo yung -30% SULIT SALE!\"";
            OnShopUpdated?.Invoke();
        }

        public bool TryBuySlot(int slotIndex, out string message)
        {
            EnsureInitialized();
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                message = "Invalid shop slot.";
                return false;
            }

            var slot = _slots[slotIndex];
            if (slot == null || slot.Card == null)
            {
                message = "Slot is empty.";
                return false;
            }

            if (slot.IsSoldOut)
            {
                message = "Already sold out! Wait for the 5-minute rotation.";
                return false;
            }

            var inv = PlayerInventory.Instance;
            if (inv == null || !inv.TrySpendCoins(slot.Price))
            {
                message = $"Not enough Barya Coins! Need {slot.Price} Coins.";
                return false;
            }

            inv.AddCard(slot.Card.Id, 1);
            inv.SaveToDisk();

            slot.IsSoldOut = true;
            LastVendorMessage = $"Aling Nena: \"Salamat {inv.PlayerNickname}! Ang ganda ng {slot.Card.Name} na 'yan!\"";
            message = $"Bought {slot.Card.Name} ({TeksDatabase.GetRarityName(slot.Card.Rarity)}) for {slot.Price} Coins!";
            OnPurchaseSucceeded?.Invoke();
            OnShopUpdated?.Invoke();
            return true;
        }

        public bool TryBuyPack(ShopPackType packType, out List<TeksCardDefinition> drawnCards, out string message)
        {
            EnsureInitialized();
            drawnCards = null;

            if (packType == ShopPackType.DailyFree)
            {
                return TryClaimDailyFreeDraw(out drawnCards, out _, out message);
            }

            var inv = PlayerInventory.Instance;
            if (inv == null)
            {
                message = "Player inventory is not ready.";
                return false;
            }

            ShopPackInfo info = GetPackInfo(packType);
            if (info.UsesGems)
            {
                if (!inv.TrySpendGems(info.Cost))
                {
                    message = $"Not enough Gems! Need {info.Cost} Gems.";
                    return false;
                }
            }
            else
            {
                if (!inv.TrySpendCoins(info.Cost))
                {
                    message = $"Not enough Barya Coins! Need {info.Cost} Coins.";
                    return false;
                }
            }

            drawnCards = TeksDatabase.RollPackCards(packType) ?? new List<TeksCardDefinition>();
            for (int i = 0; i < drawnCards.Count; i++)
            {
                if (drawnCards[i] != null)
                {
                    inv.AddCard(drawnCards[i].Id, 1);
                }
            }

            inv.ReportPackOpened(packType, drawnCards.Count);
            inv.SaveToDisk();

            LastVendorMessage = BuildVendorPackMessage(packType, info.DisplayName, inv.PlayerNickname);
            message = packType == ShopPackType.SpecialGem
                ? $"Opened {info.DisplayName}! Obtained {drawnCards.Count} Teks cards (Guaranteed Epic+)!"
                : $"Opened {info.DisplayName}! Obtained {drawnCards.Count} Teks cards!";

            OnPurchaseSucceeded?.Invoke();
            OnPackOpened?.Invoke(packType, drawnCards);
            OnShopUpdated?.Invoke();
            return true;
        }

        public bool TryClaimDailyFreeDraw(out List<TeksCardDefinition> drawnCards, out int baryaBonus, out string message)
        {
            EnsureInitialized();
            drawnCards = null;
            baryaBonus = 0;

            var inv = PlayerInventory.Instance;
            if (inv == null)
            {
                message = "Player inventory is not ready.";
                return false;
            }

            if (!inv.CanClaimDailyFreeDrawToday())
            {
                message = "Already claimed today's Daily Free Draw! Come back tomorrow.";
                return false;
            }

            // Mark claimed BEFORE firing any events to prevent re-entrant double claims
            inv.MarkDailyFreeDrawClaimedToday();

            drawnCards = TeksDatabase.RollPackCards(ShopPackType.DailyFree) ?? new List<TeksCardDefinition>();
            baryaBonus = DailyFreeBaryaAmount;
            inv.AddCoins(baryaBonus);

            for (int i = 0; i < drawnCards.Count; i++)
            {
                if (drawnCards[i] != null)
                {
                    inv.AddCard(drawnCards[i].Id, 1);
                }
            }

            inv.ReportPackOpened(ShopPackType.DailyFree, drawnCards.Count);
            inv.SaveToDisk();

            string cardLabel = (drawnCards.Count > 0 && drawnCards[0] != null)
                ? $"{drawnCards[0].Name} ({TeksDatabase.GetRarityName(drawnCards[0].Rarity)})"
                : "1 Teks Card";

            LastVendorMessage = $"Aling Nena: \"Regalo sa'yo today, {inv.PlayerNickname}! {cardLabel} + {baryaBonus} Barya Coins!\"";
            message = $"Daily Free Draw claimed! Got {cardLabel} + {baryaBonus} Barya Coins!";

            OnPurchaseSucceeded?.Invoke();
            OnPackOpened?.Invoke(ShopPackType.DailyFree, drawnCards);
            OnShopUpdated?.Invoke();
            return true;
        }

        public bool TryBuyBoosterPack(bool isSuperPack, out List<TeksCardDefinition> drawnCards, out string message)
        {
            ShopPackType mappedPack = isSuperPack ? ShopPackType.Rare : ShopPackType.Basic;
            return TryBuyPack(mappedPack, out drawnCards, out message);
        }

        public bool TryBuyMythicBox(out List<TeksCardDefinition> drawnCards, out string message)
        {
            EnsureInitialized();
            drawnCards = null;

            var inv = PlayerInventory.Instance;
            if (inv == null || !inv.TrySpendCoins(MythicBoxCost))
            {
                message = $"Not enough Barya Coins! Need {MythicBoxCost} Coins.";
                return false;
            }

            drawnCards = TeksDatabase.RollPackCards(ShopPackType.SpecialGem) ?? new List<TeksCardDefinition>();
            for (int i = 0; i < drawnCards.Count; i++)
            {
                if (drawnCards[i] != null)
                {
                    inv.AddCard(drawnCards[i].Id, 1);
                }
            }

            inv.ReportPackOpened(ShopPackType.SpecialGem, drawnCards.Count);
            inv.SaveToDisk();

            LastVendorMessage = $"Aling Nena: \"WHOA! Barangay Mythic Box! Jackpot yan, {inv.PlayerNickname}!\"";
            message = $"Opened BARANGAY MYTHIC BOX! Obtained {drawnCards.Count} Teks cards (Guaranteed Epic+)!";

            OnPurchaseSucceeded?.Invoke();
            OnPackOpened?.Invoke(ShopPackType.SpecialGem, drawnCards);
            OnShopUpdated?.Invoke();
            return true;
        }

        public bool TryClaimDailyBarya(out string message)
        {
            return TryClaimDailyFreeDraw(out _, out _, out message);
        }

        public bool TryManualRefresh(out string message)
        {
            EnsureInitialized();
            var inv = PlayerInventory.Instance;
            if (inv == null || !inv.TrySpendCoins(ManualRefreshCost))
            {
                message = $"Need {ManualRefreshCost} Coins to force-refresh now.";
                return false;
            }

            inv.SaveToDisk();
            RefreshShopSlots(resetTimer: true);
            OnPurchaseSucceeded?.Invoke();
            message = "Sari-Sari Store stock rotated early!";
            return true;
        }

        public string GetFormattedTimer()
        {
            float clamped = Mathf.Max(0f, secondsUntilRefresh);
            int totalSeconds = Mathf.FloorToInt(clamped);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static string BuildVendorPackMessage(ShopPackType packType, string displayName, string playerNickname)
        {
            return packType switch
            {
                ShopPackType.SpecialGem => $"Aling Nena: \"WHOA! {displayName}! Jackpot pull 'yan, {playerNickname}!\"",
                ShopPackType.Rare => $"Aling Nena: \"Ganda ng mga Rare pull mo sa {displayName}, {playerNickname}!\"",
                ShopPackType.Premium => $"Aling Nena: \"Sulit na sulit ang {displayName}, {playerNickname}!\"",
                _ => $"Aling Nena: \"Uy swerte! Magaganda nakuha mo sa {displayName}, {playerNickname}!\""
            };
        }
    }
}
