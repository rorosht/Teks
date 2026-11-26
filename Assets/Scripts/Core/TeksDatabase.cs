using System.Collections.Generic;
using UnityEngine;

namespace TeksMaster.Core
{
    public class CardFusionRecipe
    {
        public string RecipeId;
        public string IngredientAId;
        public string IngredientBId;
        public string ResultCardId;
        public int CoinCost;
        public string Description;

        public CardFusionRecipe(string id, string a, string b, string result, int coinCost, string desc)
        {
            RecipeId = id;
            IngredientAId = a;
            IngredientBId = b;
            ResultCardId = result;
            CoinCost = coinCost;
            Description = desc;
        }
    }

    public static class TeksDatabase
    {
        private static List<TeksCardDefinition> _cards;
        private static Dictionary<string, TeksCardDefinition> _cardsById;
        private static List<CardFusionRecipe> _fusionRecipes;

        public static IReadOnlyList<TeksCardDefinition> AllCards
        {
            get
            {
                EnsureInitialized();
                return _cards;
            }
        }

        public static IReadOnlyList<CardFusionRecipe> FusionRecipes
        {
            get
            {
                EnsureInitialized();
                return _fusionRecipes;
            }
        }

        private static void EnsureInitialized()
        {
            if (_cards != null) return;

            _cards = new List<TeksCardDefinition>
            {
                // Classic Hero Theme
                new TeksCardDefinition(
                    "hero", "HERO", "Golden Sun Fist Champion",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Legendary,
                    81, 75, 30, 24, 39, 17, 650, 220,
                    new Color(0.95f, 0.45f, 0.12f), new Color(0.16f, 0.34f, 0.78f), new Color(1.0f, 0.84f, 0.18f)
                ),
                new TeksCardDefinition(
                    "kapitan_barangay", "KAPITAN BARANGAY", "Defender of the Kalye",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Epic,
                    74, 68, 28, 28, 35, 16, 400, 140,
                    new Color(0.18f, 0.38f, 0.85f), new Color(0.84f, 0.18f, 0.22f), new Color(0.98f, 0.82f, 0.22f)
                ),
                new TeksCardDefinition(
                    "kid_kidlat", "KID KIDLAT", "Lightning Tsinelas Striker",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Rare,
                    64, 58, 34, 20, 31, 22, 220, 75,
                    new Color(0.14f, 0.56f, 0.94f), new Color(0.09f, 0.16f, 0.38f), new Color(1.0f, 0.92f, 0.25f)
                ),
                new TeksCardDefinition(
                    "daragang_mandirigma", "DARAGANG MANDIRIGMA", "Fearless Street Warrior",
                    CardTheme.ClassicHero, CardCategory.Warriors, CardRarity.Uncommon,
                    56, 50, 27, 22, 26, 17, 110, 38,
                    new Color(0.86f, 0.26f, 0.45f), new Color(0.20f, 0.62f, 0.40f), new Color(1.0f, 0.76f, 0.32f)
                ),
                new TeksCardDefinition(
                    "bantay_kalye", "BANTAY KALYE", "Rookie Court Protector",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Common,
                    48, 42, 22, 20, 21, 14, 55, 18,
                    new Color(0.36f, 0.44f, 0.56f), new Color(0.82f, 0.42f, 0.18f), new Color(0.90f, 0.90f, 0.92f)
                ),

                // Pinoy Mythical Creatures Theme
                new TeksCardDefinition(
                    "bakunawa", "BAKUNAWA", "Moon-Devouring Sea Dragon",
                    CardTheme.PinoyMythical, CardCategory.FilipinoMythology, CardRarity.Legendary,
                    84, 78, 33, 27, 42, 19, 680, 240,
                    new Color(0.08f, 0.25f, 0.55f), new Color(0.10f, 0.78f, 0.92f), new Color(0.85f, 0.96f, 1.0f)
                ),
                new TeksCardDefinition(
                    "minokawa", "MINOKAWA", "Sky-Swallowing Eclipse Bird",
                    CardTheme.PinoyMythical, CardCategory.Monsters, CardRarity.Epic,
                    72, 71, 35, 22, 37, 21, 410, 145,
                    new Color(0.35f, 0.12f, 0.58f), new Color(0.88f, 0.32f, 0.15f), new Color(1.0f, 0.78f, 0.20f)
                ),
                new TeksCardDefinition(
                    "sarimanok", "SARIMANOK", "Prismatic Bird of Fortune",
                    CardTheme.PinoyMythical, CardCategory.Animals, CardRarity.Epic,
                    70, 65, 36, 25, 34, 22, 390, 135,
                    new Color(0.92f, 0.22f, 0.48f), new Color(0.15f, 0.74f, 0.64f), new Color(1.0f, 0.88f, 0.24f)
                ),
                new TeksCardDefinition(
                    "tikbalang_striker", "TIKBALANG STRIKER", "Phantom Hoof Trickster",
                    CardTheme.PinoyMythical, CardCategory.FilipinoMythology, CardRarity.Rare,
                    63, 60, 32, 23, 30, 19, 210, 72,
                    new Color(0.24f, 0.48f, 0.26f), new Color(0.45f, 0.25f, 0.14f), new Color(0.62f, 0.95f, 0.45f)
                ),
                new TeksCardDefinition(
                    "sigbin_runner", "SIGBIN RUNNER", "Shadow-Hopping Night Scout",
                    CardTheme.PinoyMythical, CardCategory.Animals, CardRarity.Uncommon,
                    54, 52, 30, 19, 25, 20, 105, 36,
                    new Color(0.25f, 0.18f, 0.38f), new Color(0.18f, 0.65f, 0.35f), new Color(0.75f, 0.95f, 0.35f)
                ),
                new TeksCardDefinition(
                    "duwende_trickster", "DUWENDE TRICKSTER", "Mound-Dwelling Luck Sprite",
                    CardTheme.PinoyMythical, CardCategory.Fantasy, CardRarity.Common,
                    46, 40, 26, 18, 22, 16, 50, 16,
                    new Color(0.42f, 0.32f, 0.22f), new Color(0.32f, 0.56f, 0.28f), new Color(0.95f, 0.78f, 0.35f)
                ),

                // Sci-Fi / Robot Mechs Theme
                new TeksCardDefinition(
                    "jeepney_mech", "JEEPNEY MECH MK-V", "Chrome Kingsroad Transformer",
                    CardTheme.SciFiMech, CardCategory.Warriors, CardRarity.Legendary,
                    86, 76, 28, 32, 41, 18, 700, 245,
                    new Color(0.86f, 0.16f, 0.24f), new Color(0.14f, 0.64f, 0.95f), new Color(1.0f, 0.88f, 0.25f)
                ),
                new TeksCardDefinition(
                    "mecha_bathala", "MECHA BATHALA", "Orbital Plasma Guardian",
                    CardTheme.SciFiMech, CardCategory.Fantasy, CardRarity.Epic,
                    75, 69, 29, 30, 36, 17, 420, 150,
                    new Color(0.44f, 0.18f, 0.76f), new Color(0.12f, 0.82f, 0.88f), new Color(0.92f, 0.68f, 1.0f)
                ),
                new TeksCardDefinition(
                    "baryon_sentinel", "BARYON SENTINEL", "Heavy Armor Core Unit",
                    CardTheme.SciFiMech, CardCategory.Warriors, CardRarity.Rare,
                    66, 57, 24, 29, 32, 15, 230, 80,
                    new Color(0.16f, 0.42f, 0.78f), new Color(0.28f, 0.34f, 0.44f), new Color(0.38f, 0.92f, 1.0f)
                ),
                new TeksCardDefinition(
                    "cyber_taho_bot", "CYBER-TAHO BOT", "Steam-Jet Street Automaton",
                    CardTheme.SciFiMech, CardCategory.Fantasy, CardRarity.Uncommon,
                    58, 49, 23, 25, 27, 15, 115, 40,
                    new Color(0.18f, 0.68f, 0.46f), new Color(0.76f, 0.48f, 0.20f), new Color(0.55f, 1.0f, 0.78f)
                ),
                new TeksCardDefinition(
                    "scrap_trisikad", "SCRAP TRISIKAD", "Welded Sidecar Drone",
                    CardTheme.SciFiMech, CardCategory.Villains, CardRarity.Common,
                    50, 41, 21, 22, 20, 13, 55, 18,
                    new Color(0.45f, 0.48f, 0.52f), new Color(0.68f, 0.36f, 0.22f), new Color(0.82f, 0.88f, 0.94f)
                ),

                // --- Expanded Collection (10 New Standard Cards) ---
                new TeksCardDefinition(
                    "tumbang_striker", "TUMBANG STRIKER", "Tin-Can Sniper Kid",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Uncommon,
                    55, 51, 29, 21, 27, 19, 115, 38,
                    new Color(0.18f, 0.66f, 0.44f), new Color(0.88f, 0.34f, 0.16f), new Color(1.0f, 0.88f, 0.28f)
                ),
                new TeksCardDefinition(
                    "agila_kid", "AGILA KID", "Sky-Diving Court Ace",
                    CardTheme.ClassicHero, CardCategory.Animals, CardRarity.Rare,
                    66, 61, 35, 22, 32, 23, 230, 78,
                    new Color(0.18f, 0.46f, 0.92f), new Color(0.82f, 0.58f, 0.16f), new Color(1.0f, 0.94f, 0.45f)
                ),
                new TeksCardDefinition(
                    "panday_blade", "PANDAY BLADE", "Meteorite Kampilan Forger",
                    CardTheme.ClassicHero, CardCategory.Warriors, CardRarity.Epic,
                    76, 74, 30, 27, 38, 18, 430, 150,
                    new Color(0.86f, 0.24f, 0.12f), new Color(0.24f, 0.14f, 0.12f), new Color(1.0f, 0.78f, 0.18f)
                ),
                new TeksCardDefinition(
                    "nuno_guard", "NUNO GUARDIAN", "Ancient Mound Keeper",
                    CardTheme.PinoyMythical, CardCategory.FilipinoMythology, CardRarity.Common,
                    49, 39, 23, 24, 21, 14, 52, 17,
                    new Color(0.38f, 0.34f, 0.26f), new Color(0.24f, 0.46f, 0.24f), new Color(0.86f, 0.74f, 0.36f)
                ),
                new TeksCardDefinition(
                    "kapre_titan", "KAPRE TITAN", "Balete Tree Colossus",
                    CardTheme.PinoyMythical, CardCategory.Monsters, CardRarity.Rare,
                    72, 62, 22, 31, 33, 14, 235, 80,
                    new Color(0.22f, 0.36f, 0.18f), new Color(0.46f, 0.24f, 0.12f), new Color(1.0f, 0.56f, 0.18f)
                ),
                new TeksCardDefinition(
                    "manananggal_wing", "NIGHT WING", "Midnight Roof Stalker",
                    CardTheme.PinoyMythical, CardCategory.Villains, CardRarity.Epic,
                    69, 73, 37, 21, 36, 24, 415, 145,
                    new Color(0.34f, 0.12f, 0.48f), new Color(0.14f, 0.08f, 0.22f), new Color(0.95f, 0.30f, 0.56f)
                ),
                new TeksCardDefinition(
                    "diwata_maria", "DIWATA MARIA", "Goddess of Mount Makiling",
                    CardTheme.PinoyMythical, CardCategory.FilipinoMythology, CardRarity.Legendary,
                    85, 76, 36, 30, 43, 22, 690, 245,
                    new Color(0.16f, 0.68f, 0.46f), new Color(0.96f, 0.78f, 0.24f), new Color(0.88f, 1.0f, 0.82f)
                ),
                new TeksCardDefinition(
                    "sorbetes_bot", "SORBETES CRYO-BOT", "Dirty Ice Cream Freeze Unit",
                    CardTheme.SciFiMech, CardCategory.Fantasy, CardRarity.Common,
                    51, 40, 22, 23, 21, 14, 56, 18,
                    new Color(0.24f, 0.72f, 0.94f), new Color(0.96f, 0.46f, 0.66f), new Color(1.0f, 0.96f, 0.72f)
                ),
                new TeksCardDefinition(
                    "halo_halo_core", "HALO-HALO REACTOR", "Ube-Ice Overdrive Mech",
                    CardTheme.SciFiMech, CardCategory.Fantasy, CardRarity.Uncommon,
                    59, 52, 26, 25, 28, 17, 120, 42,
                    new Color(0.58f, 0.22f, 0.86f), new Color(0.96f, 0.76f, 0.20f), new Color(0.45f, 0.96f, 0.88f)
                ),
                new TeksCardDefinition(
                    "tricycle_turret", "TRICYCLE TURRET", "Turbo Sidecar Artillery",
                    CardTheme.SciFiMech, CardCategory.Villains, CardRarity.Rare,
                    68, 63, 27, 28, 33, 18, 240, 82,
                    new Color(0.84f, 0.22f, 0.24f), new Color(0.24f, 0.30f, 0.40f), new Color(1.0f, 0.84f, 0.24f)
                ),

                // --- Special Fusion-Exclusive Cards (5 Cards) ---
                new TeksCardDefinition(
                    "sun_dragon_hero", "SUN DRAGON HERO", "Fused Solar Bakunawa Champion",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Legendary,
                    92, 86, 38, 33, 48, 23, 900, 320,
                    new Color(1.0f, 0.72f, 0.12f), new Color(0.10f, 0.78f, 0.96f), new Color(1.0f, 0.96f, 0.55f)
                ),
                new TeksCardDefinition(
                    "mecha_kapitan", "MECHA KAPITAN MK-X", "Fused Orbital Barangay Guardian",
                    CardTheme.SciFiMech, CardCategory.Warriors, CardRarity.Legendary,
                    90, 84, 34, 36, 46, 21, 880, 310,
                    new Color(0.32f, 0.24f, 0.86f), new Color(0.14f, 0.80f, 0.94f), new Color(1.0f, 0.84f, 0.22f)
                ),
                new TeksCardDefinition(
                    "eclipse_sarimanok", "ECLIPSE SARIMANOK", "Fused Celestial Fortune Phoenix",
                    CardTheme.PinoyMythical, CardCategory.Animals, CardRarity.Legendary,
                    88, 83, 40, 31, 45, 25, 860, 300,
                    new Color(0.76f, 0.16f, 0.62f), new Color(0.98f, 0.62f, 0.14f), new Color(0.32f, 0.96f, 0.86f)
                ),
                new TeksCardDefinition(
                    "cyber_tikbalang", "CYBER TIKBALANG", "Fused Neon Forest Striker",
                    CardTheme.PinoyMythical, CardCategory.Monsters, CardRarity.Epic,
                    78, 74, 36, 30, 39, 22, 480, 170,
                    new Color(0.14f, 0.74f, 0.44f), new Color(0.16f, 0.44f, 0.88f), new Color(0.55f, 1.0f, 0.68f)
                ),
                new TeksCardDefinition(
                    "lightning_jeepney", "KIDLAT JEEPNEY", "Fused Thunder Kingsroad Titan",
                    CardTheme.SciFiMech, CardCategory.Warriors, CardRarity.Legendary,
                    91, 85, 36, 34, 47, 24, 890, 315,
                    new Color(0.16f, 0.58f, 0.98f), new Color(0.92f, 0.20f, 0.24f), new Color(1.0f, 0.94f, 0.28f)
                ),

                // --- Wave 2 Expansion (10 More Cards -> 41 Total) ---
                new TeksCardDefinition(
                    "luksong_tinik", "LUKSONG TINIK", "High-Flying Backyard Jumper",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Common,
                    47, 41, 28, 19, 21, 18, 52, 17,
                    new Color(0.24f, 0.62f, 0.84f), new Color(0.96f, 0.72f, 0.18f), new Color(0.95f, 0.95f, 0.98f)
                ),
                new TeksCardDefinition(
                    "sipa_master", "SIPA MASTER", "Washer-Kick Street Trickster",
                    CardTheme.ClassicHero, CardCategory.Warriors, CardRarity.Uncommon,
                    57, 52, 33, 21, 28, 21, 118, 40,
                    new Color(0.92f, 0.36f, 0.18f), new Color(0.16f, 0.56f, 0.78f), new Color(1.0f, 0.88f, 0.24f)
                ),
                new TeksCardDefinition(
                    "patintero_flash", "PATINTERO FLASH", "Uncatchable Chalk-Line Dasher",
                    CardTheme.ClassicHero, CardCategory.Heroes, CardRarity.Rare,
                    65, 60, 39, 22, 33, 25, 240, 82,
                    new Color(0.96f, 0.52f, 0.12f), new Color(0.14f, 0.24f, 0.58f), new Color(1.0f, 0.95f, 0.32f)
                ),
                new TeksCardDefinition(
                    "berberoka", "BERBEROKA", "River-Swallowing Water Ogre",
                    CardTheme.PinoyMythical, CardCategory.Monsters, CardRarity.Rare,
                    70, 61, 24, 29, 32, 16, 230, 78,
                    new Color(0.10f, 0.44f, 0.58f), new Color(0.18f, 0.68f, 0.62f), new Color(0.68f, 0.98f, 0.96f)
                ),
                new TeksCardDefinition(
                    "mambabarang", "SWARM MYSTIC", "Twilight Spirit Summoner",
                    CardTheme.PinoyMythical, CardCategory.Villains, CardRarity.Epic,
                    71, 72, 33, 25, 37, 20, 425, 148,
                    new Color(0.32f, 0.12f, 0.46f), new Color(0.16f, 0.56f, 0.28f), new Color(0.84f, 1.0f, 0.32f)
                ),
                new TeksCardDefinition(
                    "ibong_adarna", "IBONG ADARNA", "Enchanted Songbird of Fortune",
                    CardTheme.PinoyMythical, CardCategory.FilipinoMythology, CardRarity.Legendary,
                    87, 79, 39, 31, 44, 24, 710, 250,
                    new Color(0.96f, 0.68f, 0.14f), new Color(0.92f, 0.22f, 0.52f), new Color(0.35f, 0.96f, 0.92f)
                ),
                new TeksCardDefinition(
                    "balut_drone", "BALUT DRONE", "Night-Market Hover Scout",
                    CardTheme.SciFiMech, CardCategory.Villains, CardRarity.Common,
                    48, 42, 24, 22, 21, 16, 54, 18,
                    new Color(0.42f, 0.46f, 0.54f), new Color(0.94f, 0.68f, 0.18f), new Color(0.45f, 0.94f, 1.0f)
                ),
                new TeksCardDefinition(
                    "kalesa_strider", "KALESA STRIDER", "Cybernetic Intramuros Walker",
                    CardTheme.SciFiMech, CardCategory.Animals, CardRarity.Uncommon,
                    60, 53, 27, 26, 29, 18, 125, 44,
                    new Color(0.64f, 0.32f, 0.16f), new Color(0.22f, 0.64f, 0.84f), new Color(1.0f, 0.84f, 0.28f)
                ),
                new TeksCardDefinition(
                    "rizal_vanguard", "MANILA VANGUARD", "Neo-Intramuros Plasma Knight",
                    CardTheme.SciFiMech, CardCategory.Warriors, CardRarity.Epic,
                    77, 73, 31, 32, 39, 19, 440, 155,
                    new Color(0.16f, 0.32f, 0.78f), new Color(0.88f, 0.20f, 0.22f), new Color(1.0f, 0.88f, 0.25f)
                ),
                new TeksCardDefinition(
                    "adarna_phoenix", "ADARNA SOLARIS", "Fused Celestial Diwata Songbird",
                    CardTheme.PinoyMythical, CardCategory.Animals, CardRarity.Legendary,
                    94, 88, 42, 35, 49, 26, 920, 330,
                    new Color(1.0f, 0.78f, 0.14f), new Color(0.18f, 0.82f, 0.56f), new Color(1.0f, 0.96f, 0.72f)
                )
            };

            _fusionRecipes = new List<CardFusionRecipe>
            {
                new CardFusionRecipe("fuse_sun_dragon", "hero", "bakunawa", "sun_dragon_hero", 300, "Fuse HERO + BAKUNAWA -> SUN DRAGON HERO"),
                new CardFusionRecipe("fuse_mecha_kapitan", "kapitan_barangay", "mecha_bathala", "mecha_kapitan", 250, "Fuse KAPITAN BARANGAY + MECHA BATHALA -> MECHA KAPITAN MK-X"),
                new CardFusionRecipe("fuse_adarna_solaris", "diwata_maria", "ibong_adarna", "adarna_phoenix", 320, "Fuse DIWATA MARIA + IBONG ADARNA -> ADARNA SOLARIS"),
                new CardFusionRecipe("fuse_eclipse_bird", "minokawa", "sarimanok", "eclipse_sarimanok", 250, "Fuse MINOKAWA + SARIMANOK -> ECLIPSE SARIMANOK"),
                new CardFusionRecipe("fuse_cyber_tikbalang", "tikbalang_striker", "baryon_sentinel", "cyber_tikbalang", 150, "Fuse TIKBALANG STRIKER + BARYON SENTINEL -> CYBER TIKBALANG"),
                new CardFusionRecipe("fuse_kidlat_jeepney", "kid_kidlat", "jeepney_mech", "lightning_jeepney", 300, "Fuse KID KIDLAT + JEEPNEY MECH MK-V -> KIDLAT JEEPNEY"),
                new CardFusionRecipe("fuse_panday_vanguard", "panday_blade", "rizal_vanguard", "mecha_kapitan", 220, "Fuse PANDAY BLADE + MANILA VANGUARD -> MECHA KAPITAN MK-X")
            };

            _cardsById = new Dictionary<string, TeksCardDefinition>(_cards.Count);
            foreach (var card in _cards)
            {
                _cardsById[card.Id] = card;
            }
        }

