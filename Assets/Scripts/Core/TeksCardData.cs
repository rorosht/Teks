using System;
using UnityEngine;

namespace TeksMaster.Core
{
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum CardTheme
    {
        ClassicHero,
        PinoyMythical,
        SciFiMech
    }

    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard,
        Boss
    }

    public enum PlayerGender
    {
        Male,
        Female
    }

    [Serializable]
    public class TeksCardDefinition
    {
        public string Id;
        public string Name;
        public string Tagline;
        public CardTheme Theme;
        public CardRarity Rarity;
        public int BaseHP;
        public int BaseAtk;
        public int BaseAgility;
        public int BaseDef;
        public int BasePower;
        public int BaseSpeed;
        public int BuyPrice;
        public int SellPrice;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color AccentColor;

        public TeksCardDefinition(
            string id,
            string name,
            string tagline,
            CardTheme theme,
            CardRarity rarity,
            int baseHp,
            int baseAtk,
            int baseAgility,
            int baseDef,
            int basePower,
            int baseSpeed,
            int buyPrice,
            int sellPrice,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor)
        {
            Id = id;
            Name = name;
            Tagline = tagline;
            Theme = theme;
            Rarity = rarity;
            BaseHP = baseHp;
            BaseAtk = baseAtk;
            BaseAgility = baseAgility;
            BaseDef = baseDef;
            BasePower = basePower;
            BaseSpeed = baseSpeed;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            AccentColor = accentColor;
        }
    }

    [Serializable]
    public class OwnedTeksCard
    {
        public string CardId;
        public int StarRank;
        public int DuplicateCount;

        public OwnedTeksCard(string cardId, int starRank = 1, int duplicateCount = 0)
        {
            CardId = cardId;
            StarRank = Mathf.Clamp(starRank, 1, 5);
            DuplicateCount = Mathf.Max(0, duplicateCount);
        }

        private float GetStatScale()
        {
            int clamped = Mathf.Clamp(StarRank, 1, 5);
            return 1.0f + (clamped - 1) * 0.06f;
        }

        public int GetHP(TeksCardDefinition def)
        {
            if (def == null) return 0;
            return Mathf.RoundToInt(def.BaseHP * GetStatScale());
        }

        public int GetAtk(TeksCardDefinition def)
        {
            if (def == null) return 0;
            return Mathf.RoundToInt(def.BaseAtk * GetStatScale());
        }

        public int GetAgility(TeksCardDefinition def)
        {
            if (def == null) return 0;
            return Mathf.RoundToInt(def.BaseAgility * GetStatScale());
        }

        public int GetDef(TeksCardDefinition def)
        {
            if (def == null) return 0;
            return Mathf.RoundToInt(def.BaseDef * GetStatScale());
        }

        public int GetPower(TeksCardDefinition def)
        {
            if (def == null) return 0;
            return Mathf.RoundToInt(def.BasePower * GetStatScale());
        }

        public int GetSpeed(TeksCardDefinition def)
        {
            if (def == null) return 0;
            return Mathf.RoundToInt(def.BaseSpeed * GetStatScale());
        }

        public float GetFlickSweetSpotBonus()
        {
            int clamped = Mathf.Clamp(StarRank, 1, 5);
            return (clamped - 1) * 0.02f;
        }

        public float GetLuckRatingBonus()
        {
            int clamped = Mathf.Clamp(StarRank, 1, 5);
            return (clamped - 1) * 0.045f;
        }

        public float GetFoilGlowIntensity()
        {
            int clamped = Mathf.Clamp(StarRank, 1, 5);
            return (clamped - 1) * 0.25f;
        }

        public int GetDuplicatesNeededForUpgrade()
        {
            if (StarRank >= 5) return 0;
            return 1;
        }

        public int GetUpgradeCoinCost(TeksCardDefinition def)
        {
            if (StarRank >= 5 || def == null) return 0;
            int baseCost = def.Rarity switch
            {
                CardRarity.Common => 40,
                CardRarity.Uncommon => 75,
                CardRarity.Rare => 120,
                CardRarity.Epic => 200,
                CardRarity.Legendary => 320,
                _ => 60
            };
            return baseCost * StarRank;
        }
    }
}
