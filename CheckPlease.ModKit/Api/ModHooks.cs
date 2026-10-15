using System;
using HarmonyLib;

namespace CheckPlease.ModKit
{
    /// <summary>
    /// Gameplay hooks: each one lets a mod CHANGE a value the game is about to use.
    /// Handlers receive the value by <c>ref</c> and may modify it. Several mods can subscribe; they run in subscription order,
    /// each seeing the previous one's result. A handler that throws is logged and skipped.
    /// Subscribe from <see cref="Game.Ready"/> or later.
    /// </summary>
    public static class ModHooks
    {
        public delegate void HappinessHandler(CustomerController customer, ref float delta);
        public delegate void PayoutHandler(CustomerController customer, ref int payout);
        public delegate void CookTimeHandler(FoodDefinition food, ref float seconds);
        public delegate void SpawnIntervalHandler(ref float seconds);
        public delegate void OrderHandler(CustomerController customer, ref FoodDefinition food);

        /// <summary>
        /// Every change to a guest's happiness (negative = patience drain). WARNING: the game drains patience every frame
        /// for every waiting guest, so this runs very often. Keep handlers tiny and do not log or allocate in them.
        /// </summary>
        public static event HappinessHandler HappinessChanging;

        /// <summary>The gold a guest pays for their meal, after the game's own calculation.</summary>
        public static event PayoutHandler PayoutComputing;

        /// <summary>Seconds the kitchen needs to cook a dish (already includes cook-speed upgrades).</summary>
        public static event CookTimeHandler CookTimeComputing;

        /// <summary>Seconds until the next guest party spawns.</summary>
        public static event SpawnIntervalHandler SpawnIntervalComputing;

        /// <summary>The dish a guest decides to order. Set <c>food</c> to another dish to change it (it must be a real FoodDefinition).</summary>
        public static event OrderHandler OrderChoosing;

        internal static void RaiseHappiness(CustomerController c, ref float delta)
        {
            var h = HappinessChanging;
            if (h == null) return;
            foreach (var d in h.GetInvocationList())
                try { ((HappinessHandler)d)(c, ref delta); } catch (Exception e) { Fail(d, e); }
        }

        internal static void RaisePayout(CustomerController c, ref int payout)
        {
            var h = PayoutComputing;
            if (h == null) return;
            foreach (var d in h.GetInvocationList())
                try { ((PayoutHandler)d)(c, ref payout); } catch (Exception e) { Fail(d, e); }
            if (payout < 0) payout = 0;
        }

        internal static void RaiseCookTime(FoodDefinition f, ref float seconds)
        {
            var h = CookTimeComputing;
            if (h == null) return;
            foreach (var d in h.GetInvocationList())
                try { ((CookTimeHandler)d)(f, ref seconds); } catch (Exception e) { Fail(d, e); }
            if (seconds < 0.1f) seconds = 0.1f;
        }

        internal static void RaiseSpawnInterval(ref float seconds)
        {
            var h = SpawnIntervalComputing;
            if (h == null) return;
            foreach (var d in h.GetInvocationList())
                try { ((SpawnIntervalHandler)d)(ref seconds); } catch (Exception e) { Fail(d, e); }
            if (seconds < 0.5f) seconds = 0.5f; // protect the game from a spawn loop
        }

        internal static void RaiseOrder(CustomerController c, ref FoodDefinition food)
        {
            var h = OrderChoosing;
            if (h == null) return;
            var original = food;
            foreach (var d in h.GetInvocationList())
                try { ((OrderHandler)d)(c, ref food); } catch (Exception e) { Fail(d, e); }
            if (food == null) food = original;
        }

        private static void Fail(Delegate d, Exception e) =>
            ModKitPlugin.Log.LogError($"Mod hook handler {d.Method.DeclaringType?.Name}.{d.Method.Name} threw: {e}");
    }

    // ---- Harmony patches that feed the hooks ----

    [HarmonyPatch(typeof(CustomerController), nameof(CustomerController.ApplyHappiness))]
    internal static class Patch_ApplyHappiness
    {
        private static void Prefix(CustomerController __instance, ref float delta) => ModHooks.RaiseHappiness(__instance, ref delta);
    }

    [HarmonyPatch(typeof(EconomyManager), nameof(EconomyManager.ComputePayout))]
    internal static class Patch_ComputePayout
    {
        private static void Postfix(CustomerController c, ref int __result) => ModHooks.RaisePayout(c, ref __result);
    }

    [HarmonyPatch(typeof(OrderManager), "CookTime")]
    internal static class Patch_CookTime
    {
        private static void Postfix(FoodDefinition food, ref float __result) => ModHooks.RaiseCookTime(food, ref __result);
    }

    [HarmonyPatch(typeof(CustomerSpawner), nameof(CustomerSpawner.NextInterval))]
    internal static class Patch_NextInterval
    {
        private static void Postfix(ref float __result) => ModHooks.RaiseSpawnInterval(ref __result);
    }

    [HarmonyPatch(typeof(OrderManager), nameof(OrderManager.GenerateOrder))]
    internal static class Patch_GenerateOrder
    {
        private static void Postfix(CustomerController customer, ref FoodDefinition __result) => ModHooks.RaiseOrder(customer, ref __result);
    }
}
