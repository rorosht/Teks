using System.Collections.Generic;
using UnityEngine;
using TeksMaster.Core;

namespace TeksMaster.Visuals
{
    public static class CardArtGenerator
    {
        private static readonly Dictionary<string, Texture2D> _portraitTexCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> _portraitSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _uiIconCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<ShopPackType, Sprite> _shopPackSpriteCache = new Dictionary<ShopPackType, Sprite>();

        private static Texture2D _cardBackTex;
        private static Sprite _cardBackSprite;
        private static Texture2D _courtFloorTex;
        private static Texture2D _chainLinkTex;
        private static Sprite _roundedPanelSprite;
        private static Sprite _triangleDownSprite;
        private static Sprite _triangleUpSprite;

        private static Shader _cachedLitShader;
        private static Material _litTemplateMat;
        private static Material _alphaClipTemplateMat;
        private static Material _emissiveTemplateMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachesOnPlay()
        {
            _cachedLitShader = null;
            _litTemplateMat = null;
            _alphaClipTemplateMat = null;
            _emissiveTemplateMat = null;
            _portraitTexCache.Clear();
            _portraitSpriteCache.Clear();
            _uiIconCache.Clear();
            _shopPackSpriteCache.Clear();
            _cardBackTex = null;
            _cardBackSprite = null;
            _courtFloorTex = null;
            _chainLinkTex = null;
            _roundedPanelSprite = null;
            _triangleDownSprite = null;
            _triangleUpSprite = null;
        }

        private static Material CloneTemplateMaterial(bool transparent, bool emissive)
        {
            if (transparent)
            {
                if (_alphaClipTemplateMat == null)
                    _alphaClipTemplateMat = Resources.Load<Material>("TeksMaterials/TeksAlphaClipTemplate");
                if (_alphaClipTemplateMat != null) return new Material(_alphaClipTemplateMat);
            }
            if (emissive)
            {
                if (_emissiveTemplateMat == null)
                    _emissiveTemplateMat = Resources.Load<Material>("TeksMaterials/TeksEmissiveTemplate");
                if (_emissiveTemplateMat != null) return new Material(_emissiveTemplateMat);
            }
            if (_litTemplateMat == null)
                _litTemplateMat = Resources.Load<Material>("TeksMaterials/TeksLitTemplate");
            if (_litTemplateMat != null) return new Material(_litTemplateMat);

            Shader shader = ResolveLitShader();
            return new Material(shader);
        }

        private static Shader ResolveLitShader()
        {
            if (_cachedLitShader != null) return _cachedLitShader;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            {
                var defMat = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial;
                if (defMat != null) shader = defMat.shader;
            }
            if (shader == null && UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
            {
                var defMat = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultMaterial;
                if (defMat != null) shader = defMat.shader;
            }
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            _cachedLitShader = shader;
            return shader;
        }

        public static Material CreateLitMaterial(
            Color color,
            float smoothness = 0.2f,
            float metallic = 0.0f,
            Texture2D mainTex = null,
            bool transparent = false)
        {
            var mat = CloneTemplateMaterial(transparent, emissive: false);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            }

            if (transparent)
            {
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.35f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2450;
            }

            return mat;
        }

        public static Material CreateUnlitEmissiveMaterial(Color color, float emissionIntensity = 0f, Texture2D mainTex = null)
        {
            var mat = CloneTemplateMaterial(transparent: false, emissive: emissionIntensity > 0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            }

            if (emissionIntensity > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emissionIntensity);
            }

            return mat;
        }

        public static Sprite GetCardPortraitSprite(TeksCardDefinition def, int starRank)
        {
            if (def == null) return null;
            int clamped = Mathf.Clamp(starRank, 1, 5);
            string key = def.Id + "_" + clamped;
            if (_portraitSpriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var tex = GetCardPortraitTexture(def, clamped);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _portraitSpriteCache[key] = sprite;
            return sprite;
        }

        public static Texture2D GetCardPortraitTexture(TeksCardDefinition def, int starRank)
        {
            if (def == null) return Texture2D.whiteTexture;
            int clamped = Mathf.Clamp(starRank, 1, 5);
            string key = def.Id + "_" + clamped;
            if (_portraitTexCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var customArt = Resources.Load<Texture2D>("CardArt/" + def.Id);
            if (customArt != null)
            {
                _portraitTexCache[key] = customArt;
                return customArt;
            }

            const int w = 160;
            const int h = 220;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color rarityColor = TeksDatabase.GetRarityColor(def.Rarity);
            Color bgTop = Color.Lerp(def.PrimaryColor, Color.black, 0.35f);
            Color bgBot = Color.Lerp(def.SecondaryColor, Color.black, 0.25f);

            // Background radial burst
            Vector2 burstCenter = def.Id == "hero" ? new Vector2(0.32f, 0.54f) : new Vector2(0.5f, 0.62f);
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    Color c = Color.Lerp(bgBot, bgTop, ny);

                    Vector2 delta = new Vector2(nx, ny) - burstCenter;
                    float dist = delta.magnitude;
                    float angle = Mathf.Atan2(delta.y, delta.x);
                    float rays = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 7f)), 4f);

