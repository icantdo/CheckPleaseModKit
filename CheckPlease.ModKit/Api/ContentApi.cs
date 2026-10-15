using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CheckPlease.ModKit
{
    /// <summary>
    /// Add new foods and upgrades at runtime. Call from <see cref="Game.Ready"/> or later.
    /// Registering an id that already exists returns the existing object (so it is safe to call twice).
    /// Ids are saved as plain strings: if a mod is removed later, its owned upgrades are ignored with a warning and nothing breaks.
    /// </summary>
    public static class ContentApi
    {
        // ---- Foods ----

        /// <summary>Creates a food and registers it in <c>GameConfig.foods</c>.</summary>
        /// <param name="unlockedByDefault">
        /// true: always on the menu. false: only appears after a recipe upgrade with an
        /// <see cref="UpgradeEffects.UnlockFood"/> effect (that upgrade is then sold in the shop), or at <paramref name="unlockStar"/> when no recipe is sold.
        /// </param>
        /// <param name="sprite">Null borrows the first existing food's sprite. See <see cref="SpriteApi"/>.</param>
        public static FoodDefinition AddFood(
            string id, string displayName, int price, FoodType type, BaseIngredient ingredient,
            Sprite sprite = null, float cookTime = 8f, float eatTime = 10f,
            bool unlockedByDefault = true, BaseIngredient? secondIngredient = null,
            string unlockMapId = null, int unlockStar = 1)
        {
            var cfg = RequireConfig();
            var existing = cfg.GetFood(id);
            if (existing != null) { ModKitPlugin.Log.LogWarning($"Food '{id}' already exists; returning existing."); return existing; }

            var f = ScriptableObject.CreateInstance<FoodDefinition>();
            f.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            f.name = "Food_" + id;
            f.id = id;
            f.displayName = displayName;
            f.sprite = sprite != null ? sprite : cfg.foods.Select(x => x?.sprite).FirstOrDefault(s => s != null);
            f.basePrice = price;
            f.cookTime = cookTime;
            f.eatTime = eatTime;
            f.unlockedByDefault = unlockedByDefault;
            f.unlockMapId = unlockMapId;
            f.unlockStar = unlockStar;
            f.foodType = type;
            f.baseIngredient = ingredient;
            f.hasSecondIngredient = secondIngredient.HasValue;
            if (secondIngredient.HasValue) f.secondIngredient = secondIngredient.Value;

            cfg.foods = cfg.foods.Concat(new[] { f }).ToArray();
            InvalidateLookups(cfg);
            ModKitPlugin.Log.LogInfo($"Registered food '{id}'");
            return f;
        }

        // ---- Upgrades ----

        /// <summary>
        /// Creates an upgrade and adds it to a map's shop list.
        /// <see cref="UpgradeCategory.KitchenAndFood"/> upgrades are sold on the chef, other categories on the waiter.
        /// The shop shows at most two buttons per character at a time.
        /// </summary>
        /// <param name="mapId">Map to sell on, or null for every map.</param>
        public static UpgradeDefinition AddUpgrade(
            string mapId, string id, string displayName, string description, int cost,
            UpgradeCategory category, IEnumerable<UpgradeEffect> effects, Sprite icon = null,
            int requiredStars = 1, string prerequisiteId = null)
        {
            var cfg = RequireConfig();
            var existing = cfg.GetUpgrade(id);
            if (existing != null) { ModKitPlugin.Log.LogWarning($"Upgrade '{id}' already exists; returning existing."); return existing; }

            var u = ScriptableObject.CreateInstance<UpgradeDefinition>();
            u.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            u.name = "Upg_" + id;
            u.upgradeId = id;
            u.displayName = displayName;
            u.description = description;
            u.icon = icon != null ? icon : FindAnyUpgradeIcon(cfg);
            u.category = category;
            u.cost = cost;
            u.requiredStars = requiredStars;
            u.prerequisiteId = prerequisiteId;
            u.effects = new List<UpgradeEffect>(effects ?? Enumerable.Empty<UpgradeEffect>());

            int added = 0;
            foreach (var map in cfg.maps)
            {
                if (map == null || (mapId != null && map.mapId != mapId)) continue;
                map.upgrades.Add(u);
                added++;
            }
            if (added == 0) ModKitPlugin.Log.LogWarning($"Upgrade '{id}' was not added to any map (mapId='{mapId}').");

            InvalidateLookups(cfg);
            // Re-apply owned upgrades: a purchase from an earlier session may only now resolve to a definition.
            UpgradeManager.Instance?.ApplyAllPurchased();
            ModKitPlugin.Log.LogInfo($"Registered upgrade '{id}' on {added} map(s)");
            return u;
        }

        private static Sprite FindAnyUpgradeIcon(GameConfig cfg) =>
            cfg.characterUpgrades.Where(x => x != null && x.icon != null).Select(x => x.icon).FirstOrDefault()
            ?? cfg.maps.Where(m => m != null).SelectMany(m => m.upgrades).Where(x => x != null && x.icon != null).Select(x => x.icon).FirstOrDefault();

        // GameConfig builds its id -> definition dictionaries once and never refreshes them.
        private static void InvalidateLookups(GameConfig cfg)
        {
            foreach (var name in new[] { "foodById", "upgradeById", "mapById" })
                AccessTools.Field(typeof(GameConfig), name).SetValue(cfg, null);
        }

        private static GameConfig RequireConfig() =>
            Game.Config ?? throw new InvalidOperationException("Game is not ready yet (use Game.Ready).");
    }

    /// <summary>Shortcuts for building <see cref="UpgradeEffect"/> lists.</summary>
    public static class UpgradeEffects
    {
        /// <summary>final stat = (base + add) * mult, combined with other upgrades.</summary>
        public static UpgradeEffect Stat(StatId stat, float add = 0f, float mult = 1f) =>
            new UpgradeEffect { type = UpgradeEffectType.ModifyStat, stat = stat, addValue = add, multValue = mult };

        /// <summary>Makes a food orderable. The shop shows this upgrade as the food's recipe.</summary>
        public static UpgradeEffect UnlockFood(FoodDefinition food) =>
            new UpgradeEffect { type = UpgradeEffectType.UnlockFood, food = food };

        /// <summary>Turns on a map object (a MapToggleObject id in the map prefab).</summary>
        public static UpgradeEffect EnableMapObject(string toggleId) =>
            new UpgradeEffect { type = UpgradeEffectType.EnableMapObject, mapObjectId = toggleId };
    }

    /// <summary>Loading sprites for custom content.</summary>
    public static class SpriteApi
    {
        /// <summary>
        /// Draws a filled square with a darker border. If <paramref name="sizeLike"/> is given the sprite gets the same
        /// pixel size and pixels-per-unit, so it appears in the game at the same world size. Handy as a placeholder.
        /// </summary>
        public static Sprite Square(Color color, Sprite sizeLike = null)
        {
            int w = sizeLike != null ? Mathf.Max(16, (int)sizeLike.rect.width) : 128;
            int h = sizeLike != null ? Mathf.Max(16, (int)sizeLike.rect.height) : 128;
            float ppu = sizeLike != null ? sizeLike.pixelsPerUnit : 100f;
            int side = Mathf.Min(w, h);
            int x0 = (w - side) / 2 + side / 8, x1 = (w + side) / 2 - side / 8;
            int y0 = (h - side) / 2 + side / 8, y1 = (h + side) / 2 - side / 8;
            int border = Mathf.Max(2, side / 24);
            var border32 = (Color32)Color.Lerp(color, Color.black, 0.45f);
            var fill32 = (Color32)color;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (x < x0 || x >= x1 || y < y0 || y >= y1) continue; // transparent
                    bool edge = x < x0 + border || x >= x1 - border || y < y0 + border || y >= y1 - border;
                    pixels[y * w + x] = edge ? border32 : fill32;
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "ModKit_Square" };
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
            s.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return s;
        }

        // UnityEngine.ImageConversionModule targets netstandard 2.1, which this net472 project cannot reference, so call it by reflection.
        private static readonly System.Reflection.MethodInfo LoadImageMethod =
            Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });

        private static bool LoadImage(Texture2D tex, byte[] data)
        {
            if (LoadImageMethod == null) throw new InvalidOperationException("UnityEngine.ImageConversion.LoadImage not found.");
            return (bool)LoadImageMethod.Invoke(null, new object[] { tex, data });
        }

        /// <summary>Loads a PNG file as a sprite (centered pivot). Returns null and logs if the file is missing or invalid.</summary>
        public static Sprite FromPng(string path, float pixelsPerUnit = 100f, Sprite fitTo = null)
        {
            try
            {
                if (!File.Exists(path)) { ModKitPlugin.Log.LogWarning($"Sprite file not found: {path}"); return null; }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileNameWithoutExtension(path) };
                if (!LoadImage(tex, File.ReadAllBytes(path))) { ModKitPlugin.Log.LogWarning($"Not a valid image: {path}"); return null; }
                tex.filterMode = FilterMode.Bilinear;
                tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                if (fitTo != null) pixelsPerUnit = Mathf.Max(tex.width, tex.height) / (Mathf.Max(fitTo.rect.width, fitTo.rect.height) / fitTo.pixelsPerUnit); // same world size as fitTo
                var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                s.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return s;
            }
            catch (Exception e)
            {
                ModKitPlugin.Log.LogError($"Failed to load sprite '{path}': {e}");
                return null;
            }
        }
    }
}
