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

    public enum CardCategory
    {
        Heroes,
        Villains,
        Monsters,
        Animals,
        Warriors,
        Fantasy,
        FilipinoMythology
    }

    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard,
        Boss,
        Extreme
    }

    public enum PlayerCharacterType
    {
        StreetKid = 0,
        CourtBaller = 1,
        HeadphoneDJ = 2,
        StreetPunk = 3,
        PopStar = 4,
        SportyAce = 5,
        BarangayChef = 6,
        ShadowBoss = 7
    }

    public readonly struct PlayerCharacterPreset
    {
        public readonly PlayerCharacterType Type;
        public readonly string DisplayName;
        public readonly string ShortLabel;
        public readonly string Subtitle;
        public readonly int PrefabNumber;
        public readonly Color AccentColor;

        public PlayerCharacterPreset(
            PlayerCharacterType type,
            string displayName,
            string shortLabel,
            string subtitle,
            int prefabNumber,
            Color accentColor)
        {
            Type = type;
            DisplayName = displayName;
            ShortLabel = shortLabel;
            Subtitle = subtitle;
            PrefabNumber = prefabNumber;
            AccentColor = accentColor;
        }
    }

    public static class TeksCharacterPresets
    {
        public static readonly PlayerCharacterPreset[] All =
        {
            new PlayerCharacterPreset(
                PlayerCharacterType.StreetKid,
                "Street Kid",
                "STREET KID",
                "Red Cap • Kalye Classic",
                26,
                new Color(0.88f, 0.24f, 0.24f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.CourtBaller,
                "Court Baller",
                "BALLER",
                "Striped Jersey • Court Hooper",
                4,
                new Color(0.18f, 0.52f, 0.92f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.HeadphoneDJ,
                "Headphone DJ",
                "BEAT DJ",
                "Headphones • Rhythm Striker",
                72,
                new Color(0.22f, 0.72f, 0.88f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.StreetPunk,
                "Street Punk",
                "PUNK",
                "Mohawk & Shades • Kanto Rebel",
                24,
                new Color(0.22f, 0.80f, 0.36f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.PopStar,
                "Pop Star",
                "POP STAR",
                "Pink Hair • Fiesta Idol",
                61,
                new Color(0.90f, 0.32f, 0.64f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.SportyAce,
                "Sporty Ace",
                "SPORTY",
                "Red Visor • Quick Runner",
                92,
                new Color(0.94f, 0.48f, 0.16f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.BarangayChef,
                "Barangay Chef",
                "CHEF",
                "Chef Hat • Kalye Culinary Pro",
                19,
                new Color(0.16f, 0.68f, 0.78f)),
            new PlayerCharacterPreset(
                PlayerCharacterType.ShadowBoss,
                "Shadow Boss",
                "BOSS",
                "Tall Hat & Shades • Street VIP",
                18,
                new Color(0.58f, 0.30f, 0.88f))
        };

        public static int Count => All.Length;

        public static PlayerCharacterPreset GetPreset(PlayerCharacterType type)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= All.Length) idx = 0;
            return All[idx];
        }

        public static int GetPrefabNumber(PlayerCharacterType type)
        {
            return GetPreset(type).PrefabNumber;
        }

        public static PlayerCharacterType FromIndex(int index)
        {
            int count = All.Length;
            int wrapped = ((index % count) + count) % count;
            return All[wrapped].Type;
        }
    }

    public enum BarangayAreaId
    {
        PlayersNeighborhood = 0,
        SariSariAndKalye = 1,
        BasketballCourt = 2,
        OldNeighborhood = 3,
        FiestaArena = 4,
        TournamentArena = 5
    }

    public enum MissionCategory
    {
        MainStory,
        Daily,
        Collection,
        Exploration,
        Achievement
    }

    public enum ShopPackType
    {
        Basic,
        Premium,
        Rare,
        SpecialGem,
        DailyFree
    }

    public enum GameModeType
    {
        Story,
        QuickBattle,
        Tournament,
        Challenge
    }

    public enum ChallengeRuleType
    {
        WinStreak3,
        CommonOnly,
        ExtremeBossGauntlet
    }

    [Serializable]
    public class AudioSettingsData
    {
        public float MasterVolume = 0.85f;
        public float BgmVolume = 0.45f;
        public float SfxVolume = 0.90f;
        public bool IsMuted = false;
        public bool IsFullscreen = true;

        public AudioSettingsData()
        {
        }

        public AudioSettingsData(float masterVolume, float bgmVolume, float sfxVolume, bool isMuted, bool isFullscreen)
        {
            MasterVolume = Mathf.Clamp01(masterVolume);
            BgmVolume = Mathf.Clamp01(bgmVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
            IsMuted = isMuted;
            IsFullscreen = isFullscreen;
        }

        public void Clamp()
        {
            MasterVolume = Mathf.Clamp01(MasterVolume);
            BgmVolume = Mathf.Clamp01(BgmVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
        }

        public AudioSettingsData Clone()
        {
            return new AudioSettingsData(MasterVolume, BgmVolume, SfxVolume, IsMuted, IsFullscreen);
        }
    }

    [Serializable]
    public class BarangayQuest
    {
        public string Id;
        public string Title;
        public string Description;
        public MissionCategory Category;
        public int CurrentProgress;
        public int TargetCount;
        public int CoinReward;
        public int GemReward;
        public bool HasPackReward;
        public ShopPackType PackRewardType;
        public string SpecialCardRewardId;
        public bool IsDaily;
        public bool IsClaimed;

        public bool IsCompleted => TargetCount > 0 && CurrentProgress >= TargetCount;
    }

    [Serializable]
    public class TeksCardDefinition
    {
        public string Id;
        public string Name;
        public string Tagline;
        public CardTheme Theme;
        public CardCategory Category;
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
            : this(
                id, name, tagline, theme, InferDefaultCategory(theme), rarity,
                baseHp, baseAtk, baseAgility, baseDef, basePower, baseSpeed,
                buyPrice, sellPrice, primaryColor, secondaryColor, accentColor)
        {
        }

        public TeksCardDefinition(
            string id,
            string name,
            string tagline,
            CardTheme theme,
            CardCategory category,
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
            Category = category;
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

        private static CardCategory InferDefaultCategory(CardTheme theme)
        {
            return theme switch
            {
                CardTheme.ClassicHero => CardCategory.Heroes,
                CardTheme.PinoyMythical => CardCategory.FilipinoMythology,
                CardTheme.SciFiMech => CardCategory.Warriors,
                _ => CardCategory.Heroes
            };
        }
    }

    [Serializable]
    public class OwnedTeksCard
    {
        public string CardId;
        public int StarRank = 1;
        public int DuplicateCount;
        public int FusionStage;

        public int Level => Mathf.Clamp(StarRank, 1, 5);
        public int Quantity => 1 + Mathf.Max(0, DuplicateCount);

        public OwnedTeksCard()
        {
            CardId = string.Empty;
            StarRank = 1;
            DuplicateCount = 0;
            FusionStage = 0;
        }

        public OwnedTeksCard(string cardId, int starRank = 1, int duplicateCount = 0, int fusionStage = 0)
        {
            CardId = cardId;
            StarRank = Mathf.Clamp(starRank, 1, 5);
            DuplicateCount = Mathf.Max(0, duplicateCount);
            FusionStage = Mathf.Max(0, fusionStage);
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
