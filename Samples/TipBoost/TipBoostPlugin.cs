using BepInEx;
using CheckPlease;
using CheckPlease.ModKit;

namespace TipBoost
{
    /// <summary>Sample mod: doubles happy-guest tips, makes the waiter 25% faster, and logs gold earned.</summary>
    [BepInPlugin("com.example.tipboost", "Tip Boost", "1.0.0")]
    [BepInDependency("com.checkplease.modkit")]
    public class TipBoostPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Never touch game types here; wait for the game to be ready.
            Game.Ready += OnGameReady;
        }

        private void OnGameReady()
        {
            BalanceApi.Scale("tipHappiest", 2f);
            BalanceApi.Scale("tipHappy", 2f);
            StatApi.SetModifier("com.example.tipboost", StatId.MoveSpeed, mult: 1.25f);

            ModEvents.GoldEarned += amount => Logger.LogInfo($"Earned {amount} gold");
            Logger.LogInfo($"Tip Boost active. MoveSpeed is now {StatApi.Get(StatId.MoveSpeed):0.00}");
        }
    }
}
