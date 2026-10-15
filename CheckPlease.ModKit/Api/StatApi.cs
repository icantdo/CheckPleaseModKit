using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CheckPlease.ModKit
{
    /// <summary>
    /// Mod-owned modifiers on the game's upgrade stats (<see cref="StatId"/>): final = (base + adds) * mults.
    /// The game rebuilds its modifiers whenever an upgrade is bought; ModKit re-applies yours afterwards.
    /// </summary>
    public static class StatApi
    {
        private struct Mod { public float Add; public float Mult; }
        private static readonly Dictionary<(string owner, StatId stat), Mod> mods = new Dictionary<(string, StatId), Mod>();

        private static readonly FieldInfo AddField = AccessTools.Field(typeof(UpgradeManager), "statAdd");
        private static readonly FieldInfo MultField = AccessTools.Field(typeof(UpgradeManager), "statMult");

        /// <summary>Sets (replaces) this owner's modifier for a stat. Use your mod's GUID as owner.</summary>
        public static void SetModifier(string owner, StatId stat, float add = 0f, float mult = 1f)
        {
            mods[(owner, stat)] = new Mod { Add = add, Mult = mult };
            UpgradeManager.Instance?.ApplyAllPurchased(); // postfix below layers the mods
        }

        public static void RemoveModifier(string owner, StatId stat)
        {
            if (mods.Remove((owner, stat))) UpgradeManager.Instance?.ApplyAllPurchased();
        }

        /// <summary>Current final value of a stat as the game sees it.</summary>
        public static float Get(StatId stat) => UpgradeManager.Instance != null ? UpgradeManager.Instance.GetStat(stat) : 0f;

        internal static void ApplyTo(UpgradeManager um)
        {
            if (mods.Count == 0) return;
            var add = (float[])AddField.GetValue(um);
            var mult = (float[])MultField.GetValue(um);
            foreach (var kv in mods)
            {
                int i = (int)kv.Key.stat;
                add[i] += kv.Value.Add;
                mult[i] *= kv.Value.Mult;
            }
        }
    }

    // ApplyAllPurchased resets the game's modifiers and re-applies upgrades; that is the safe moment to layer ours on top.
    [HarmonyPatch(typeof(UpgradeManager), nameof(UpgradeManager.ApplyAllPurchased))]
    internal static class UpgradeManager_ApplyAllPurchased_Patch
    {
        private static void Postfix(UpgradeManager __instance)
        {
            try { StatApi.ApplyTo(__instance); }
            catch (Exception e) { ModKitPlugin.Log.LogError($"StatApi re-apply failed: {e}"); }
        }
    }
}