                    if (def.Id == "hero")
                    {
                        c = Color.Lerp(new Color(0.16f, 0.36f, 0.78f), new Color(0.98f, 0.56f, 0.14f), Mathf.Clamp01(1.1f - dist * 1.4f));
                        c += new Color(1.0f, 0.76f, 0.25f) * rays * 0.25f * Mathf.Clamp01(1f - dist);
                    }
                    else if (def.Id == "bakunawa")
                    {
                        c = Color.Lerp(new Color(0.04f, 0.14f, 0.34f), new Color(0.08f, 0.42f, 0.68f), ny);
                        c += new Color(0.18f, 0.88f, 1.0f) * rays * 0.18f * Mathf.Clamp01(1f - dist * 1.1f);
                    }
                    else
                    {
                        c += def.AccentColor * rays * 0.22f * Mathf.Clamp01(1f - dist);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            // Archetype-specific custom artwork so every card without a custom JPG has a distinct visual design
            switch (def.Id)
            {
                case "minokawa":
                case "ibong_adarna":
                case "eclipse_sarimanok":
                case "adarna_phoenix":
                case "manananggal_wing":
                case "agila_kid":
                    DrawWingedPhoenixArt(tex, w, h, def);
                    break;
                case "kapre_titan":
                case "berberoka":
                    DrawColossusArt(tex, w, h, def);
                    break;
                case "duwende_trickster":
                case "nuno_guard":
                case "mambabarang":
                    DrawMysticMageArt(tex, w, h, def);
                    break;
                case "tricycle_turret":
                case "scrap_trisikad":
                case "kalesa_strider":
                case "lightning_jeepney":
                    DrawArmoredVehicleMechArt(tex, w, h, def);
                    break;
                case "sorbetes_bot":
                case "balut_drone":
                case "cyber_taho_bot":
                case "halo_halo_core":
                    DrawHoverBotArt(tex, w, h, def);
                    break;
                case "patintero_flash":
                case "sigbin_runner":
                case "luksong_tinik":
                case "sipa_master":
                case "tumbang_striker":
                    DrawSpeedRunnerArt(tex, w, h, def);
                    break;
                default:
                    if (def.Theme == CardTheme.ClassicHero) DrawClassicHeroArt(tex, w, h, def);
                    else if (def.Theme == CardTheme.PinoyMythical) DrawMythicalCreatureArt(tex, w, h, def);
                    else DrawSciFiMechArt(tex, w, h, def);
                    break;
            }

            // Star-rank foil shimmer streaks (scales from 1★ to 5★)
            if (clamped >= 2)
            {
                float foilStrength = (clamped - 1) * 0.16f;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float diag = (x * 0.85f + y * 0.55f) / w;
                        float band1 = Mathf.Exp(-Mathf.Pow((diag - 0.55f) * 8f, 2f));
                        float band2 = Mathf.Exp(-Mathf.Pow((diag - 1.05f) * 10f, 2f));
                        float sparkle = (Mathf.Sin(x * 0.45f) * Mathf.Cos(y * 0.45f) > 0.92f) ? 0.85f : 0f;

                        float shimmer = (band1 * 0.55f + band2 * 0.45f + sparkle * (clamped >= 4 ? 0.5f : 0f)) * foilStrength;
                        if (shimmer > 0.01f)
                        {
                            Color cur = tex.GetPixel(x, y);
                            Color foilTint = Color.Lerp(Color.white, rarityColor, 0.45f);
                            tex.SetPixel(x, y, Color.Lerp(cur, foilTint, Mathf.Clamp01(shimmer)));
                        }
                    }
                }
            }

            // Inner rarity border glow
            int border = 4;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < border || x >= w - border || y < border || y >= h - border)
                    {
                        Color edge = Color.Lerp(rarityColor, Color.white, clamped >= 5 ? 0.35f : 0.1f);
                        tex.SetPixel(x, y, edge);
                    }
                }
            }

            tex.Apply();
            _portraitTexCache[key] = tex;
            return tex;
        }

        private static void DrawClassicHeroArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            Color skin = new Color(0.76f, 0.52f, 0.38f);
            Color hair = new Color(0.10f, 0.08f, 0.08f);
            Color cape = def.Id == "hero" ? new Color(0.84f, 0.15f, 0.18f) : def.SecondaryColor;
            Color suit = def.Id == "hero" ? new Color(0.14f, 0.30f, 0.72f) : def.PrimaryColor;
            Color belt = def.AccentColor;

            // Cape flowing behind
            FillTriangle(tex, new Vector2(w * 0.52f, h * 0.65f), new Vector2(w * 0.94f, h * 0.22f), new Vector2(w * 0.58f, h * 0.14f), cape);
            FillTriangle(tex, new Vector2(w * 0.52f, h * 0.65f), new Vector2(w * 0.88f, h * 0.52f), new Vector2(w * 0.94f, h * 0.22f), Color.Lerp(cape, Color.black, 0.2f));

            // Torso & legs
            FillEllipse(tex, w * 0.56f, h * 0.45f, 26f, 34f, suit);
            FillRect(tex, Mathf.RoundToInt(w * 0.42f), Mathf.RoundToInt(h * 0.34f), Mathf.RoundToInt(w * 0.28f), 7, belt);
            FillEllipse(tex, w * 0.56f, h * 0.49f, 9f, 9f, def.AccentColor); // Chest emblem

            // Hero legs
            FillCapsule(tex, new Vector2(w * 0.49f, h * 0.32f), new Vector2(w * 0.43f, h * 0.10f), 9f, suit);
            FillCapsule(tex, new Vector2(w * 0.62f, h * 0.32f), new Vector2(w * 0.72f, h * 0.10f), 9f, suit);

            // Extended punching arm + golden fist on left (matching Hero in concept art)
            FillCapsule(tex, new Vector2(w * 0.48f, h * 0.56f), new Vector2(w * 0.25f, h * 0.55f), 8f, suit);
            FillEllipse(tex, w * 0.23f, h * 0.55f, 22f, 22f, new Color(1.0f, 0.82f, 0.22f, 0.55f)); // Energy aura
            FillEllipse(tex, w * 0.23f, h * 0.55f, 13f, 12f, new Color(1.0f, 0.90f, 0.42f)); // Glowing fist

            // Other arm
            FillCapsule(tex, new Vector2(w * 0.65f, h * 0.55f), new Vector2(w * 0.77f, h * 0.40f), 7f, suit);

            // Head & expressive Pinoy hero hair
            FillEllipse(tex, w * 0.56f, h * 0.72f, 18f, 21f, skin);
            FillEllipse(tex, w * 0.57f, h * 0.81f, 20f, 13f, hair);
            FillTriangle(tex, new Vector2(w * 0.44f, h * 0.82f), new Vector2(w * 0.68f, h * 0.91f), new Vector2(w * 0.74f, h * 0.77f), hair);
            // Eyes
            FillEllipse(tex, w * 0.48f, h * 0.72f, 3f, 3f, Color.white);
            FillEllipse(tex, w * 0.59f, h * 0.72f, 3f, 3f, Color.white);
            FillEllipse(tex, w * 0.48f, h * 0.72f, 1.8f, 1.8f, Color.black);
            FillEllipse(tex, w * 0.59f, h * 0.72f, 1.8f, 1.8f, Color.black);
        }

        private static void DrawMythicalCreatureArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Glowing moon in background
            FillEllipse(tex, w * 0.56f, h * 0.74f, 28f, 28f, new Color(0.88f, 0.97f, 1.0f));
            FillEllipse(tex, w * 0.56f, h * 0.74f, 36f, 36f, new Color(0.35f, 0.82f, 1.0f, 0.22f));

            Color bodyMain = def.Id == "bakunawa" ? new Color(0.10f, 0.48f, 0.76f) : def.PrimaryColor;
            Color bodyHighlight = def.Id == "bakunawa" ? new Color(0.24f, 0.86f, 0.96f) : def.SecondaryColor;

            // Coiled serpent/winged silhouette
            FillCapsule(tex, new Vector2(w * 0.18f, h * 0.24f), new Vector2(w * 0.82f, h * 0.36f), 17f, bodyMain);
            FillCapsule(tex, new Vector2(w * 0.78f, h * 0.36f), new Vector2(w * 0.32f, h * 0.56f), 15f, bodyHighlight);
            FillCapsule(tex, new Vector2(w * 0.34f, h * 0.56f), new Vector2(w * 0.62f, h * 0.70f), 16f, bodyMain);

            // Spikes / dorsal fins / wings
            for (int i = 0; i < 6; i++)
            {
                float fx = w * (0.24f + i * 0.10f);
                float fy = h * (0.34f + (i % 2) * 0.14f);
                FillTriangle(tex, new Vector2(fx - 7, fy), new Vector2(fx, fy + 18), new Vector2(fx + 7, fy), def.AccentColor);
            }

            // Dragon/creature head & horns
            FillEllipse(tex, w * 0.64f, h * 0.69f, 24f, 17f, bodyMain);
            FillTriangle(tex, new Vector2(w * 0.55f, h * 0.76f), new Vector2(w * 0.42f, h * 0.92f), new Vector2(w * 0.62f, h * 0.82f), def.AccentColor);
            FillTriangle(tex, new Vector2(w * 0.68f, h * 0.77f), new Vector2(w * 0.76f, h * 0.93f), new Vector2(w * 0.75f, h * 0.76f), def.AccentColor);

            // Glowing mythical eyes
            FillEllipse(tex, w * 0.60f, h * 0.72f, 4.5f, 3.2f, def.AccentColor);
            FillEllipse(tex, w * 0.72f, h * 0.71f, 4.5f, 3.2f, def.AccentColor);
        }

        private static void DrawSciFiMechArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            Color armor = def.PrimaryColor;
            Color trim = def.SecondaryColor;
            Color visor = def.AccentColor;

            // Shoulder thrusters / cannons
            FillRect(tex, Mathf.RoundToInt(w * 0.14f), Mathf.RoundToInt(h * 0.52f), 26, 36, trim);
            FillRect(tex, Mathf.RoundToInt(w * 0.70f), Mathf.RoundToInt(h * 0.52f), 26, 36, trim);

            // Angular chest chassis
            FillRect(tex, Mathf.RoundToInt(w * 0.28f), Mathf.RoundToInt(h * 0.34f), Mathf.RoundToInt(w * 0.44f), Mathf.RoundToInt(h * 0.34f), armor);
            FillEllipse(tex, w * 0.50f, h * 0.51f, 12f, 12f, visor); // Reactor core

            // Legs
            FillRect(tex, Mathf.RoundToInt(w * 0.31f), Mathf.RoundToInt(h * 0.10f), 22, 46, Color.Lerp(armor, Color.black, 0.25f));
            FillRect(tex, Mathf.RoundToInt(w * 0.55f), Mathf.RoundToInt(h * 0.10f), 22, 46, Color.Lerp(armor, Color.black, 0.25f));

            // Mech helmet + glowing visor + V-fin crest
            FillRect(tex, Mathf.RoundToInt(w * 0.37f), Mathf.RoundToInt(h * 0.68f), Mathf.RoundToInt(w * 0.26f), 28, armor);
            FillRect(tex, Mathf.RoundToInt(w * 0.40f), Mathf.RoundToInt(h * 0.74f), Mathf.RoundToInt(w * 0.20f), 8, visor);
            FillTriangle(tex, new Vector2(w * 0.50f, h * 0.80f), new Vector2(w * 0.28f, h * 0.92f), new Vector2(w * 0.45f, h * 0.84f), visor);
            FillTriangle(tex, new Vector2(w * 0.50f, h * 0.80f), new Vector2(w * 0.72f, h * 0.92f), new Vector2(w * 0.55f, h * 0.84f), visor);
        }

        private static void DrawWingedPhoenixArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Glowing halo sun/moon behind wings
            FillEllipse(tex, w * 0.50f, h * 0.64f, 44f, 44f, new Color(def.AccentColor.r, def.AccentColor.g, def.AccentColor.b, 0.32f));
            FillEllipse(tex, w * 0.50f, h * 0.64f, 26f, 26f, def.AccentColor);

            // Sweeping outstretched wings
            FillTriangle(tex, new Vector2(w * 0.48f, h * 0.58f), new Vector2(w * 0.06f, h * 0.82f), new Vector2(w * 0.16f, h * 0.40f), def.PrimaryColor);
            FillTriangle(tex, new Vector2(w * 0.52f, h * 0.58f), new Vector2(w * 0.94f, h * 0.82f), new Vector2(w * 0.84f, h * 0.40f), def.PrimaryColor);
            FillTriangle(tex, new Vector2(w * 0.46f, h * 0.54f), new Vector2(w * 0.12f, h * 0.68f), new Vector2(w * 0.24f, h * 0.34f), def.SecondaryColor);
            FillTriangle(tex, new Vector2(w * 0.54f, h * 0.54f), new Vector2(w * 0.88f, h * 0.68f), new Vector2(w * 0.76f, h * 0.34f), def.SecondaryColor);

            // Flowing tail plumes
            FillTriangle(tex, new Vector2(w * 0.44f, h * 0.40f), new Vector2(w * 0.56f, h * 0.40f), new Vector2(w * 0.34f, h * 0.08f), def.AccentColor);
            FillTriangle(tex, new Vector2(w * 0.44f, h * 0.40f), new Vector2(w * 0.56f, h * 0.40f), new Vector2(w * 0.66f, h * 0.08f), def.SecondaryColor);

            // Body, crest & glowing eyes
            FillEllipse(tex, w * 0.50f, h * 0.54f, 18f, 26f, def.PrimaryColor);
            FillEllipse(tex, w * 0.50f, h * 0.74f, 13f, 14f, def.PrimaryColor);
            FillTriangle(tex, new Vector2(w * 0.44f, h * 0.80f), new Vector2(w * 0.50f, h * 0.94f), new Vector2(w * 0.56f, h * 0.80f), def.AccentColor);
            FillEllipse(tex, w * 0.45f, h * 0.74f, 3f, 3f, Color.white);
            FillEllipse(tex, w * 0.55f, h * 0.74f, 3f, 3f, Color.white);
        }

        private static void DrawColossusArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Massive broad shoulders & giant arms
            FillEllipse(tex, w * 0.50f, h * 0.50f, 44f, 42f, def.PrimaryColor);
            FillCapsule(tex, new Vector2(w * 0.22f, h * 0.56f), new Vector2(w * 0.16f, h * 0.16f), 16f, def.SecondaryColor);
            FillCapsule(tex, new Vector2(w * 0.78f, h * 0.56f), new Vector2(w * 0.84f, h * 0.16f), 16f, def.SecondaryColor);
            // Glowing chest runes
            FillTriangle(tex, new Vector2(w * 0.50f, h * 0.62f), new Vector2(w * 0.36f, h * 0.42f), new Vector2(w * 0.64f, h * 0.42f), def.AccentColor);
            // Giant head + fiery eyes
            FillEllipse(tex, w * 0.50f, h * 0.76f, 22f, 20f, def.SecondaryColor);
            FillEllipse(tex, w * 0.42f, h * 0.78f, 5f, 3.5f, def.AccentColor);
            FillEllipse(tex, w * 0.58f, h * 0.78f, 5f, 3.5f, def.AccentColor);
        }

        private static void DrawMysticMageArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Cloak & pointed mystic hat
            FillTriangle(tex, new Vector2(w * 0.50f, h * 0.72f), new Vector2(w * 0.18f, h * 0.12f), new Vector2(w * 0.82f, h * 0.12f), def.PrimaryColor);
            FillTriangle(tex, new Vector2(w * 0.50f, h * 0.94f), new Vector2(w * 0.24f, h * 0.70f), new Vector2(w * 0.76f, h * 0.70f), def.SecondaryColor);
            // Glowing floating lantern/orb staff
            FillCapsule(tex, new Vector2(w * 0.76f, h * 0.14f), new Vector2(w * 0.78f, h * 0.66f), 4f, new Color(0.45f, 0.30f, 0.16f));
            FillEllipse(tex, w * 0.78f, h * 0.68f, 18f, 18f, new Color(def.AccentColor.r, def.AccentColor.g, def.AccentColor.b, 0.45f));
            FillEllipse(tex, w * 0.78f, h * 0.68f, 10f, 10f, def.AccentColor);
            // Glowing eyes under hood
            FillEllipse(tex, w * 0.43f, h * 0.65f, 4f, 3f, def.AccentColor);
            FillEllipse(tex, w * 0.55f, h * 0.65f, 4f, 3f, def.AccentColor);
        }

        private static void DrawArmoredVehicleMechArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Armored chassis & glowing neon wheels/thrusters
            FillRect(tex, Mathf.RoundToInt(w * 0.14f), Mathf.RoundToInt(h * 0.26f), Mathf.RoundToInt(w * 0.72f), Mathf.RoundToInt(h * 0.36f), def.PrimaryColor);
            FillRect(tex, Mathf.RoundToInt(w * 0.22f), Mathf.RoundToInt(h * 0.58f), Mathf.RoundToInt(w * 0.56f), Mathf.RoundToInt(h * 0.20f), def.SecondaryColor);
            // Twin headlights / energy cannons
            FillEllipse(tex, w * 0.28f, h * 0.42f, 11f, 11f, def.AccentColor);
            FillEllipse(tex, w * 0.72f, h * 0.42f, 11f, 11f, def.AccentColor);
            // Wheels / treads
            FillEllipse(tex, w * 0.26f, h * 0.22f, 16f, 16f, new Color(0.12f, 0.14f, 0.18f));
            FillEllipse(tex, w * 0.74f, h * 0.22f, 16f, 16f, new Color(0.12f, 0.14f, 0.18f));
            FillEllipse(tex, w * 0.26f, h * 0.22f, 7f, 7f, def.AccentColor);
            FillEllipse(tex, w * 0.74f, h * 0.22f, 7f, 7f, def.AccentColor);
            // Roof turret / visor
            FillRect(tex, Mathf.RoundToInt(w * 0.34f), Mathf.RoundToInt(h * 0.64f), Mathf.RoundToInt(w * 0.32f), 12, def.AccentColor);
        }

        private static void DrawHoverBotArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Hover thruster plume
            FillTriangle(tex, new Vector2(w * 0.38f, h * 0.30f), new Vector2(w * 0.62f, h * 0.30f), new Vector2(w * 0.50f, h * 0.08f), def.AccentColor);
            // Rounded spherical/capsule drone chassis
            FillEllipse(tex, w * 0.50f, h * 0.54f, 38f, 36f, def.PrimaryColor);
            FillEllipse(tex, w * 0.50f, h * 0.58f, 28f, 16f, def.SecondaryColor);
            // Glowing digital eye screen
            FillEllipse(tex, w * 0.50f, h * 0.58f, 16f, 9f, def.AccentColor);
            // Side stabilizer wings & antenna
            FillCapsule(tex, new Vector2(w * 0.14f, h * 0.54f), new Vector2(w * 0.26f, h * 0.54f), 8f, def.SecondaryColor);
            FillCapsule(tex, new Vector2(w * 0.74f, h * 0.54f), new Vector2(w * 0.86f, h * 0.54f), 8f, def.SecondaryColor);
            FillCapsule(tex, new Vector2(w * 0.50f, h * 0.72f), new Vector2(w * 0.50f, h * 0.88f), 3f, def.AccentColor);
            FillEllipse(tex, w * 0.50f, h * 0.89f, 6f, 6f, def.AccentColor);
        }

        private static void DrawSpeedRunnerArt(Texture2D tex, int w, int h, TeksCardDefinition def)
        {
            // Diagonal speed lines
            for (int i = 0; i < 4; i++)
            {
                float y0 = h * (0.24f + i * 0.16f);
                FillCapsule(tex, new Vector2(w * 0.08f, y0), new Vector2(w * 0.44f, y0 + 14f), 3.5f, def.AccentColor);
            }
            // Dynamic sprinting kid/creature silhouette
            Color skin = new Color(0.76f, 0.52f, 0.38f);
            FillCapsule(tex, new Vector2(w * 0.42f, h * 0.34f), new Vector2(w * 0.62f, h * 0.58f), 15f, def.PrimaryColor);
            FillCapsule(tex, new Vector2(w * 0.44f, h * 0.34f), new Vector2(w * 0.22f, h * 0.14f), 8f, def.SecondaryColor);
            FillCapsule(tex, new Vector2(w * 0.50f, h * 0.34f), new Vector2(w * 0.78f, h * 0.18f), 8f, def.SecondaryColor);
            FillEllipse(tex, w * 0.68f, h * 0.72f, 16f, 18f, skin);
            FillTriangle(tex, new Vector2(w * 0.54f, h * 0.78f), new Vector2(w * 0.76f, h * 0.90f), new Vector2(w * 0.84f, h * 0.74f), def.AccentColor);
        }

        public static Texture2D GetCardBackTexture()
        {
            if (_cardBackTex != null) return _cardBackTex;

            var customBack = Resources.Load<Texture2D>("CardArt/card_back");
            if (customBack != null)
            {
                _cardBackTex = customBack;
                return _cardBackTex;
            }

            const int w = 128;
            const int h = 180;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color cardboard = new Color(0.68f, 0.54f, 0.36f);
            Color darkBrown = new Color(0.38f, 0.25f, 0.14f);
            Color cream = new Color(0.92f, 0.84f, 0.66f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool outerBorder = x < 8 || x >= w - 8 || y < 8 || y >= h - 8;
                    bool checker = ((x / 8) + (y / 8)) % 2 == 0;
                    Color c = cardboard;

                    if (outerBorder)
                    {
                        c = checker ? darkBrown : cream;
                    }
                    else if (x < 12 || x >= w - 12 || y < 12 || y >= h - 12)
                    {
                        c = darkBrown;
                    }
                    else
                    {
                        float grain = (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 0.08f;
                        c = cardboard + new Color(grain, grain * 0.8f, grain * 0.6f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            // Center diamond emblem on cardboard back
            for (int y = 25; y < h - 25; y++)
            {
                for (int x = 20; x < w - 20; x++)
                {
                    float dx = Mathf.Abs(x - w * 0.5f) / (w * 0.30f);
                    float dy = Mathf.Abs(y - h * 0.5f) / (h * 0.30f);
                    if (dx + dy < 1.0f && dx + dy > 0.82f)
                    {
                        tex.SetPixel(x, y, darkBrown);
                    }
                    else if (dx + dy < 0.72f)
                    {
                        tex.SetPixel(x, y, Color.Lerp(cardboard, cream, 0.35f));
                    }
                }
            }

            tex.Apply();
            _cardBackTex = tex;
            return tex;
        }

        public static Sprite GetCardBackSprite()
        {
            if (_cardBackSprite != null) return _cardBackSprite;
            var tex = GetCardBackTexture();
            _cardBackSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _cardBackSprite;
        }

        public static Texture2D GetCourtFloorTexture()
        {
            if (_courtFloorTex != null) return _courtFloorTex;

            const int w = 1024;
            const int h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color concreteBase = new Color(0.56f, 0.53f, 0.49f);
            Color concreteDark = new Color(0.45f, 0.42f, 0.39f);
            Color terracottaKey = new Color(0.64f, 0.34f, 0.25f);
            Color fadedWhite = new Color(0.89f, 0.87f, 0.82f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float noise = Mathf.PerlinNoise(nx * 14f, ny * 7f);
                    float fine = Mathf.PerlinNoise(nx * 60f, ny * 30f);
                    Color c = Color.Lerp(concreteDark, concreteBase, noise * 0.75f + fine * 0.25f);

                    // Slight concrete slab joints
                    if (Mathf.Abs((nx * 8f) % 1f) < 0.015f || Mathf.Abs((ny * 4f) % 1f) < 0.018f)
                    {
                        c = Color.Lerp(c, concreteDark, 0.45f);
                    }

                    // Free-throw terracotta painted keys (left & right)
                    bool inLeftKey = nx >= 0.04f && nx <= 0.22f && ny >= 0.32f && ny <= 0.68f;
                    bool inRightKey = nx >= 0.78f && nx <= 0.96f && ny >= 0.32f && ny <= 0.68f;
                    if (inLeftKey || inRightKey)
                    {
                        float wear = Mathf.Lerp(0.65f, 0.95f, noise);
                        c = Color.Lerp(c, terracottaKey, wear);
                    }

                    // Court outer border lines
                    bool onOuterLine =
                        ((nx >= 0.038f && nx <= 0.046f) || (nx >= 0.954f && nx <= 0.962f)) && ny >= 0.06f && ny <= 0.94f ||
                        ((ny >= 0.055f && ny <= 0.070f) || (ny >= 0.930f && ny <= 0.945f)) && nx >= 0.038f && nx <= 0.962f;

                    // Half-court line
                    bool onCenterLine = nx >= 0.496f && nx <= 0.504f && ny >= 0.06f && ny <= 0.94f;

                    // Center play circle
                    float dCenter = Vector2.Distance(new Vector2((nx - 0.5f) * 2f, ny - 0.5f), Vector2.zero);
                    bool onCenterCircle = Mathf.Abs(dCenter - 0.175f) < 0.011f;

                    // Free-throw box lines
                    bool onKeyBorder =
                        (nx >= 0.04f && nx <= 0.22f && (Mathf.Abs(ny - 0.32f) < 0.008f || Mathf.Abs(ny - 0.68f) < 0.008f)) ||
                        (Mathf.Abs(nx - 0.22f) < 0.004f && ny >= 0.32f && ny <= 0.68f) ||
                        (nx >= 0.78f && nx <= 0.96f && (Mathf.Abs(ny - 0.32f) < 0.008f || Mathf.Abs(ny - 0.68f) < 0.008f)) ||
                        (Mathf.Abs(nx - 0.78f) < 0.004f && ny >= 0.32f && ny <= 0.68f);

                    // Free-throw circles
                    float dLeftFt = Vector2.Distance(new Vector2((nx - 0.22f) * 2f, ny - 0.5f), Vector2.zero);
                    float dRightFt = Vector2.Distance(new Vector2((nx - 0.78f) * 2f, ny - 0.5f), Vector2.zero);
                    bool onFtCircle = Mathf.Abs(dLeftFt - 0.18f) < 0.009f || Mathf.Abs(dRightFt - 0.18f) < 0.009f;

                    // 3-Point arcs
                    float dLeft3 = Vector2.Distance(new Vector2((nx - 0.08f) * 2f, ny - 0.5f), Vector2.zero);
                    float dRight3 = Vector2.Distance(new Vector2((nx - 0.92f) * 2f, ny - 0.5f), Vector2.zero);
                    bool on3PtArc =
                        (nx >= 0.11f && Mathf.Abs(dLeft3 - 0.42f) < 0.010f) ||
                        (nx <= 0.89f && Mathf.Abs(dRight3 - 0.42f) < 0.010f);

                    if (onOuterLine || onCenterLine || onCenterCircle || onKeyBorder || onFtCircle || on3PtArc)
                    {
                        float paintWear = Mathf.Lerp(0.55f, 0.92f, fine);
                        c = Color.Lerp(c, fadedWhite, paintWear);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            _courtFloorTex = tex;
            return tex;
        }

        public static Texture2D GetChainLinkTexture()
        {
            if (_chainLinkTex != null) return _chainLinkTex;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color wire = new Color(0.56f, 0.58f, 0.62f, 0.95f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int modA = (x + y) % 16;
                    int modB = (x - y + size * 2) % 16;
                    bool isWire = modA < 2 || modB < 2;
                    tex.SetPixel(x, y, isWire ? wire : clear);
                }
            }

            tex.Apply();
            _chainLinkTex = tex;
            return tex;
        }

        public static Sprite GetRoundedPanelSprite()
        {
            if (_roundedPanelSprite != null) return _roundedPanelSprite;

            const int size = 64;
            const int radius = 12;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;
                    int cx = x < radius ? radius : (x >= size - radius ? size - radius - 1 : x);
                    int cy = y < radius ? radius : (y >= size - radius ? size - radius - 1 : y);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist > radius) alpha = 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            _roundedPanelSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            return _roundedPanelSprite;
        }

        public static Sprite GetTriangleSprite(bool pointingDown)
        {
            if (pointingDown && _triangleDownSprite != null) return _triangleDownSprite;
            if (!pointingDown && _triangleUpSprite != null) return _triangleUpSprite;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / (size - 1);
                float halfWidth = pointingDown ? ny * 0.5f : (1f - ny) * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((float)x / (size - 1) - 0.5f);
                    tex.SetPixel(x, y, nx <= halfWidth ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            if (pointingDown) _triangleDownSprite = sprite;
            else _triangleUpSprite = sprite;
            return sprite;
        }

        public static Sprite GetUIIconSprite(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) iconName = "default";

            if (_uiIconCache.TryGetValue(iconName, out var cached) && cached != null)
                return cached;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            ClearTex(tex, Color.clear);

            switch (iconName)
            {
                case "backpack":
                    FillEllipse(tex, 32, 30, 18, 22, new Color(0.54f, 0.33f, 0.16f));
                    FillEllipse(tex, 32, 43, 15, 11, new Color(0.65f, 0.42f, 0.22f));
                    FillRect(tex, 20, 16, 24, 14, new Color(0.42f, 0.24f, 0.11f));
                    FillRect(tex, 25, 34, 14, 5, new Color(0.95f, 0.78f, 0.22f)); // Buckle
                    break;
                case "shop":
                    FillRect(tex, 10, 10, 44, 28, new Color(0.82f, 0.76f, 0.66f)); // Stall body
                    FillRect(tex, 18, 12, 28, 16, new Color(0.24f, 0.52f, 0.72f)); // Window
                    for (int stripe = 0; stripe < 6; stripe++)
                    {
                        Color aw = stripe % 2 == 0 ? new Color(0.86f, 0.22f, 0.20f) : Color.white;
                        FillRect(tex, 8 + stripe * 8, 36, 8, 16, aw); // Awning
                    }
                    break;
                case "coin":
                    // Embossed Golden Barya Coin with rim, inner ring, shine, and Peso/Barya emblem
                    FillEllipse(tex, 32, 32, 26f, 26f, new Color(0.66f, 0.40f, 0.04f));
                    FillEllipse(tex, 32, 32, 23.5f, 23.5f, new Color(0.98f, 0.76f, 0.12f));
                    FillEllipse(tex, 32, 32, 18.5f, 18.5f, new Color(0.84f, 0.56f, 0.06f));
                    FillEllipse(tex, 32, 32, 16f, 16f, new Color(1.0f, 0.87f, 0.28f));
                    // Gloss crescent at top-left
                    FillEllipse(tex, 25, 42, 7f, 4f, new Color(1.0f, 0.98f, 0.78f, 0.85f));
                    // Stylized peso/barya vertical bar + crossbars
                    FillRect(tex, 30, 19, 4, 26, new Color(0.76f, 0.46f, 0.04f));
                    FillRect(tex, 24, 34, 15, 4, new Color(0.76f, 0.46f, 0.04f));
                    FillRect(tex, 24, 27, 15, 4, new Color(0.76f, 0.46f, 0.04f));
                    FillEllipse(tex, 37, 37, 4.5f, 4.5f, new Color(0.76f, 0.46f, 0.04f));
                    FillEllipse(tex, 36, 37, 2.2f, 2.2f, new Color(1.0f, 0.87f, 0.28f));
                    break;
                case "star":
                    DrawStar(tex, 32, 32, 24f, 10f, new Color(1.0f, 0.86f, 0.18f));
                    break;
                case "bolt":
                    FillTriangle(tex, new Vector2(36, 56), new Vector2(18, 30), new Vector2(34, 30), new Color(1.0f, 0.86f, 0.18f));
                    FillTriangle(tex, new Vector2(28, 8), new Vector2(46, 34), new Vector2(30, 34), new Color(1.0f, 0.86f, 0.18f));
                    break;
                case "heart":
                    FillEllipse(tex, 22, 38, 13, 13, new Color(0.95f, 0.24f, 0.28f));
                    FillEllipse(tex, 42, 38, 13, 13, new Color(0.95f, 0.24f, 0.28f));
                    FillTriangle(tex, new Vector2(10, 35), new Vector2(54, 35), new Vector2(32, 10), new Color(0.95f, 0.24f, 0.28f));
                    break;
                case "sword":
                    FillCapsule(tex, new Vector2(14, 14), new Vector2(52, 52), 4.5f, new Color(0.92f, 0.95f, 1.0f));
                    FillCapsule(tex, new Vector2(16, 28), new Vector2(28, 16), 3.5f, new Color(0.96f, 0.74f, 0.18f));
                    break;
                case "spin":
                    FillEllipse(tex, 32, 32, 20, 20, new Color(0.32f, 0.88f, 0.44f));
                    FillEllipse(tex, 32, 32, 10, 10, Color.clear);
                    FillTriangle(tex, new Vector2(46, 46), new Vector2(58, 34), new Vector2(40, 32), new Color(0.32f, 0.88f, 0.44f));
                    break;
                case "shield":
                    FillRect(tex, 16, 28, 32, 24, new Color(0.28f, 0.68f, 0.98f));
                    FillTriangle(tex, new Vector2(16, 28), new Vector2(48, 28), new Vector2(32, 8), new Color(0.28f, 0.68f, 0.98f));
                    break;
                case "gem":
                    // Faceted Sapphire-Cyan Barangay Gem with dark border and crisp highlights
                    FillTriangle(tex, new Vector2(7, 40), new Vector2(57, 40), new Vector2(32, 5), new Color(0.04f, 0.32f, 0.62f));
                    FillTriangle(tex, new Vector2(7, 40), new Vector2(18, 57), new Vector2(46, 57), new Color(0.06f, 0.44f, 0.78f));
                    FillTriangle(tex, new Vector2(7, 40), new Vector2(46, 57), new Vector2(57, 40), new Color(0.06f, 0.44f, 0.78f));

                    FillTriangle(tex, new Vector2(10, 40), new Vector2(54, 40), new Vector2(32, 8), new Color(0.10f, 0.58f, 0.92f));
                    FillTriangle(tex, new Vector2(10, 40), new Vector2(20, 54), new Vector2(44, 54), new Color(0.28f, 0.90f, 1.0f));
                    FillTriangle(tex, new Vector2(10, 40), new Vector2(44, 54), new Vector2(54, 40), new Color(0.18f, 0.80f, 0.98f));
                    FillTriangle(tex, new Vector2(20, 40), new Vector2(44, 40), new Vector2(32, 11), new Color(0.26f, 0.86f, 1.0f));
                    FillTriangle(tex, new Vector2(22, 40), new Vector2(32, 53), new Vector2(42, 40), new Color(0.78f, 0.99f, 1.0f));
                    FillTriangle(tex, new Vector2(20, 54), new Vector2(32, 53), new Vector2(22, 40), new Color(0.94f, 1.0f, 1.0f));
                    DrawStar(tex, 21, 49, 6f, 2.4f, Color.white);
                    break;
                case "trophy":
                    // Golden Championship Trophy with dark rim & bright cup sheen
                    FillEllipse(tex, 15, 42, 8.5f, 9.5f, new Color(0.68f, 0.44f, 0.04f));
                    FillEllipse(tex, 15, 42, 7.2f, 8.2f, new Color(0.94f, 0.72f, 0.14f));
                    FillEllipse(tex, 15, 42, 3.8f, 4.8f, Color.clear);
                    FillEllipse(tex, 49, 42, 8.5f, 9.5f, new Color(0.68f, 0.44f, 0.04f));
                    FillEllipse(tex, 49, 42, 7.2f, 8.2f, new Color(0.94f, 0.72f, 0.14f));
                    FillEllipse(tex, 49, 42, 3.8f, 4.8f, Color.clear);
                    FillRect(tex, 17, 34, 30, 21, new Color(0.68f, 0.44f, 0.04f));
                    FillRect(tex, 18, 34, 28, 20, new Color(0.98f, 0.78f, 0.14f));
                    FillEllipse(tex, 32, 34, 14, 12, new Color(0.98f, 0.78f, 0.14f));
                    FillRect(tex, 21, 36, 5, 15, new Color(1.0f, 0.95f, 0.60f)); // Cup highlight
                    FillRect(tex, 18, 52, 28, 2, new Color(1.0f, 0.94f, 0.56f)); // Top lip shine
                    FillRect(tex, 28, 17, 8, 11, new Color(0.86f, 0.62f, 0.08f)); // Stem
                    FillRect(tex, 20, 12, 24, 6, new Color(0.78f, 0.54f, 0.08f)); // Upper base
                    FillRect(tex, 15, 7, 34, 6, new Color(0.42f, 0.24f, 0.08f));  // Pedestal
                    break;
                case "lock":
                    // Padlock icon for locked areas/gates
                    FillEllipse(tex, 32, 42, 14, 14, new Color(0.76f, 0.80f, 0.86f));
                    FillEllipse(tex, 32, 42, 8, 8, Color.clear);
                    FillRect(tex, 18, 32, 6, 12, new Color(0.76f, 0.80f, 0.86f));
                    FillRect(tex, 40, 32, 6, 12, new Color(0.76f, 0.80f, 0.86f));
                    FillRect(tex, 14, 10, 36, 26, new Color(0.95f, 0.72f, 0.16f)); // Body
                    FillRect(tex, 16, 12, 32, 22, new Color(1.0f, 0.82f, 0.28f));  // Inner face
                    FillEllipse(tex, 32, 25, 4.5f, 4.5f, new Color(0.24f, 0.16f, 0.06f)); // Keyhole top
                    FillTriangle(tex, new Vector2(29, 24), new Vector2(35, 24), new Vector2(32, 15), new Color(0.24f, 0.16f, 0.06f));
                    break;
                case "sound":
                    // Speaker + acoustic sound waves
                    FillRect(tex, 10, 24, 12, 16, new Color(0.94f, 0.96f, 1.0f));
                    FillTriangle(tex, new Vector2(18, 32), new Vector2(34, 48), new Vector2(34, 16), new Color(0.94f, 0.96f, 1.0f));
                    for (int y = 10; y < 54; y++)
                    {
                        for (int x = 36; x < 58; x++)
                        {
                            float dx = x - 32f;
                            float dy = y - 32f;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            float angle = Mathf.Abs(Mathf.Atan2(dy, dx));
                            if (angle <= 0.78f && (Mathf.Abs(dist - 12f) <= 2.1f || Mathf.Abs(dist - 20f) <= 2.1f))
                            {
                                SetPixelSafe(tex, x, y, new Color(0.45f, 0.92f, 1.0f));
                            }
                        }
                    }
                    break;
                case "settings":
                    {
                        Color gearOuter = new Color(0.84f, 0.92f, 1.0f);
                        Color gearRim = new Color(0.56f, 0.72f, 0.90f);
                        Color gearInner = new Color(0.16f, 0.24f, 0.36f);
                        for (int t = 0; t < 8; t++)
                        {
                            float a = t * Mathf.PI * 0.25f;
                            Vector2 tip = new Vector2(32f + Mathf.Cos(a) * 23f, 32f + Mathf.Sin(a) * 23f);
                            FillCapsule(tex, new Vector2(32f, 32f), tip, 4.4f, gearRim);
                        }
                        FillEllipse(tex, 32, 32, 17.5f, 17.5f, gearOuter);
                        FillEllipse(tex, 32, 32, 11.5f, 11.5f, gearInner);
                        FillEllipse(tex, 32, 32, 6.2f, 6.2f, Color.clear);
                    }
                    break;
                case "fusion":
                    {
                        Color violetGlow = new Color(0.80f, 0.42f, 1.0f);
                        Color magentaCore = new Color(0.95f, 0.30f, 0.82f);
                        Color goldSpark = new Color(1.0f, 0.88f, 0.30f);
                        // Outer alchemical diamond
                        FillTriangle(tex, new Vector2(32, 58), new Vector2(10, 32), new Vector2(54, 32), violetGlow);
                        FillTriangle(tex, new Vector2(32, 6), new Vector2(10, 32), new Vector2(54, 32), magentaCore);
                        // Inner golden fusion core
                        FillTriangle(tex, new Vector2(32, 48), new Vector2(20, 32), new Vector2(44, 32), goldSpark);
                        FillTriangle(tex, new Vector2(32, 16), new Vector2(20, 32), new Vector2(44, 32), goldSpark);
                        FillEllipse(tex, 32, 32, 6f, 6f, Color.white);
                        DrawStar(tex, 14, 50, 5f, 2f, new Color(0.55f, 0.96f, 1.0f));
                        DrawStar(tex, 50, 14, 5f, 2f, goldSpark);
                    }
                    break;
                case "missions":
                    {
                        Color parchment = new Color(0.98f, 0.93f, 0.78f);
                        Color goldEdge = new Color(0.95f, 0.68f, 0.16f);
                        Color ink = new Color(0.24f, 0.30f, 0.44f);
                        FillRect(tex, 14, 10, 36, 44, goldEdge);
                        FillRect(tex, 17, 13, 30, 38, parchment);
                        FillRect(tex, 11, 48, 42, 7, goldEdge);
                        FillRect(tex, 11, 9, 42, 7, goldEdge);
                        FillRect(tex, 21, 37, 22, 4, ink);
                        FillRect(tex, 21, 29, 18, 4, ink);
                        FillRect(tex, 21, 21, 20, 4, ink);
                        DrawStar(tex, 46, 20, 9f, 4f, new Color(1.0f, 0.82f, 0.18f));
                    }
                    break;
                case "location":
                    {
                        Color pinOuter = new Color(0.32f, 0.88f, 1.0f);
                        Color pinRim = new Color(0.14f, 0.56f, 0.90f);
                        FillTriangle(tex, new Vector2(17, 36), new Vector2(47, 36), new Vector2(32, 8), pinRim);
                        FillEllipse(tex, 32, 40, 16, 16, pinOuter);
                        FillEllipse(tex, 32, 40, 7, 7, Color.white);
                    }
                    break;
                default:
                    FillEllipse(tex, 32, 32, 20, 20, Color.white);
                    break;
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _uiIconCache[iconName] = sprite;
            return sprite;
        }

        public static Sprite GetShopPackSprite(ShopPackType packType)
        {
            if (_shopPackSpriteCache.TryGetValue(packType, out var cached) && cached != null)
                return cached;

            const int w = 160;
            const int h = 210;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            ClearTex(tex, Color.clear);

            Color botCol;
            Color topCol;
            Color trimCol;
            Color crimpCol;
            Color emblemCol;

            switch (packType)
            {
                case ShopPackType.Premium:
                    botCol = new Color(0.06f, 0.32f, 0.22f);
                    topCol = new Color(0.18f, 0.76f, 0.50f);
                    trimCol = new Color(0.98f, 0.84f, 0.28f);
                    crimpCol = new Color(0.84f, 0.92f, 0.86f);
                    emblemCol = new Color(1.0f, 0.88f, 0.28f);
                    break;
                case ShopPackType.Rare:
                    botCol = new Color(0.24f, 0.08f, 0.44f);
                    topCol = new Color(0.66f, 0.24f, 0.90f);
                    trimCol = new Color(0.42f, 0.94f, 1.0f);
                    crimpCol = new Color(0.84f, 0.80f, 0.96f);
                    emblemCol = new Color(0.36f, 0.94f, 1.0f);
                    break;
                case ShopPackType.SpecialGem:
                    botCol = new Color(0.18f, 0.08f, 0.36f);
                    topCol = new Color(0.94f, 0.46f, 0.08f);
                    trimCol = new Color(1.0f, 0.86f, 0.24f);
                    crimpCol = new Color(1.0f, 0.90f, 0.58f);
                    emblemCol = new Color(0.32f, 0.92f, 1.0f);
                    break;
                case ShopPackType.DailyFree:
                    botCol = new Color(0.56f, 0.12f, 0.16f);
                    topCol = new Color(0.96f, 0.62f, 0.14f);
                    trimCol = new Color(1.0f, 0.92f, 0.42f);
                    crimpCol = new Color(1.0f, 0.86f, 0.46f);
                    emblemCol = new Color(1.0f, 0.92f, 0.36f);
                    break;
                default:
                    botCol = new Color(0.08f, 0.24f, 0.58f);
                    topCol = new Color(0.22f, 0.62f, 0.96f);
                    trimCol = new Color(0.96f, 0.84f, 0.28f);
                    crimpCol = new Color(0.82f, 0.88f, 0.96f);
                    emblemCol = new Color(1.0f, 0.86f, 0.26f);
                    break;
            }

            const int marginX = 6;
            const int crimpH = 18;
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = marginX; x < w - marginX; x++)
                {
                    // Serrated foil teeth at very top and bottom edges
                    bool inBottomTeeth = y < 4 && ((x - marginX) % 8 < 4) && y < 2;
                    bool inTopTeeth = y >= h - 4 && ((x - marginX) % 8 < 4) && y >= h - 2;
                    if (inBottomTeeth || inTopTeeth) continue;

                    bool inCrimp = y < crimpH || y >= h - crimpH;
                    Color c;
                    if (inCrimp)
                    {
                        float groove = ((x - marginX) % 5 == 0) ? 0.72f : (((x - marginX) % 5 == 1) ? 1.12f : 0.94f);
                        c = crimpCol * groove;
                        c.a = 1f;
                    }
                    else if (y == crimpH || y == crimpH + 1 || y == h - crimpH - 1 || y == h - crimpH - 2 ||
                             x <= marginX + 2 || x >= w - marginX - 3)
                    {
                        c = trimCol;
                    }
                    else
                    {
                        c = Color.Lerp(botCol, topCol, (ny - 0.08f) / 0.84f);

                        // Center sunburst rays behind emblem
                        Vector2 delta = new Vector2((float)x / w - 0.5f, (ny - 0.52f) * 1.25f);
                        float dist = delta.magnitude;
                        float ang = Mathf.Atan2(delta.y, delta.x);
                        float ray = Mathf.Pow(Mathf.Abs(Mathf.Sin(ang * 8f)), 6f);
                        c += trimCol * (ray * 0.24f * Mathf.Clamp01(1f - dist * 1.4f));

                        // Glossy diagonal foil sheen
                        float diag = (x * 0.85f + y * 0.45f) / w;
                        float sheen = Mathf.Exp(-Mathf.Pow((diag - 0.62f) * 7f, 2f)) * 0.30f
                                    + Mathf.Exp(-Mathf.Pow((diag - 1.12f) * 9f, 2f)) * 0.22f;
                        c = Color.Lerp(c, Color.white, Mathf.Clamp01(sheen));
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            // Inner foil framing card badge
            FillRect(tex, 18, 30, w - 36, h - 60, new Color(0.06f, 0.09f, 0.16f, 0.36f));
            FillRect(tex, 18, 30, w - 36, 3, new Color(trimCol.r, trimCol.g, trimCol.b, 0.85f));
            FillRect(tex, 18, h - 33, w - 36, 3, new Color(trimCol.r, trimCol.g, trimCol.b, 0.85f));

            DrawShopPackEmblem(tex, w, h, packType, trimCol, emblemCol);

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _shopPackSpriteCache[packType] = sprite;
            return sprite;
        }

        private static void DrawShopPackEmblem(Texture2D tex, int w, int h, ShopPackType packType, Color trimCol, Color emblemCol)
        {
            float cx = w * 0.5f;
            float cy = h * 0.52f;

            // Medallion halo behind centerpiece
            FillEllipse(tex, cx, cy, 38f, 38f, new Color(trimCol.r, trimCol.g, trimCol.b, 0.25f));
            FillEllipse(tex, cx, cy, 31f, 31f, new Color(0.07f, 0.10f, 0.18f, 0.86f));

            if (packType == ShopPackType.DailyFree)
            {
                // Gift ribbon cross + 3 fiesta stars
                FillRect(tex, Mathf.RoundToInt(cx - 5f), 34, 10, h - 68, new Color(0.92f, 0.20f, 0.22f, 0.90f));
                FillRect(tex, 20, Mathf.RoundToInt(cy - 5f), w - 40, 10, new Color(0.92f, 0.20f, 0.22f, 0.90f));
                FillEllipse(tex, cx, cy, 29f, 29f, new Color(0.18f, 0.10f, 0.06f, 0.90f));
                DrawStar(tex, cx - 18f, cy - 6f, 10f, 4.2f, trimCol);
                DrawStar(tex, cx + 18f, cy - 6f, 10f, 4.2f, trimCol);
                DrawStar(tex, cx, cy + 7f, 18f, 8f, emblemCol);
                return;
            }

            if (packType == ShopPackType.SpecialGem)
            {
                // Faceted Mythic Gem centerpiece
                FillTriangle(tex, new Vector2(cx - 26f, cy + 8f), new Vector2(cx + 26f, cy + 8f), new Vector2(cx, cy - 28f), new Color(0.08f, 0.52f, 0.88f));
                FillTriangle(tex, new Vector2(cx - 26f, cy + 8f), new Vector2(cx - 14f, cy + 24f), new Vector2(cx + 14f, cy + 24f), emblemCol);
                FillTriangle(tex, new Vector2(cx - 26f, cy + 8f), new Vector2(cx + 14f, cy + 24f), new Vector2(cx + 26f, cy + 8f), new Color(0.20f, 0.80f, 0.98f));
                FillTriangle(tex, new Vector2(cx - 12f, cy + 8f), new Vector2(cx, cy + 22f), new Vector2(cx + 12f, cy + 8f), Color.white);
                DrawStar(tex, cx - 18f, cy + 22f, 7f, 2.8f, Color.white);
                DrawStar(tex, cx + 20f, cy - 16f, 6f, 2.5f, trimCol);
                return;
            }

            if (packType == ShopPackType.Rare)
            {
                // Radiant diamond crest + lightning bolt
                FillTriangle(tex, new Vector2(cx, cy + 29f), new Vector2(cx - 26f, cy), new Vector2(cx + 26f, cy), emblemCol);
                FillTriangle(tex, new Vector2(cx, cy - 29f), new Vector2(cx - 26f, cy), new Vector2(cx + 26f, cy), new Color(0.76f, 0.36f, 1.0f));
                FillTriangle(tex, new Vector2(cx + 5f, cy + 20f), new Vector2(cx - 12f, cy - 2f), new Vector2(cx + 3f, cy - 2f), Color.white);
                FillTriangle(tex, new Vector2(cx - 5f, cy - 20f), new Vector2(cx + 12f, cy + 2f), new Vector2(cx - 3f, cy + 2f), trimCol);
                DrawStar(tex, cx - 22f, h - 46f, 6f, 2.5f, trimCol);
                DrawStar(tex, cx + 22f, h - 46f, 6f, 2.5f, trimCol);
                return;
            }

            if (packType == ShopPackType.Premium)
            {
                // Crown emblem + 3 stars
                FillRect(tex, Mathf.RoundToInt(cx - 20f), Mathf.RoundToInt(cy - 10f), 40, 12, emblemCol);
                FillTriangle(tex, new Vector2(cx - 20f, cy + 2f), new Vector2(cx - 23f, cy + 22f), new Vector2(cx - 7f, cy + 2f), emblemCol);
                FillTriangle(tex, new Vector2(cx - 8f, cy + 2f), new Vector2(cx, cy + 26f), new Vector2(cx + 8f, cy + 2f), Color.white);
                FillTriangle(tex, new Vector2(cx + 7f, cy + 2f), new Vector2(cx + 23f, cy + 22f), new Vector2(cx + 20f, cy + 2f), emblemCol);
                DrawStar(tex, cx - 18f, cy - 20f, 6f, 2.5f, trimCol);
                DrawStar(tex, cx, cy - 22f, 7f, 3f, Color.white);
                DrawStar(tex, cx + 18f, cy - 20f, 6f, 2.5f, trimCol);
                return;
            }

            // Basic: Classic 3-Star Teks Shield Emblem
            FillRect(tex, Mathf.RoundToInt(cx - 19f), Mathf.RoundToInt(cy - 2f), 38, 22, trimCol);
            FillTriangle(tex, new Vector2(cx - 19f, cy - 2f), new Vector2(cx + 19f, cy - 2f), new Vector2(cx, cy - 25f), trimCol);
            FillRect(tex, Mathf.RoundToInt(cx - 15f), Mathf.RoundToInt(cy + 1f), 30, 16, new Color(0.10f, 0.24f, 0.54f));
            FillTriangle(tex, new Vector2(cx - 15f, cy + 1f), new Vector2(cx + 15f, cy + 1f), new Vector2(cx, cy - 19f), new Color(0.10f, 0.24f, 0.54f));
            DrawStar(tex, cx, cy + 3f, 13f, 5.5f, emblemCol);
            DrawStar(tex, cx - 18f, h - 46f, 5.5f, 2.2f, Color.white);
            DrawStar(tex, cx + 18f, h - 46f, 5.5f, 2.2f, Color.white);
        }

        private static void ClearTex(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
        }

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color col)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
            if (col.a >= 0.999f)
            {
                tex.SetPixel(x, y, col);
            }
            else if (col.a <= 0.001f)
            {
                if (col == Color.clear) tex.SetPixel(x, y, Color.clear);
            }
            else
            {
                Color dst = tex.GetPixel(x, y);
                tex.SetPixel(x, y, Color.Lerp(dst, new Color(col.r, col.g, col.b, 1f), col.a));
            }
        }

        private static void FillRect(Texture2D tex, int x0, int y0, int width, int height, Color col)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                for (int x = x0; x < x0 + width; x++)
                {
                    SetPixelSafe(tex, x, y, col);
                }
            }
        }

        private static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color col)
        {
            int minX = Mathf.FloorToInt(cx - rx - 1);
            int maxX = Mathf.CeilToInt(cx + rx + 1);
            int minY = Mathf.FloorToInt(cy - ry - 1);
            int maxY = Mathf.CeilToInt(cy + ry + 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx) / Mathf.Max(0.01f, rx);
                    float dy = (y - cy) / Mathf.Max(0.01f, ry);
                    if (dx * dx + dy * dy <= 1f)
                    {
                        SetPixelSafe(tex, x, y, col);
                    }
                }
            }
        }

        private static void FillCapsule(Texture2D tex, Vector2 a, Vector2 b, float radius, Color col)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1);
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1);
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1);
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1);

            Vector2 ab = b - a;
            float lenSq = Mathf.Max(0.001f, ab.sqrMagnitude);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                    Vector2 proj = a + ab * t;
                    if (Vector2.Distance(p, proj) <= radius)
                    {
                        SetPixelSafe(tex, x, y, col);
                    }
                }
            }
        }

        private static void FillTriangle(Texture2D tex, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (PointInTriangle(p, a, b, c))
                    {
                        SetPixelSafe(tex, x, y, col);
                    }
                }
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float s2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float s3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool hasNeg = (s1 < 0) || (s2 < 0) || (s3 < 0);
            bool hasPos = (s1 > 0) || (s2 > 0) || (s3 > 0);
            return !(hasNeg && hasPos);
        }

        private static void DrawStar(Texture2D tex, float cx, float cy, float outerR, float innerR, Color col)
        {
            Vector2[] pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Deg2Rad * (90f + i * 36f);
                float r = (i % 2 == 0) ? outerR : innerR;
                pts[i] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            }
            for (int i = 0; i < 10; i++)
            {
                FillTriangle(tex, new Vector2(cx, cy), pts[i], pts[(i + 1) % 10], col);
            }
        }
    }
}