        public static TeksCardDefinition GetCard(string id)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(id)) return null;
            _cardsById.TryGetValue(id, out var def);
            return def;
        }

        public static List<TeksCardDefinition> GetCardsByCategory(CardCategory category)
        {
            EnsureInitialized();
            var list = new List<TeksCardDefinition>();
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Category == category)
                {
                    list.Add(_cards[i]);
                }
            }
            return list;
        }

        public static Color GetRarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => new Color(0.61f, 0.64f, 0.69f),    // Gray
                CardRarity.Uncommon => new Color(0.13f, 0.77f, 0.37f),  // Green
                CardRarity.Rare => new Color(0.23f, 0.51f, 0.96f),      // Blue
                CardRarity.Epic => new Color(0.66f, 0.33f, 0.97f),      // Purple
                CardRarity.Legendary => new Color(0.96f, 0.62f, 0.04f), // Gold
                _ => Color.white
            };
        }

        public static string GetRarityName(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => "Common",
                CardRarity.Uncommon => "Uncommon",
                CardRarity.Rare => "Rare",
                CardRarity.Epic => "Epic",
                CardRarity.Legendary => "Legendary",
                _ => "Common"
            };
        }

        public static string GetThemeName(CardTheme theme)
        {
            return theme switch
            {
                CardTheme.ClassicHero => "Classic Hero",
                CardTheme.PinoyMythical => "Pinoy Mythical",
                CardTheme.SciFiMech => "Sci-Fi Mech",
                _ => "Teks"
            };
        }

        public static string GetCategoryName(CardCategory category)
        {
            return category switch
            {
                CardCategory.Heroes => "Heroes",
                CardCategory.Villains => "Villains",
                CardCategory.Monsters => "Monsters",
                CardCategory.Animals => "Animals",
                CardCategory.Warriors => "Warriors",
                CardCategory.Fantasy => "Fantasy",
                CardCategory.FilipinoMythology => "Filipino Mythology",
                _ => "Heroes"
            };
        }

        public static bool TryGetNextRarity(CardRarity rarity, out CardRarity nextRarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: nextRarity = CardRarity.Uncommon; return true;
                case CardRarity.Uncommon: nextRarity = CardRarity.Rare; return true;
                case CardRarity.Rare: nextRarity = CardRarity.Epic; return true;
                case CardRarity.Epic: nextRarity = CardRarity.Legendary; return true;
                default: nextRarity = CardRarity.Legendary; return false;
            }
        }

        public static TeksCardDefinition RollCardByRarity(CardRarity rarity)
        {
            EnsureInitialized();
            var pool = new List<TeksCardDefinition>();
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Rarity == rarity)
                {
                    pool.Add(_cards[i]);
                }
            }

            if (pool.Count == 0) return _cards[Random.Range(0, _cards.Count)];
            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>
        /// Exact TEKS SPEC rarity drop rates:
        /// Common 55%, Uncommon 25%, Rare 12%, Epic 6%, Legendary 2%.
        /// </summary>
        public static CardRarity RollRarityBySpecRates()
        {
            float roll = Random.value;
            if (roll < 0.55f) return CardRarity.Common;
            if (roll < 0.80f) return CardRarity.Uncommon;
            if (roll < 0.92f) return CardRarity.Rare;
            if (roll < 0.98f) return CardRarity.Epic;
            return CardRarity.Legendary;
        }

        public static TeksCardDefinition RollCardBySpecRates()
        {
            return RollCardByRarity(RollRarityBySpecRates());
        }

        public static List<TeksCardDefinition> RollPackCards(ShopPackType packType)
        {
            EnsureInitialized();
            var drawn = new List<TeksCardDefinition>();

            switch (packType)
            {
                case ShopPackType.Basic:
                    for (int i = 0; i < 3; i++)
                    {
                        drawn.Add(RollCardBySpecRates());
                    }
                    break;

                case ShopPackType.Premium:
                    for (int i = 0; i < 4; i++)
                    {
                        drawn.Add(RollCardBySpecRates());
                    }
                    {
                        float r = Random.value;
                        if (r < 0.45f) drawn.Add(RollCardByRarity(CardRarity.Uncommon));
                        else if (r < 0.78f) drawn.Add(RollCardByRarity(CardRarity.Rare));
                        else if (r < 0.93f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else drawn.Add(RollCardByRarity(CardRarity.Legendary));
                    }
                    break;

                case ShopPackType.Rare:
                    for (int i = 0; i < 3; i++)
                    {
                        float r = Random.value;
                        if (r < 0.25f) drawn.Add(RollCardByRarity(CardRarity.Uncommon));
                        else if (r < 0.72f) drawn.Add(RollCardByRarity(CardRarity.Rare));
                        else if (r < 0.92f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else drawn.Add(RollCardByRarity(CardRarity.Legendary));
                    }
                    {
                        float r = Random.value;
                        if (r < 0.60f) drawn.Add(RollCardByRarity(CardRarity.Rare));
                        else if (r < 0.88f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else drawn.Add(RollCardByRarity(CardRarity.Legendary));
                    }
                    break;

                case ShopPackType.SpecialGem:
                    for (int i = 0; i < 4; i++)
                    {
                        float r = Random.value;
                        if (r < 0.52f) drawn.Add(RollCardByRarity(CardRarity.Rare));
                        else if (r < 0.84f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else drawn.Add(RollCardByRarity(CardRarity.Legendary));
                    }
                    {
                        float r = Random.value;
                        if (r < 0.65f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else drawn.Add(RollCardByRarity(CardRarity.Legendary));
                    }
                    break;

                case ShopPackType.DailyFree:
                    for (int i = 0; i < 3; i++)
                    {
                        drawn.Add(RollCardBySpecRates());
                    }
                    break;

                default:
                    drawn.Add(RollCardBySpecRates());
                    break;
            }

            return drawn;
        }

        public static TeksCardDefinition RollStandardShopCard()
        {
            float roll = Random.value;
            if (roll < 0.50f) return RollCardByRarity(CardRarity.Common);
            if (roll < 0.82f) return RollCardByRarity(CardRarity.Uncommon);
            return RollCardByRarity(CardRarity.Rare);
        }

        public static TeksCardDefinition RollSuperSlotCard()
        {
            float roll = Random.value;
            if (roll < 0.55f) return RollCardByRarity(CardRarity.Rare);
            if (roll < 0.85f) return RollCardByRarity(CardRarity.Epic);
            return RollCardByRarity(CardRarity.Legendary);
        }

        public static List<TeksCardDefinition> RollBoosterPack(bool isSuperPack, int count = 3)
        {
            var drawn = new List<TeksCardDefinition>(count);
            for (int i = 0; i < count; i++)
            {
                if (i == count - 1)
                {
                    if (isSuperPack)
                    {
                        float r = Random.value;
                        if (r < 0.35f) drawn.Add(RollCardByRarity(CardRarity.Legendary));
                        else if (r < 0.75f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else drawn.Add(RollCardByRarity(CardRarity.Rare));
                    }
                    else
                    {
                        float r = Random.value;
                        if (r < 0.08f) drawn.Add(RollCardByRarity(CardRarity.Legendary));
                        else if (r < 0.28f) drawn.Add(RollCardByRarity(CardRarity.Epic));
                        else if (r < 0.68f) drawn.Add(RollCardByRarity(CardRarity.Rare));
                        else drawn.Add(RollCardByRarity(CardRarity.Uncommon));
                    }
                }
                else
                {
                    if (isSuperPack)
                    {
                        float r = Random.value;
                        if (r < 0.50f) drawn.Add(RollCardByRarity(CardRarity.Uncommon));
                        else if (r < 0.85f) drawn.Add(RollCardByRarity(CardRarity.Rare));
                        else drawn.Add(RollCardByRarity(CardRarity.Epic));
                    }
                    else
                    {
                        drawn.Add(RollStandardShopCard());
                    }
                }
            }
            return drawn;
        }
    }
}
