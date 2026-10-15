using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CheckPlease.ModKit
{
    /// <summary>BepInEx entry point. Polls from Update until the game has booted, then raises <see cref="Game.Ready"/>.</summary>
    /// <remarks>
    /// Nothing game-related may run inside Awake: on Unity 6 the chainloader runs during the
    /// assembly-loaded lifecycle, where touching game types or starting coroutines crashes natively.
    /// </remarks>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class ModKitPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Harmony Harmony;
        internal static readonly List<string> failedPatches = new List<string>();
        private bool done;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded, waiting for game...");
        }

        private void Update()
        {
            if (done) return;
            if (!WaitForGame()) return;
            done = true;
            enabled = false;
        }

        private bool WaitForGame()
        {
            if (!Game.IsReady) return false;
            Log.LogInfo($"Game version {Application.version}; ModKit {PluginInfo.Version} was written against the demo build.");
            Harmony = new Harmony(PluginInfo.Guid);
            ApplyPatches();
            Log.LogInfo($"Game ready. Map='{GameManager.Instance.CurrentMap?.mapId}', day={GameManager.Instance.DayNumber}");
            ModEvents.Hook();
            Log.LogInfo("Tunable balance values: " + string.Join(", ", BalanceApi.Names()));
            JsonModLoader.LoadAll();
            Game.RaiseReady();
            return true;
        }

        /// <summary>
        /// Applies each patch class on its own. If the game changes and one target method no longer exists,
        /// only that feature is lost (and reported); everything else keeps working.
        /// </summary>
        private static void ApplyPatches()
        {
            var patchClasses = AccessTools.GetTypesFromAssembly(typeof(ModKitPlugin).Assembly)
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                .OrderBy(t => t.Name)
                .ToList();

            int ok = 0;
            foreach (var type in patchClasses)
            {
                try
                {
                    Harmony.CreateClassProcessor(type).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    failedPatches.Add(type.Name);
                    Log.LogError($"Patch '{type.Name}' could not be applied; the feature it powers will NOT work. " +
                                 $"The game was probably updated. Reason: {e.Message}");
                }
            }

            Log.LogInfo($"Applied {ok}/{patchClasses.Count} patches: " +
                        string.Join(", ", Harmony.GetPatchedMethods().Select(m => m.DeclaringType.Name + "." + m.Name)));
            if (failedPatches.Count > 0)
                Log.LogWarning("Failed patches: " + string.Join(", ", failedPatches));
        }
    }

    internal static class PluginInfo
    {
        public const string Guid = "com.checkplease.modkit";
        public const string Name = "CheckPlease ModKit";
        public const string Version = "0.1.0";
    }
}
