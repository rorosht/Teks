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

    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public const float RotationIntervalSeconds = 300f;
        public const int KalyeBoosterCost = 150;
        public const int AlamatSuperPackCost = 400;
        public const int MythicBoxCost = 850;
        public const int ManualRefreshCost = 50;
        public const int DailyFreeBaryaAmount = 200;

        [SerializeField] private float secondsUntilRefresh = 299.9f;
        private readonly ShopSlot[] _slots = new ShopSlot[4];
        private bool _initialized;

        public bool HasClaimedDailyBarya { get; private set; }
        public string LastVendorMessage { get; private set; } = "Aling Nena: \"Bili na! May -30% Sulit Sale at bagong Teks packs ngayon!\"";
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
            slot.IsSoldOut = true;
            LastVendorMessage = $"Aling Nena: \"Salamat {inv.PlayerNickname}! Ang ganda ng {slot.Card.Name} na 'yan!\"";
            message = $"Bought {slot.Card.Name} ({TeksDatabase.GetRarityName(slot.Card.Rarity)}) for {slot.Price} Coins!";
            OnShopUpdated?.Invoke();
            return true;
        }

        public bool TryBuyBoosterPack(bool isSuperPack, out List<TeksCardDefinition> drawnCards, out string message)
        {
            EnsureInitialized();
            drawnCards = null;
            int cost = isSuperPack ? AlamatSuperPackCost : KalyeBoosterCost;

            var inv = PlayerInventory.Instance;
            if (inv == null || !inv.TrySpendCoins(cost))
            {
                message = $"Not enough Barya Coins! Need {cost} Coins.";
                return false;
            }

            drawnCards = TeksDatabase.RollBoosterPack(isSuperPack, 3);
            for (int i = 0; i < drawnCards.Count; i++)
            {
                if (drawnCards[i] != null)
                {
                    inv.AddCard(drawnCards[i].Id, 1);
                }
            }

            inv.RecordBoosterOpened();
            string packName = isSuperPack ? "Alamat Super Pack" : "Kalye Booster Pack";
            LastVendorMessage = $"Aling Nena: \"Uy swerte! Magaganda nakuha mo sa {packName}, {inv.PlayerNickname}!\"";
            message = $"Opened {packName}! Obtained 3 Teks cards!";
            return true;
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

            drawnCards = TeksDatabase.RollBoosterPack(isSuperPack: true, count: 5);
            // Guarantee slot 5 is Epic or Legendary
            drawnCards[4] = UnityEngine.Random.value < 0.45f
                ? TeksDatabase.RollCardByRarity(CardRarity.Legendary)
                : TeksDatabase.RollCardByRarity(CardRarity.Epic);

            for (int i = 0; i < drawnCards.Count; i++)
            {
                if (drawnCards[i] != null)
                {
                    inv.AddCard(drawnCards[i].Id, 1);
                }
            }

            inv.RecordBoosterOpened();
            LastVendorMessage = $"Aling Nena: \"WHOA! Barangay Mythic Box! Jackpot yan, {inv.PlayerNickname}!\"";
            message = "Opened BARANGAY MYTHIC BOX! Obtained 5 Teks cards (Guaranteed Epic+)!";
            return true;
        }

        public bool TryClaimDailyBarya(out string message)
        {
            if (HasClaimedDailyBarya)
            {
                message = "Already claimed Aling Nena's Free Barya today!";
                return false;
            }

            HasClaimedDailyBarya = true;
            var inv = PlayerInventory.Instance;
            inv?.AddCoins(DailyFreeBaryaAmount);
            LastVendorMessage = $"Aling Nena: \"Heto sukli at pamasko +{DailyFreeBaryaAmount} Barya Coins, {inv?.PlayerNickname}!\"";
            message = $"Claimed +{DailyFreeBaryaAmount} Free Barya Coins from Aling Nena!";
            OnShopUpdated?.Invoke();
            return true;
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

            RefreshShopSlots(resetTimer: true);
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
    }
}
