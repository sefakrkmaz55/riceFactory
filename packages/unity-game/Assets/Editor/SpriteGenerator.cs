// =============================================================================
// SpriteGenerator.cs
// ART_GUIDE.md renk paletine uygun programatik sprite ureticisi.
// Batch mode: -executeMethod RiceFactory.Editor.SpriteGenerator.SetupFromCommandLine
//
// Flat/Cartoon 2D stil: Basit geometrik sekillerle piksel bazli cizim.
// Yuvarlak koseler, duz renkler, minimal detay.
// =============================================================================

#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace RiceFactory.Editor
{
    public static class SpriteGenerator
    {
        private const string TAG = "[SpriteGenerator]";
        private const string SPRITE_DIR = "Assets/Sprites";
        private const string UI_DIR = "Assets/Sprites/UI";
        private const string FACTORY_DIR = "Assets/Sprites/Factories";
        private const string HUD_DIR = "Assets/Sprites/HUD";

        // =================================================================
        // ART_GUIDE.md Renk Paleti
        // =================================================================

        // Ana renkler
        private static readonly Color PRIMARY_GREEN = HexColor("#4CAF50");
        private static readonly Color GOLD_YELLOW = HexColor("#FFD54F");
        private static readonly Color ACCENT_ORANGE = HexColor("#FF7043");
        private static readonly Color BG_CREAM = HexColor("#FFF8E1");
        private static readonly Color TEXT_DARK = HexColor("#3E2723");
        private static readonly Color SUCCESS_GREEN = HexColor("#66BB6A");
        private static readonly Color WARNING_RED = HexColor("#EF5350");

        // Tesis renkleri
        private static readonly Color FIELD_GREEN = HexColor("#7CB342");
        private static readonly Color FIELD_WATER = HexColor("#81D4FA");
        private static readonly Color FACTORY_GRAY = HexColor("#78909C");
        private static readonly Color FACTORY_ORANGE = HexColor("#FFA726");
        private static readonly Color BAKERY_RED = HexColor("#D84315");
        private static readonly Color BAKERY_CREAM = HexColor("#FFCC80");
        private static readonly Color RESTAURANT_BORDO = HexColor("#AD1457");
        private static readonly Color RESTAURANT_CHAMPAGNE = HexColor("#FFE0B2");
        private static readonly Color MARKET_BLUE = HexColor("#1E88E5");
        private static readonly Color MARKET_LIGHT_GREEN = HexColor("#A5D6A7");
        private static readonly Color GLOBAL_NAVY = HexColor("#1A237E");
        private static readonly Color GLOBAL_GOLD = HexColor("#FFD700");

        // UI renkleri
        private static readonly Color BTN_GREEN = HexColor("#4CAF50");
        private static readonly Color BTN_ORANGE = HexColor("#FF9800");
        private static readonly Color BTN_RED = HexColor("#F44336");
        private static readonly Color BTN_BLUE = HexColor("#2196F3");
        private static readonly Color BTN_GRAY = HexColor("#9E9E9E");
        private static readonly Color PANEL_DARK = HexColor("#1A1A2E", 0.90f);
        private static readonly Color PANEL_LIGHT = HexColor("#FFFFFF", 0.85f);
        private static readonly Color PANEL_CARD = HexColor("#2D2D44");
        private static readonly Color HUD_BG = HexColor("#1A1A2E", 0.95f);

        // =================================================================
        // Giris Noktalari
        // =================================================================

        [MenuItem("RiceFactory/Generate Sprites")]
        public static void GenerateAll()
        {
            Debug.Log($"{TAG} Sprite uretimi basliyor...");

            EnsureDirectory(SPRITE_DIR);
            EnsureDirectory(UI_DIR);
            EnsureDirectory(FACTORY_DIR);
            EnsureDirectory(HUD_DIR);

            GenerateFactoryIcons();
            GenerateUIElements();
            GenerateHUDBackgrounds();

            AssetDatabase.Refresh();
            Debug.Log($"{TAG} Tum sprite'lar basariyla uretildi.");
        }

        /// <summary>
        /// Batch mode giris noktasi.
        /// Unity -batchmode -executeMethod RiceFactory.Editor.SpriteGenerator.SetupFromCommandLine -quit
        /// </summary>
        public static void SetupFromCommandLine()
        {
            Debug.Log($"{TAG} Batch mode sprite uretimi basliyor...");
            GenerateAll();
            Debug.Log($"{TAG} Batch mode sprite uretimi tamamlandi.");
        }

        // =================================================================
        // Fabrika Ikonlari (128x128)
        // =================================================================

        private static void GenerateFactoryIcons()
        {
            Debug.Log($"{TAG} Fabrika ikonlari uretiliyor...");

            GenerateRiceFieldIcon();
            GenerateRiceFactoryIcon();
            GenerateBakeryIcon();
            GenerateRestaurantIcon();
            GenerateMarketIcon();
            GenerateGlobalIcon();
        }

        /// <summary>Pirinc tarlasi: yesil dikdortgen zemin + sarı pirinc taneleri.</summary>
        private static void GenerateRiceFieldIcon()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Yuvarlak koseli yesil arka plan
            FillRoundedRect(tex, 4, 4, 120, 120, FIELD_GREEN, 16);

            // Su cizgileri (alt %30)
            var waterColor = new Color(FIELD_WATER.r, FIELD_WATER.g, FIELD_WATER.b, 0.4f);
            FillRect(tex, 8, 8, 112, 35, waterColor);

            // Pirinc saplari (dikey yesil cizgiler)
            var darkGreen = new Color(0.3f, 0.55f, 0.15f, 1f);
            for (int i = 0; i < 6; i++)
            {
                int x = 20 + i * 16;
                FillRect(tex, x, 30, 3, 55, darkGreen);
            }

            // Pirinc taneleri (kucuk sari ovaller sapların ustunde)
            var grainColor = GOLD_YELLOW;
            for (int i = 0; i < 6; i++)
            {
                int cx = 21 + i * 16;
                int cy = 85 + (i % 2 == 0 ? 5 : 0);
                FillCircle(tex, cx, cy, 4, grainColor);
                FillCircle(tex, cx + 3, cy + 6, 3, grainColor);
                FillCircle(tex, cx - 2, cy + 4, 3, grainColor);
            }

            SaveTexture(tex, FACTORY_DIR, "factory_rice_field.png");
        }

        /// <summary>Fabrika binasi: gri dikdortgen + baca.</summary>
        private static void GenerateRiceFactoryIcon()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Yuvarlak koseli gri arka plan
            FillRoundedRect(tex, 4, 4, 120, 120, FACTORY_GRAY, 16);

            // Ana bina govdesi (koyu gri dikdortgen)
            var buildingColor = new Color(0.35f, 0.42f, 0.47f, 1f);
            FillRect(tex, 20, 16, 88, 60, buildingColor);

            // Cati (ucgen simgeleme — duz cati)
            FillRect(tex, 16, 76, 96, 10, FACTORY_ORANGE);

            // Baca
            FillRect(tex, 80, 86, 16, 28, buildingColor);
            // Duman (kucuk daireler)
            var smokeColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            FillCircle(tex, 88, 118, 5, smokeColor);
            FillCircle(tex, 84, 122, 4, smokeColor);

            // Kapi
            FillRect(tex, 52, 16, 24, 32, FACTORY_ORANGE);

            // Pencereler
            var windowColor = FIELD_WATER;
            FillRect(tex, 26, 44, 16, 16, windowColor);
            FillRect(tex, 86, 44, 16, 16, windowColor);

            SaveTexture(tex, FACTORY_DIR, "factory_rice_factory.png");
        }

        /// <summary>Firin: turuncu/kahve kubbe + ekmek.</summary>
        private static void GenerateBakeryIcon()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Yuvarlak koseli tuğla kırmızı arka plan
            FillRoundedRect(tex, 4, 4, 120, 120, BAKERY_RED, 16);

            // Kubbe (buyuk yari daire)
            FillCircle(tex, 64, 60, 40, BAKERY_CREAM);

            // Firin agzi (koyu dikdortgen)
            var ovenColor = new Color(0.2f, 0.1f, 0.05f, 1f);
            FillRect(tex, 48, 20, 32, 24, ovenColor);

            // Ici turuncu (ates)
            var fireColor = ACCENT_ORANGE;
            FillRect(tex, 52, 24, 24, 16, fireColor);

            // Ekmek (kucuk oval, ustte)
            var breadColor = new Color(0.85f, 0.65f, 0.3f, 1f);
            FillCircle(tex, 44, 92, 10, breadColor);
            FillCircle(tex, 84, 92, 10, breadColor);
            FillCircle(tex, 64, 98, 12, breadColor);

            SaveTexture(tex, FACTORY_DIR, "factory_bakery.png");
        }

        /// <summary>Restoran: kirmizi cati + tabak ikonu.</summary>
        private static void GenerateRestaurantIcon()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Yuvarlak koseli bordo arka plan
            FillRoundedRect(tex, 4, 4, 120, 120, RESTAURANT_BORDO, 16);

            // Bina govdesi (sampanya rengi)
            FillRect(tex, 20, 12, 88, 56, RESTAURANT_CHAMPAGNE);

            // Kirmizi cati (ucgen benzeri)
            for (int row = 0; row < 20; row++)
            {
                int halfWidth = 48 + row;
                if (halfWidth > 56) halfWidth = 56;
                int startX = 64 - halfWidth;
                int endX = 64 + halfWidth;
                FillRect(tex, startX, 68 + row, endX - startX, 1, WARNING_RED);
            }

            // Tabak (beyaz daire, ortada alt kisim)
            FillCircle(tex, 64, 38, 18, Color.white);
            FillCircle(tex, 64, 38, 12, RESTAURANT_CHAMPAGNE);
            // Tabakta yemek (kucuk renkli daireler)
            FillCircle(tex, 60, 40, 5, ACCENT_ORANGE);
            FillCircle(tex, 68, 38, 4, PRIMARY_GREEN);
            FillCircle(tex, 64, 34, 4, GOLD_YELLOW);

            SaveTexture(tex, FACTORY_DIR, "factory_restaurant.png");
        }

        /// <summary>Market: mavi dikdortgen + market isareti.</summary>
        private static void GenerateMarketIcon()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Yuvarlak koseli mavi arka plan
            FillRoundedRect(tex, 4, 4, 120, 120, MARKET_BLUE, 16);

            // Market binasi (beyaz dikdortgen)
            FillRect(tex, 20, 12, 88, 64, Color.white);

            // Tabela (yesil serit ust kisim)
            FillRect(tex, 20, 72, 88, 14, MARKET_LIGHT_GREEN);

            // Kapi (ortada)
            FillRect(tex, 52, 12, 24, 36, MARKET_BLUE);

            // Pencereler
            FillRect(tex, 26, 40, 20, 20, FIELD_WATER);
            FillRect(tex, 82, 40, 20, 20, FIELD_WATER);

            // Alisveris sepeti ikonu (kucuk, sag ust)
            FillRect(tex, 86, 92, 20, 16, Color.white);
            FillCircle(tex, 90, 88, 3, Color.white);
            FillCircle(tex, 102, 88, 3, Color.white);

            SaveTexture(tex, FACTORY_DIR, "factory_market.png");
        }

        /// <summary>Dunya dagitim: mavi daire + ok isaretleri.</summary>
        private static void GenerateGlobalIcon()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Yuvarlak koseli lacivert arka plan
            FillRoundedRect(tex, 4, 4, 120, 120, GLOBAL_NAVY, 16);

            // Dunya (buyuk mavi daire)
            var globeColor = new Color(0.2f, 0.5f, 0.85f, 1f);
            FillCircle(tex, 64, 64, 36, globeColor);

            // Kita benzeri yesil alanlar
            var landColor = new Color(0.3f, 0.7f, 0.3f, 1f);
            FillCircle(tex, 52, 74, 12, landColor);
            FillCircle(tex, 76, 68, 10, landColor);
            FillCircle(tex, 58, 50, 8, landColor);
            FillCircle(tex, 72, 54, 6, landColor);

            // Ok isaretleri (altin renkli cizgiler — saga ve sola)
            // Sag ok
            FillRect(tex, 100, 62, 16, 4, GLOBAL_GOLD);
            FillRect(tex, 112, 58, 4, 12, GLOBAL_GOLD);
            // Sol ok
            FillRect(tex, 12, 62, 16, 4, GLOBAL_GOLD);
            FillRect(tex, 12, 58, 4, 12, GLOBAL_GOLD);
            // Ust ok
            FillRect(tex, 62, 102, 4, 14, GLOBAL_GOLD);
            FillRect(tex, 58, 112, 12, 4, GLOBAL_GOLD);

            SaveTexture(tex, FACTORY_DIR, "factory_global.png");
        }

        // =================================================================
        // UI Elemanlari
        // =================================================================

        private static void GenerateUIElements()
        {
            Debug.Log($"{TAG} UI elemanlari uretiliyor...");

            // Butonlar (300x80)
            GenerateButton("btn_green.png", 300, 80, BTN_GREEN, 16);
            GenerateButton("btn_orange.png", 300, 80, BTN_ORANGE, 16);
            GenerateButton("btn_red.png", 300, 80, BTN_RED, 16);
            GenerateButton("btn_blue.png", 300, 80, BTN_BLUE, 16);
            GenerateButton("btn_gray.png", 300, 80, BTN_GRAY, 16);

            // Paneller
            GeneratePanel("panel_dark.png", 512, 512, PANEL_DARK, 24);
            GeneratePanel("panel_light.png", 512, 512, PANEL_LIGHT, 24);
            GeneratePanel("panel_card.png", 400, 200, PANEL_CARD, 16);

            // Ikonlar
            GenerateCoinIcon();
            GenerateGemIcon();
            GenerateStarFilled();
            GenerateStarEmpty();
            GenerateLockIcon();
            GenerateSettingsIcon();

            // Progress barlar
            GenerateProgressBar("progress_bg.png", 300, 24, HexColor("#424242"), 12);
            GenerateProgressBar("progress_fill.png", 300, 24, PRIMARY_GREEN, 12, true);
            GenerateProgressBar("progress_fill_gold.png", 300, 24, GLOBAL_GOLD, 12, true);
        }

        /// <summary>Yuvarlak koseli buton sprite.</summary>
        private static void GenerateButton(string fileName, int w, int h, Color color, int radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            FillRoundedRect(tex, 0, 0, w, h, color, radius);

            // Ust kenarda hafif parlaklik (highlight)
            var highlight = new Color(1f, 1f, 1f, 0.15f);
            FillRoundedRect(tex, 2, h / 2, w - 4, h / 2 - 2, highlight, radius);

            SaveTexture(tex, UI_DIR, fileName);
        }

        /// <summary>Yuvarlak koseli panel sprite.</summary>
        private static void GeneratePanel(string fileName, int w, int h, Color color, int radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            FillRoundedRect(tex, 0, 0, w, h, color, radius);

            // Ince kenarlık (1px, hafif beyaz)
            var borderColor = new Color(1f, 1f, 1f, 0.1f);
            DrawRoundedRectBorder(tex, 0, 0, w, h, borderColor, radius, 2);

            SaveTexture(tex, UI_DIR, fileName);
        }

        /// <summary>Altin para ikonu (64x64): sari daire + $ isareti.</summary>
        private static void GenerateCoinIcon()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Dis halka (koyu altin)
            var darkGold = new Color(0.8f, 0.65f, 0.1f, 1f);
            FillCircle(tex, 32, 32, 28, darkGold);

            // Ic daire (parlak altin)
            FillCircle(tex, 32, 32, 24, GOLD_YELLOW);

            // $ isareti (piksel bazli)
            var symbolColor = new Color(0.6f, 0.45f, 0f, 1f);
            // Dikey cizgi
            FillRect(tex, 30, 16, 4, 32, symbolColor);
            // Ust yatay
            FillRect(tex, 22, 40, 20, 4, symbolColor);
            // Orta yatay
            FillRect(tex, 22, 30, 20, 4, symbolColor);
            // Alt yatay
            FillRect(tex, 22, 20, 20, 4, symbolColor);
            // Ust sag parca
            FillRect(tex, 38, 34, 4, 10, symbolColor);
            // Alt sol parca
            FillRect(tex, 22, 20, 4, 14, symbolColor);

            SaveTexture(tex, UI_DIR, "icon_coin.png");
        }

        /// <summary>Elmas ikonu (64x64): mavi elmas sekli.</summary>
        private static void GenerateGemIcon()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            var gemBlue = HexColor("#2196F3");
            var gemLight = HexColor("#64B5F6");
            var gemDark = HexColor("#1565C0");

            // Elmas sekli: ust ucgen + alt ucgen
            int cx = 32, topY = 52, midY = 36, botY = 10;
            int halfTop = 16, halfMid = 24;

            for (int y = midY; y <= topY; y++)
            {
                float t = (float)(y - midY) / (topY - midY);
                int hw = (int)Mathf.Lerp(halfMid, halfTop, t);
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        // Sol taraf koyu, sag taraf acik
                        Color c = x < cx ? gemDark : gemLight;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            for (int y = botY; y < midY; y++)
            {
                float t = (float)(y - botY) / (midY - botY);
                int hw = (int)Mathf.Lerp(0, halfMid, t);
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        Color c = x < cx ? gemDark : gemBlue;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Orta yatay parlama cizgisi
            FillRect(tex, cx - halfMid, midY, halfMid * 2, 2, gemLight);

            tex.Apply();
            SaveTextureRaw(tex, UI_DIR, "icon_gem.png");
        }

        /// <summary>Dolu yildiz (48x48): sari.</summary>
        private static void GenerateStarFilled()
        {
            int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            DrawStar(tex, 24, 24, 20, 9, GOLD_YELLOW);

            SaveTexture(tex, UI_DIR, "icon_star_filled.png");
        }

        /// <summary>Bos yildiz (48x48): gri outline.</summary>
        private static void GenerateStarEmpty()
        {
            int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            var grayOutline = HexColor("#9E9E9E");
            DrawStar(tex, 24, 24, 20, 9, grayOutline);
            // Ici temizle (biraz kucuk yildiz sil)
            DrawStar(tex, 24, 24, 16, 7, Color.clear);

            SaveTexture(tex, UI_DIR, "icon_star_empty.png");
        }

        /// <summary>Kilit ikonu (64x64): gri.</summary>
        private static void GenerateLockIcon()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            var lockGray = HexColor("#757575");
            var lockDark = HexColor("#424242");

            // Govde (yuvarlak koseli dikdortgen)
            FillRoundedRect(tex, 16, 8, 32, 28, lockGray, 6);

            // Halka (ust kisim, yari daire)
            for (int angle = 0; angle < 180; angle++)
            {
                float rad = angle * Mathf.Deg2Rad;
                for (int r = 10; r <= 14; r++)
                {
                    int px = 32 + (int)(Mathf.Cos(rad) * r);
                    int py = 36 + (int)(Mathf.Sin(rad) * r);
                    if (px >= 0 && px < size && py >= 0 && py < size)
                        tex.SetPixel(px, py, lockDark);
                }
            }

            // Anahtar deligi (kucuk daire + dikdortgen)
            FillCircle(tex, 32, 24, 4, lockDark);
            FillRect(tex, 30, 12, 4, 10, lockDark);

            tex.Apply();
            SaveTextureRaw(tex, UI_DIR, "icon_lock.png");
        }

        /// <summary>Disli cark ikonu (64x64).</summary>
        private static void GenerateSettingsIcon()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            var gearColor = HexColor("#9E9E9E");
            int cx = 32, cy = 32;

            // Dis daire (disli dis capi)
            FillCircle(tex, cx, cy, 22, gearColor);

            // Disli disleri (8 adet dikdortgen, etrafta)
            int toothCount = 8;
            for (int i = 0; i < toothCount; i++)
            {
                float angle = (i * 360f / toothCount) * Mathf.Deg2Rad;
                int tx = cx + (int)(Mathf.Cos(angle) * 24);
                int ty = cy + (int)(Mathf.Sin(angle) * 24);
                FillCircle(tex, tx, ty, 5, gearColor);
            }

            // Ic bosluk (koyu daire)
            FillCircle(tex, cx, cy, 10, Color.clear);

            // Ortaya kucuk nokta
            var darkGear = HexColor("#616161");
            FillCircle(tex, cx, cy, 5, darkGear);

            SaveTexture(tex, UI_DIR, "icon_settings.png");
        }

        /// <summary>Progress bar sprite (yuvarlak uclu).</summary>
        private static void GenerateProgressBar(string fileName, int w, int h, Color color, int radius,
            bool gradient = false)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            if (gradient)
            {
                // Gradient: alttan uste acilma
                for (int y = 0; y < h; y++)
                {
                    float t = (float)y / h;
                    var rowColor = Color.Lerp(
                        new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, color.a),
                        new Color(Mathf.Min(color.r * 1.2f, 1f), Mathf.Min(color.g * 1.2f, 1f),
                                  Mathf.Min(color.b * 1.2f, 1f), color.a),
                        t);
                    for (int x = 0; x < w; x++)
                    {
                        if (IsInsideRoundedRect(x, y, 0, 0, w, h, radius))
                            tex.SetPixel(x, y, rowColor);
                    }
                }
                tex.Apply();
                SaveTextureRaw(tex, UI_DIR, fileName);
            }
            else
            {
                FillRoundedRect(tex, 0, 0, w, h, color, radius);
                SaveTexture(tex, UI_DIR, fileName);
            }
        }

        // =================================================================
        // HUD Arka Planlari
        // =================================================================

        private static void GenerateHUDBackgrounds()
        {
            Debug.Log($"{TAG} HUD arka planlari uretiliyor...");

            GenerateTopBar();
            GenerateBottomBar();
        }

        /// <summary>Ust bar (1080x120): koyu panel, %95 opak, alt kenar yumusak.</summary>
        private static void GenerateTopBar()
        {
            int w = 1080, h = 120;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            // Ana dolgu
            for (int y = 0; y < h; y++)
            {
                float alpha = HUD_BG.a;
                // Alt kenar yumusak gecis (son 20 piksel)
                if (y < 20)
                    alpha *= (float)y / 20f;

                var c = new Color(HUD_BG.r, HUD_BG.g, HUD_BG.b, alpha);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }

            tex.Apply();
            SaveTextureRaw(tex, HUD_DIR, "hud_top_bar.png");
        }

        /// <summary>Alt bar (1080x200): koyu panel, %95 opak, ust kenar yumusak.</summary>
        private static void GenerateBottomBar()
        {
            int w = 1080, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Clear(tex, Color.clear);

            for (int y = 0; y < h; y++)
            {
                float alpha = HUD_BG.a;
                // Ust kenar yumusak gecis (ust 30 piksel)
                if (y > h - 30)
                    alpha *= (float)(h - y) / 30f;

                // NOT: Unity texture koordinatlarinda y=0 alt, y=h-1 ust
                // Dolayisiyla ust kenar = buyuk y degerleri
                var c = new Color(HUD_BG.r, HUD_BG.g, HUD_BG.b, alpha);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }

            tex.Apply();
            SaveTextureRaw(tex, HUD_DIR, "hud_bottom_bar.png");
        }

        // =================================================================
        // Cizim Yardimcilari
        // =================================================================

        /// <summary>Texture'u verilen renkle temizler.</summary>
        private static void Clear(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
        }

        /// <summary>Duz dikdortgen doldurur.</summary>
        private static void FillRect(Texture2D tex, int startX, int startY, int width, int height, Color color)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        if (color.a < 1f && color.a > 0f)
                        {
                            // Alpha blending
                            var existing = tex.GetPixel(x, y);
                            var blended = Color.Lerp(existing, color, color.a);
                            blended.a = Mathf.Max(existing.a, color.a);
                            tex.SetPixel(x, y, blended);
                        }
                        else
                        {
                            tex.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }

        /// <summary>Dolu daire cizer.</summary>
        private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                    {
                        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                            tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>Piksel yuvarlak koseli dikdortgenin icinde mi?</summary>
        private static bool IsInsideRoundedRect(int px, int py, int rx, int ry, int rw, int rh, int radius)
        {
            // Kose bolgelerini kontrol et
            // Sol alt
            if (px < rx + radius && py < ry + radius)
                return Vector2.Distance(new Vector2(px, py), new Vector2(rx + radius, ry + radius)) <= radius;
            // Sag alt
            if (px > rx + rw - radius - 1 && py < ry + radius)
                return Vector2.Distance(new Vector2(px, py), new Vector2(rx + rw - radius - 1, ry + radius)) <= radius;
            // Sol ust
            if (px < rx + radius && py > ry + rh - radius - 1)
                return Vector2.Distance(new Vector2(px, py), new Vector2(rx + radius, ry + rh - radius - 1)) <= radius;
            // Sag ust
            if (px > rx + rw - radius - 1 && py > ry + rh - radius - 1)
                return Vector2.Distance(new Vector2(px, py), new Vector2(rx + rw - radius - 1, ry + rh - radius - 1)) <= radius;

            // Normal dikdortgen alani
            return px >= rx && px < rx + rw && py >= ry && py < ry + rh;
        }

        /// <summary>Yuvarlak koseli dikdortgen doldurur.</summary>
        private static void FillRoundedRect(Texture2D tex, int rx, int ry, int rw, int rh,
            Color color, int radius)
        {
            for (int x = rx; x < rx + rw; x++)
            {
                for (int y = ry; y < ry + rh; y++)
                {
                    if (IsInsideRoundedRect(x, y, rx, ry, rw, rh, radius))
                    {
                        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                        {
                            if (color.a < 1f && color.a > 0f)
                            {
                                var existing = tex.GetPixel(x, y);
                                var blended = Color.Lerp(existing, color, color.a);
                                blended.a = Mathf.Max(existing.a, color.a);
                                tex.SetPixel(x, y, blended);
                            }
                            else
                            {
                                tex.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Yuvarlak koseli dikdortgen kenarligi cizer.</summary>
        private static void DrawRoundedRectBorder(Texture2D tex, int rx, int ry, int rw, int rh,
            Color color, int radius, int thickness)
        {
            for (int x = rx; x < rx + rw; x++)
            {
                for (int y = ry; y < ry + rh; y++)
                {
                    bool outsideInner = !IsInsideRoundedRect(x, y, rx + thickness, ry + thickness,
                        rw - thickness * 2, rh - thickness * 2, Mathf.Max(0, radius - thickness));
                    bool insideOuter = IsInsideRoundedRect(x, y, rx, ry, rw, rh, radius);

                    if (insideOuter && outsideInner)
                    {
                        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                            tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>5 koseli yildiz cizer (dolu).</summary>
        private static void DrawStar(Texture2D tex, int cx, int cy, int outerR, int innerR, Color color)
        {
            // 5 koseli yildiz noktalari hesapla
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (i * 36f - 90f) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? outerR : innerR;
                points[i] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            }

            // Scanline fill
            int minY = Mathf.Max(0, cy - outerR - 1);
            int maxY = Mathf.Min(tex.height - 1, cy + outerR + 1);
            int minX = Mathf.Max(0, cx - outerR - 1);
            int maxX = Mathf.Min(tex.width - 1, cx + outerR + 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (PointInPolygon(x, y, points))
                        tex.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>Point-in-polygon testi (ray casting).</summary>
        private static bool PointInPolygon(float px, float py, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i].y > py) != (polygon[j].y > py) &&
                    px < (polygon[j].x - polygon[i].x) * (py - polygon[i].y) /
                         (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        // =================================================================
        // Dosya I/O
        // =================================================================

        /// <summary>Dizin yoksa olusturur.</summary>
        private static void EnsureDirectory(string assetPath)
        {
            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        /// <summary>Texture'u Apply() yapip PNG olarak kaydeder.</summary>
        private static void SaveTexture(Texture2D tex, string directory, string fileName)
        {
            tex.Apply();
            SaveTextureRaw(tex, directory, fileName);
        }

        /// <summary>Texture'u PNG olarak kaydeder (Apply zaten yapilmis varsayar).</summary>
        private static void SaveTextureRaw(Texture2D tex, string directory, string fileName)
        {
            var bytes = tex.EncodeToPNG();
            // directory: "Assets/Sprites/UI" gibi
            var dirFullPath = Path.Combine(Application.dataPath, "..", directory);
            if (!Directory.Exists(dirFullPath))
                Directory.CreateDirectory(dirFullPath);

            var fullPath = Path.Combine(dirFullPath, fileName);
            File.WriteAllBytes(fullPath, bytes);
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"{TAG} Sprite kaydedildi: {directory}/{fileName}");
        }

        /// <summary>HEX string'den Color olusturur.</summary>
        private static Color HexColor(string hex, float alphaOverride = -1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            if (alphaOverride >= 0f)
                color.a = alphaOverride;
            return color;
        }
    }
}

#endif
