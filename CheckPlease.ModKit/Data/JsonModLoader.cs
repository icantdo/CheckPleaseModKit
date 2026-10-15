using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CheckPlease.ModKit
{
    /// <summary>
    /// Loads data-only mods: every <c>mod.json</c> found under <c>BepInEx/plugins</c>.
    /// See docs/MODDING_JSON.md for the format. One bad entry is reported (mod name + path) and skipped; the rest still load.
    /// </summary>
    internal static class JsonModLoader
    {
        private sealed class Ctx
        {
            public string Id, Name, Dir;
            public int Errors;
            public void Error(string path, string msg)
            {
                Errors++;
                ModKitPlugin.Log.LogError($"[{Name}] {path}: {msg}");
            }
        }

        public static void LoadAll()
        {
            string[] files;
            try { files = Directory.GetFiles(Paths.PluginPath, "mod.json", SearchOption.AllDirectories); }
            catch (Exception e) { ModKitPlugin.Log.LogError($"Could not scan for mod.json: {e.Message}"); return; }

            foreach (var f in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                try { LoadOne(f); }
                catch (Exception e) { ModKitPlugin.Log.LogError($"Unexpected error loading {f}: {e}"); }
            }
        }

        private static void LoadOne(string file)
        {
            string dir = Path.GetDirectoryName(file);
            JObject root;
            try { root = JObject.Parse(File.ReadAllText(file)); }
            catch (Exception e) { ModKitPlugin.Log.LogError($"[{dir}] mod.json is not valid JSON: {e.Message}"); return; }

            var ctx = new Ctx
            {
                Dir = dir,
                Id = root.Value<string>("id") ?? Path.GetFileName(dir),
            };
            ctx.Name = root.Value<string>("name") ?? ctx.Id;

            if (root.Value<bool?>("enabled") == false)
            {
                ModKitPlugin.Log.LogInfo($"[{ctx.Name}] disabled in mod.json, skipping.");
                return;
            }

            int foods = 0, upgrades = 0, stats = 0, balance = 0;

            // Order matters: balance and stats first, foods before upgrades (an upgrade may unlock a food from the same mod).
            if (root["balance"] is JObject bal) balance = LoadBalance(ctx, bal);
            if (root["stats"] is JArray st) stats = Each(ctx, "stats", st, LoadStat);
            if (root["foods"] is JArray fo) foods = Each(ctx, "foods", fo, LoadFood);
            if (root["upgrades"] is JArray up) upgrades = Each(ctx, "upgrades", up, LoadUpgrade);

            foreach (var key in root.Properties().Select(p => p.Name).Except(KnownKeys))
                ctx.Error(key, "unknown top-level key (ignored). Valid keys: " + string.Join(", ", KnownKeys));

            ModKitPlugin.Log.LogInfo(
                $"[{ctx.Name}] loaded: {foods} food(s), {upgrades} upgrade(s), {stats} stat modifier(s), {balance} balance value(s)" +
                (ctx.Errors > 0 ? $", {ctx.Errors} problem(s) (see above)" : ""));
        }

        private static readonly string[] KnownKeys = { "id", "name", "version", "author", "description", "enabled", "balance", "stats", "foods", "upgrades" };

        private static int Each(Ctx ctx, string section, JArray items, Action<Ctx, JObject, string> load)
        {
            int ok = 0;
            for (int i = 0; i < items.Count; i++)
            {
                string path = $"{section}[{i}]";
                if (!(items[i] is JObject o)) { ctx.Error(path, "must be an object { ... }"); continue; }
                int before = ctx.Errors;
                try { load(ctx, o, path); }
                catch (Exception e) { ctx.Error(path, e.Message); }
                if (ctx.Errors == before) ok++;
            }
            return ok;
        }

        // ---------- balance ----------

        private static int LoadBalance(Ctx ctx, JObject bal)
        {
            int n = 0;
            foreach (var p in bal.Properties())
            {
                string path = "balance." + p.Name;
                try
                {
                    if (p.Value.Type == JTokenType.Object)
                    {
                        var o = (JObject)p.Value;
                        var scale = o["scale"];
                        if (scale == null) { ctx.Error(path, "object form must be { \"scale\": number }"); continue; }
                        BalanceApi.Scale(p.Name, scale.Value<float>());
                    }
                    else if (p.Value.Type == JTokenType.Boolean) BalanceApi.Set(p.Name, p.Value.Value<bool>());
                    else if (p.Value.Type == JTokenType.Integer || p.Value.Type == JTokenType.Float) BalanceApi.Set(p.Name, p.Value.Value<double>());
                    else { ctx.Error(path, "value must be a number, true/false, or { \"scale\": number }"); continue; }
                    n++;
                }
                catch (ArgumentException)
                {
                    ctx.Error(path, $"no such balance setting. Check spelling; see the list logged at startup (BalanceApi.Names).");
                }
                catch (Exception e) { ctx.Error(path, e.Message); }
            }
            return n;
        }

        // ---------- stats ----------

        private static void LoadStat(Ctx ctx, JObject o, string path)
        {
            var stat = ParseEnum<StatId>(ctx, o, "stat", path, required: true);
            float add = o.Value<float?>("add") ?? 0f;
            float mult = o.Value<float?>("mult") ?? 1f;
            // one modifier per (mod, stat); a second entry for the same stat replaces the first
            StatApi.SetModifier(ctx.Id, stat, add, mult);
        }

        // ---------- foods ----------

        private static void LoadFood(Ctx ctx, JObject o, string path)
        {
            string id = Req(ctx, o, "id", path);
            string name = Req(ctx, o, "name", path);
            int price = o.Value<int?>("price") ?? throw new Exception($"{path}.price is required (a whole number)");
            var type = ParseEnum<FoodType>(ctx, o, "type", path, required: true);
            var ing = ParseEnum<BaseIngredient>(ctx, o, "ingredient", path, required: true);
            BaseIngredient? second = o["secondIngredient"] != null ? ParseEnum<BaseIngredient>(ctx, o, "secondIngredient", path, required: true) : (BaseIngredient?)null;

            var reference = Game.Config.foods.Select(f => f?.sprite).FirstOrDefault(s => s != null);
            Sprite sprite = LoadSprite(ctx, o, "sprite", path, reference);

            ContentApi.AddFood(
                id, name, price, type, ing, sprite,
                cookTime: o.Value<float?>("cookTime") ?? 8f,
                eatTime: o.Value<float?>("eatTime") ?? 10f,
                unlockedByDefault: o.Value<bool?>("unlockedByDefault") ?? true,
                secondIngredient: second,
                unlockMapId: o.Value<string>("unlockMapId"),
                unlockStar: o.Value<int?>("unlockStar") ?? 1);
        }

        // ---------- upgrades ----------

        private static void LoadUpgrade(Ctx ctx, JObject o, string path)
        {
            string id = Req(ctx, o, "id", path);
            string name = Req(ctx, o, "name", path);
            int cost = o.Value<int?>("cost") ?? throw new Exception($"{path}.cost is required (a whole number)");
            var category = ParseEnum<UpgradeCategory>(ctx, o, "category", path, required: true);

            string mapId = o.Value<string>("map");
            if (mapId != null && Game.Config.GetMap(mapId) == null)
                throw new Exception($"{path}.map: no map with id '{mapId}'. Valid: {string.Join(", ", Game.Config.maps.Where(m => m != null).Select(m => m.mapId))}");

            var effects = new List<UpgradeEffect>();
            if (o["effects"] is JArray arr)
                for (int i = 0; i < arr.Count; i++)
                {
                    string ep = $"{path}.effects[{i}]";
                    try
                    {
                        if (!(arr[i] is JObject eo)) throw new Exception("must be an object { ... }");
                        effects.Add(ParseEffect(ctx, eo, ep));
                    }
                    catch (Exception e) { ctx.Error(ep, e.Message); }
                }

            var reference = Game.Config.characterUpgrades.Select(u => u?.icon).FirstOrDefault(s => s != null);
            Sprite icon = LoadSprite(ctx, o, "icon", path, reference);

            ContentApi.AddUpgrade(
                mapId, id, name, o.Value<string>("description") ?? "", cost, category, effects, icon,
                requiredStars: o.Value<int?>("requiredStars") ?? 1,
                prerequisiteId: o.Value<string>("prerequisite"));
        }

        private static UpgradeEffect ParseEffect(Ctx ctx, JObject o, string path)
        {
            string type = Req(ctx, o, "type", path);
            switch (type.ToLowerInvariant())
            {
                case "stat":
                    return UpgradeEffects.Stat(
                        ParseEnum<StatId>(ctx, o, "stat", path, required: true),
                        o.Value<float?>("add") ?? 0f, o.Value<float?>("mult") ?? 1f);
                case "unlockfood":
                    string foodId = Req(ctx, o, "food", path);
                    var food = Game.Config.GetFood(foodId)
                        ?? throw new Exception($"{path}.food: no food with id '{foodId}' (define it in \"foods\" or use a game food id)");
                    return UpgradeEffects.UnlockFood(food);
                case "enablemapobject":
                    return UpgradeEffects.EnableMapObject(Req(ctx, o, "object", path));
                default:
                    throw new Exception($"{path}.type: unknown effect '{type}'. Valid: Stat, UnlockFood, EnableMapObject");
            }
        }

        // ---------- helpers ----------

        private static string Req(Ctx ctx, JObject o, string key, string path)
        {
            var v = o.Value<string>(key);
            if (string.IsNullOrWhiteSpace(v)) throw new Exception($"{path}.{key} is required");
            return v;
        }

        private static T ParseEnum<T>(Ctx ctx, JObject o, string key, string path, bool required) where T : struct
        {
            string s = o.Value<string>(key);
            if (s == null)
            {
                if (required) throw new Exception($"{path}.{key} is required. Valid: {string.Join(", ", Enum.GetNames(typeof(T)))}");
                return default;
            }
            if (Enum.TryParse(s, ignoreCase: true, out T result) && Enum.IsDefined(typeof(T), result)) return result;
            throw new Exception($"{path}.{key}: '{s}' is not valid. Valid: {string.Join(", ", Enum.GetNames(typeof(T)))}");
        }

        /// <summary>Loads a PNG named by the JSON field relative to the mod folder. Falls back to a borrowed game sprite (null) on any problem.</summary>
        private static Sprite LoadSprite(Ctx ctx, JObject o, string key, string path, Sprite fitTo)
        {
            string file = o.Value<string>(key);
            if (string.IsNullOrWhiteSpace(file)) return null;
            string full = Path.GetFullPath(Path.Combine(ctx.Dir, file));
            if (!full.StartsWith(Path.GetFullPath(ctx.Dir), StringComparison.OrdinalIgnoreCase))
            {
                ctx.Error($"{path}.{key}", "image path must stay inside the mod folder");
                return null;
            }
            var s = SpriteApi.FromPng(full, 100f, fitTo);
            if (s == null) ctx.Error($"{path}.{key}", $"could not load image '{file}' (using a placeholder instead)");
            return s;
        }
    }
}
