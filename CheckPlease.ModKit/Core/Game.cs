using System;
using System.Collections.Generic;

namespace CheckPlease.ModKit
{
    /// <summary>Safe access to the game's singletons. Use <see cref="Ready"/> before touching game state.</summary>
    public static class Game
    {
        private static event Action ready;
        private static bool raised;

        /// <summary>
        /// Raised once, after GameManager has booted and the save is loaded.
        /// If the game is already ready when you subscribe, your handler is called immediately.
        /// </summary>
        public static event Action Ready
        {
            add
            {
                if (raised) Invoke(value);
                else ready += value;
            }
            remove { ready -= value; }
        }

        public static bool IsReady =>
            GameManager.Instance != null
            && GameManager.Instance.Config != null
            && GameManager.Instance.CurrentMap != null
            && SaveManager.Instance != null
            && SaveManager.Instance.Loaded;

        /// <summary>Names of ModKit patches that could not be applied (empty when everything works). Check before relying on a hook.</summary>
        public static IReadOnlyList<string> FailedPatches => ModKitPlugin.failedPatches;

        public static GameConfig Config => GameManager.Instance?.Config;
        public static GameBalance Balance => GameManager.Instance?.Balance;
        public static MapDefinition CurrentMap => GameManager.Instance?.CurrentMap;

        internal static void RaiseReady()
        {
            if (raised) return;
            raised = true;
            var handlers = ready;
            ready = null;
            if (handlers == null) return;
            foreach (var d in handlers.GetInvocationList()) Invoke((Action)d);
        }

        private static void Invoke(Action a)
        {
            try { a(); }
            catch (Exception e) { ModKitPlugin.Log.LogError($"Exception in Game.Ready handler {a.Method.DeclaringType?.Name}.{a.Method.Name}: {e}"); }
        }
    }
}
