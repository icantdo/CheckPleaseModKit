using System;

namespace CheckPlease.ModKit
{
    /// <summary>
    /// Stable, mod-facing copy of the game's event bus. Subscribe from <see cref="Game.Ready"/> or later.
    /// Handler exceptions are caught and logged so one broken mod cannot break the game or other mods.
    /// </summary>
    public static class ModEvents
    {
        public static event Action<DayPhase> DayPhaseChanged;
        public static event Action<int> DayStarted;
        public static event Action<DayStats> DayEnded;
        public static event Action<int> GoldChanged;
        public static event Action<int> GoldEarned;
        public static event Action<CustomerGroup> CustomerSpawned;
        public static event Action<CustomerController> CustomerSeated;
        public static event Action<CustomerController> OrderTaken;
        public static event Action<FoodItem> FoodReady;
        public static event Action<CustomerController> FoodDelivered;
        public static event Action<CustomerController, LeaveReason> CustomerLeft;
        public static event Action<StatCounterId, int> StatCounterChanged;
        public static event Action<MissionDefinition> MissionCompleted;
        public static event Action<int> MedalEarned;
        public static event Action<int> StarEarned;
        public static event Action<UpgradeDefinition> UpgradePurchased;
        public static event Action<MapDefinition> MapCompleted;

        private static bool hooked;

        internal static void Hook()
        {
            if (hooked) return;
            hooked = true;
            GameEvents.OnDayPhaseChanged += a => Raise(DayPhaseChanged, a);
            GameEvents.OnDayStarted += a => Raise(DayStarted, a);
            GameEvents.OnDayEnded += a => Raise(DayEnded, a);
            GameEvents.OnGoldChanged += a => Raise(GoldChanged, a);
            GameEvents.OnGoldEarned += a => Raise(GoldEarned, a);
            GameEvents.OnCustomerSpawned += a => Raise(CustomerSpawned, a);
            GameEvents.OnCustomerSeated += a => Raise(CustomerSeated, a);
            GameEvents.OnOrderTaken += a => Raise(OrderTaken, a);
            GameEvents.OnFoodReady += a => Raise(FoodReady, a);
            GameEvents.OnFoodDelivered += a => Raise(FoodDelivered, a);
            GameEvents.OnCustomerLeft += (a, b) => Raise(CustomerLeft, a, b);
            GameEvents.OnStatCounterChanged += (a, b) => Raise(StatCounterChanged, a, b);
            GameEvents.OnMissionCompleted += a => Raise(MissionCompleted, a);
            GameEvents.OnMedalEarned += a => Raise(MedalEarned, a);
            GameEvents.OnStarEarned += a => Raise(StarEarned, a);
            GameEvents.OnUpgradePurchased += a => Raise(UpgradePurchased, a);
            GameEvents.OnMapCompleted += a => Raise(MapCompleted, a);
        }

        private static void Raise<T>(Action<T> h, T a)
        {
            if (h == null) return;
            foreach (var d in h.GetInvocationList())
            {
                try { ((Action<T>)d)(a); }
                catch (Exception e) { LogError(d, e); }
            }
        }

        private static void Raise<T1, T2>(Action<T1, T2> h, T1 a, T2 b)
        {
            if (h == null) return;
            foreach (var d in h.GetInvocationList())
            {
                try { ((Action<T1, T2>)d)(a, b); }
                catch (Exception e) { LogError(d, e); }
            }
        }

        private static void LogError(Delegate d, Exception e) =>
            ModKitPlugin.Log.LogError($"Mod event handler {d.Method.DeclaringType?.Name}.{d.Method.Name} threw: {e}");
    }
}
