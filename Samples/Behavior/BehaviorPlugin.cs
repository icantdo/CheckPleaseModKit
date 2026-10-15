using System.Linq;
using BepInEx;
using CheckPlease;
using CheckPlease.ModKit;
using UnityEngine;

namespace Behavior
{
    /// <summary>Sample mod using every gameplay hook.</summary>
    [BepInPlugin("com.example.behavior", "Behavior Sample", "1.0.0")]
    [BepInDependency("com.checkplease.modkit")]
    public class BehaviorPlugin : BaseUnityPlugin
    {
        private void Awake() => Game.Ready += Register;

        // Logs each hook the first time it really changes something, so you can see it working without spamming the log.
        private readonly System.Collections.Generic.HashSet<string> logged = new System.Collections.Generic.HashSet<string>();
        private void LogOnce(string key, string message)
        {
            if (logged.Add(key)) Logger.LogInfo(message);
        }

        private void Register()
        {
            // Guests lose patience at half speed (positive changes are left alone).
            ModHooks.HappinessChanging += (CustomerController c, ref float delta) =>
            {
                if (delta < 0f) delta *= 0.5f;
            };

            // Happiest guests pay 25% extra.
            ModHooks.PayoutComputing += (CustomerController c, ref int payout) =>
            {
                if (c.Face == HappinessFace.Happiest)
                {
                    int before = payout;
                    payout = Mathf.RoundToInt(payout * 1.25f);
                    LogOnce("payout", $"Payout hook fired: {before} -> {payout}");
                }
            };

            // Desserts cook 30% faster.
            ModHooks.CookTimeComputing += (FoodDefinition food, ref float seconds) =>
            {
                if (food.foodType == FoodType.Sweet)
                {
                    seconds *= 0.7f;
                    LogOnce("cook", $"Cook-time hook fired for '{food.id}': {seconds:0.0}s");
                }
            };

            // A guest party arrives 15% sooner.
            ModHooks.SpawnIntervalComputing += (ref float seconds) =>
            {
                seconds *= 0.85f;
                LogOnce("spawn", $"Spawn-interval hook fired: next guests in {seconds:0.0}s");
            };

            // One guest in four splurges on the most expensive dish they can safely eat.
            ModHooks.OrderChoosing += (CustomerController c, ref FoodDefinition food) =>
            {
                if (Random.value > 0.25f) return;
                var priciest = UpgradeManager.Instance.CurrentMenu()
                    .Where(f => !c.IsAllergicTo(f))
                    .OrderByDescending(f => f.basePrice)
                    .FirstOrDefault();
                if (priciest != null)
                {
                    food = priciest;
                    LogOnce("order", $"Order hook fired: a guest splurged on '{priciest.id}'");
                }
            };

            Logger.LogInfo("Behavior Sample active: patient guests, happy-guest bonus, fast desserts, quicker spawns, splurging guests.");
        }
    }
}
