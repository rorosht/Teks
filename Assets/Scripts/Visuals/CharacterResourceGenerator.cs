using System.Collections.Generic;
using UnityEngine;
using TeksMaster.Core;

namespace TeksMaster.Visuals
{
    public static class CharacterResourceGenerator
    {
        private static readonly Dictionary<string, Texture2D> _shirtTexCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> _avatarPortraitCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _npcPortraitCache = new Dictionary<string, Sprite>();
        private static Texture2D _faceTex;

        public static Texture2D GetCharacterFaceTexture(Color skinTone)
        {
            if (_faceTex != null) return _faceTex;

            const int w = 128;
            const int h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, skinTone);
                }
            }

            // Soft rosy cheeks & cheerful Pinoy street-kid smile
            FillCircle(tex, 42, 54, 9, new Color(0.92f, 0.56f, 0.52f, 0.35f));
            FillCircle(tex, 86, 54, 9, new Color(0.92f, 0.56f, 0.52f, 0.35f));

            // Eyebrows
            FillRect(tex, 36, 78, 18, 4, new Color(0.10f, 0.08f, 0.07f));
            FillRect(tex, 74, 78, 18, 4, new Color(0.10f, 0.08f, 0.07f));

            // Smile arc
            for (int x = 50; x <= 78; x++)
            {
                float norm = (x - 64f) / 14f;
                int y = Mathf.RoundToInt(40f + norm * norm * 5f);
                FillCircle(tex, x, y, 2, new Color(0.32f, 0.14f, 0.12f));
            }

            tex.Apply();
            _faceTex = tex;
            return tex;
        }

        public static Texture2D GetStreetwearShirtTexture(Color primaryColor, Color accentColor, int jerseyStyleIndex)
        {
            string key = $"{ColorUtility.ToHtmlStringRGB(primaryColor)}_{ColorUtility.ToHtmlStringRGB(accentColor)}_{jerseyStyleIndex}";
            if (_shirtTexCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            const int w = 128;
            const int h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    Color c = Color.Lerp(primaryColor * 0.90f, primaryColor * 1.05f, ny);

                    // Collar V-trim
                    if (y > 108 && Mathf.Abs(nx - 0.5f) < 0.24f)
                    {
                        c = accentColor;
                    }

                    // Bottom hem trim
                    if (y < 8)
                    {
                        c = Color.Lerp(primaryColor, accentColor, 0.65f);
                    }

                    // Style patterns: Basketball jersey stripe, Horizontal street stripes, or Star chest emblem
                    int style = Mathf.Abs(jerseyStyleIndex) % 3;
                    if (style == 0)
                    {
                        // Barangay basketball jersey vertical side stripes & chest band
                        if (nx < 0.12f || nx > 0.88f) c = accentColor;
                        if (y > 68 && y < 86) c = Color.Lerp(c, accentColor, 0.85f);
                    }
                    else if (style == 1)
                    {
                        // Horizontal retro streetwear stripes
                        if ((y / 14) % 2 == 0) c = Color.Lerp(c, accentColor, 0.42f);
                    }
                    else
                    {
                        // Diagonal varsity sash
                        float diag = Mathf.Abs((nx - 0.5f) + (ny - 0.5f) * 0.8f);
                        if (diag < 0.11f) c = accentColor;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            // Center emblem circle on chest
            FillCircle(tex, 64, 76, 15, Color.white);
            FillCircle(tex, 64, 76, 11, accentColor);
            FillCircle(tex, 64, 76, 5, primaryColor);

            tex.Apply();
            _shirtTexCache[key] = tex;
            return tex;
        }

        public static Sprite GetAvatarPortraitSprite(PlayerGender gender)
        {
            string key = gender.ToString();
            if (_avatarPortraitCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color bgTop = gender == PlayerGender.Male
                ? new Color(0.14f, 0.42f, 0.78f)
                : new Color(0.76f, 0.24f, 0.54f);
            Color bgBot = gender == PlayerGender.Male
                ? new Color(0.08f, 0.16f, 0.34f)
                : new Color(0.32f, 0.10f, 0.28f);

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                Color c = Color.Lerp(bgBot, bgTop, ny);
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }

            Color skin = new Color(0.77f, 0.53f, 0.38f);
            Color hair = new Color(0.10f, 0.08f, 0.08f);
            Color shirt = gender == PlayerGender.Male
                ? new Color(0.95f, 0.95f, 0.96f)
                : new Color(0.98f, 0.45f, 0.62f);
            Color trim = gender == PlayerGender.Male
                ? new Color(0.18f, 0.56f, 0.92f)
                : new Color(1.0f, 0.84f, 0.22f);

            // Shoulders & jersey
            FillEllipse(tex, 64, 18, 44, 30, shirt);
            FillEllipse(tex, 64, 34, 18, 12, trim);
            FillRect(tex, 54, 32, 20, 18, skin); // Neck

            // Hair back / bun for Female
            if (gender == PlayerGender.Female)
            {
                FillCircle(tex, 64, 108, 16, hair);
                FillEllipse(tex, 64, 72, 34, 34, hair);
            }

            // Head
            FillEllipse(tex, 64, 72, 28, 32, skin);

            // Hair bangs / tuft
            FillEllipse(tex, 64, 94, 30, 16, hair);
            if (gender == PlayerGender.Female)
            {
                FillRect(tex, 36, 88, 56, 6, new Color(0.96f, 0.36f, 0.72f)); // Headband
            }
            else
            {
                FillRect(tex, 42, 98, 44, 12, hair);
            }

            // Eyes
            FillCircle(tex, 52, 72, 5, Color.white);
            FillCircle(tex, 76, 72, 5, Color.white);
            FillCircle(tex, 53, 72, 3, Color.black);
            FillCircle(tex, 77, 72, 3, Color.black);
            FillCircle(tex, 54, 74, 1, Color.white);
            FillCircle(tex, 78, 74, 1, Color.white);

            // Smile
            for (int x = 54; x <= 74; x++)
            {
                float norm = (x - 64f) / 10f;
                int y = Mathf.RoundToInt(54f + norm * norm * 4f);
                FillCircle(tex, x, y, 2, new Color(0.42f, 0.16f, 0.14f));
            }

            // Gold avatar frame border
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 4 || x >= size - 4 || y < 4 || y >= size - 4)
                    {
                        tex.SetPixel(x, y, new Color(1.0f, 0.84f, 0.24f));
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _avatarPortraitCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetNpcPortraitSprite(string npcName, AIDifficulty difficulty, bool isChampion)
        {
            string normName = string.IsNullOrWhiteSpace(npcName) ? "npc" : npcName.Trim().ToLowerInvariant();
            string key = $"{normName}_{(int)difficulty}_{(isChampion ? 1 : 0)}";
            if (_npcPortraitCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            // Deterministic FNV-1a seed from npcName
            uint hash = 2166136261u;
            for (int i = 0; i < normName.Length; i++)
            {
                hash ^= normName[i];
                hash *= 16777619u;
            }

            Color frameCol;
            Color bgTop;
            Color bgBot;
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    frameCol = new Color(0.30f, 0.86f, 0.48f);
                    bgTop = new Color(0.14f, 0.44f, 0.28f);
                    bgBot = new Color(0.06f, 0.16f, 0.12f);
                    break;
                case AIDifficulty.Hard:
                    frameCol = new Color(0.98f, 0.54f, 0.18f);
                    bgTop = new Color(0.56f, 0.24f, 0.12f);
                    bgBot = new Color(0.20f, 0.08f, 0.06f);
                    break;
                case AIDifficulty.Extreme:
                    frameCol = new Color(0.94f, 0.26f, 0.38f);
                    bgTop = new Color(0.48f, 0.12f, 0.28f);
                    bgBot = new Color(0.16f, 0.05f, 0.14f);
                    break;
                default:
                    frameCol = new Color(0.32f, 0.72f, 0.98f);
                    bgTop = new Color(0.14f, 0.34f, 0.58f);
                    bgBot = new Color(0.07f, 0.13f, 0.24f);
                    break;
            }

            if (isChampion)
            {
                frameCol = new Color(1.0f, 0.84f, 0.22f);
                bgTop = Color.Lerp(bgTop, new Color(0.58f, 0.38f, 0.08f), 0.45f);
            }

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                Color c = Color.Lerp(bgBot, bgTop, ny);
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }

            Color[] skinPalette =
            {
                new Color(0.82f, 0.60f, 0.44f),
                new Color(0.75f, 0.52f, 0.37f),
                new Color(0.68f, 0.46f, 0.32f),
                new Color(0.86f, 0.66f, 0.50f)
            };
            Color[] hairPalette =
            {
                new Color(0.09f, 0.08f, 0.08f),
                new Color(0.18f, 0.12f, 0.09f),
                new Color(0.12f, 0.10f, 0.12f),
                new Color(0.26f, 0.16f, 0.10f)
            };
            Color[] shirtPalette =
            {
                new Color(0.88f, 0.24f, 0.24f),
                new Color(0.20f, 0.56f, 0.88f),
                new Color(0.22f, 0.68f, 0.38f),
                new Color(0.92f, 0.64f, 0.16f),
                new Color(0.56f, 0.28f, 0.78f),
                new Color(0.92f, 0.92f, 0.94f)
            };
            Color[] trimPalette =
            {
                new Color(1.0f, 0.84f, 0.22f),
                new Color(0.96f, 0.96f, 0.98f),
                new Color(0.16f, 0.20f, 0.30f),
                new Color(0.96f, 0.36f, 0.24f)
            };

            Color skin = skinPalette[hash % (uint)skinPalette.Length];
            Color hair = hairPalette[(hash >> 3) % (uint)hairPalette.Length];
            Color shirt = isChampion
                ? new Color(0.16f, 0.14f, 0.22f)
                : shirtPalette[(hash >> 6) % (uint)shirtPalette.Length];
            Color trim = isChampion
                ? new Color(1.0f, 0.84f, 0.22f)
                : trimPalette[(hash >> 9) % (uint)trimPalette.Length];
            int hairStyle = (int)((hash >> 12) % 4u);

            // Shoulders & jersey
            FillEllipse(tex, 64, 18, 44, 30, shirt);
            FillEllipse(tex, 64, 34, 18, 12, trim);
            FillRect(tex, 54, 32, 20, 18, skin);

            // Hair back for longer style
            if (hairStyle == 3)
            {
                FillEllipse(tex, 64, 70, 33, 33, hair);
            }

            // Head
            FillEllipse(tex, 64, 70, 27, 31, skin);

            // Hair styles
            if (hairStyle == 0)
            {
                // Spiky barangay cut
                FillEllipse(tex, 64, 92, 29, 15, hair);
                FillRect(tex, 42, 95, 10, 11, hair);
                FillRect(tex, 58, 97, 12, 13, hair);
                FillRect(tex, 76, 95, 10, 11, hair);
            }
            else if (hairStyle == 1)
            {
                // Side-swept bangs
                FillEllipse(tex, 64, 92, 30, 15, hair);
                FillRect(tex, 38, 84, 24, 12, hair);
            }
            else if (hairStyle == 2)
            {
                // Street cap / bandana headband
                FillEllipse(tex, 64, 92, 29, 15, hair);
                FillRect(tex, 36, 86, 56, 7, trim);
            }
            else
            {
                // Rounded bob / wavy cut
                FillEllipse(tex, 64, 93, 30, 14, hair);
            }

            // Eyebrows (confident angle for Hard/Extreme/Champion)
            Color browCol = new Color(0.10f, 0.08f, 0.07f);
            if (difficulty == AIDifficulty.Hard || difficulty == AIDifficulty.Extreme || isChampion)
            {
                FillRect(tex, 44, 79, 14, 3, browCol);
                FillRect(tex, 70, 79, 14, 3, browCol);
            }
            else
            {
                FillRect(tex, 44, 80, 14, 3, browCol);
                FillRect(tex, 70, 80, 14, 3, browCol);
            }

            // Eyes
            FillCircle(tex, 52, 70, 5, Color.white);
            FillCircle(tex, 76, 70, 5, Color.white);
            FillCircle(tex, 53, 70, 3, Color.black);
            FillCircle(tex, 77, 70, 3, Color.black);
            FillCircle(tex, 54, 72, 1, Color.white);
            FillCircle(tex, 78, 72, 1, Color.white);

            // Confident grin / smirk
            for (int x = 54; x <= 74; x++)
            {
                float norm = (x - 64f) / 10f;
                int y = Mathf.RoundToInt(52f + norm * norm * 3.5f);
                FillCircle(tex, x, y, 2, new Color(0.38f, 0.15f, 0.13f));
            }

            // Golden crown accent for champions
            if (isChampion)
            {
                Color gold = new Color(1.0f, 0.86f, 0.22f);
                Color gem = new Color(0.95f, 0.22f, 0.28f);
                FillRect(tex, 42, 102, 44, 8, gold);
                FillRect(tex, 44, 110, 8, 9, gold);
                FillRect(tex, 59, 110, 10, 12, gold);
                FillRect(tex, 76, 110, 8, 9, gold);
                FillCircle(tex, 64, 106, 3, gem);
            }

            // Colored difficulty / champion frame border
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 4 || x >= size - 4 || y < 4 || y >= size - 4)
                    {
                        tex.SetPixel(x, y, frameCol);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _npcPortraitCache[key] = sprite;
            return sprite;
        }

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color col)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
            if (col.a >= 0.999f) tex.SetPixel(x, y, col);
            else
            {
                Color dst = tex.GetPixel(x, y);
                tex.SetPixel(x, y, Color.Lerp(dst, new Color(col.r, col.g, col.b, 1f), col.a));
            }
        }

        private static void FillRect(Texture2D tex, int x0, int y0, int w, int h, Color col)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                    SetPixelSafe(tex, x, y, col);
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r * r) SetPixelSafe(tex, x, y, col);
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
                    float dx = (x - cx) / Mathf.Max(0.1f, rx);
                    float dy = (y - cy) / Mathf.Max(0.1f, ry);
                    if (dx * dx + dy * dy <= 1f) SetPixelSafe(tex, x, y, col);
                }
            }
        }
    }
}
