using System;
using System.Collections.Generic;
using System.Reflection;

namespace CheckPlease.ModKit
{
    /// <summary>
    /// Read and change <see cref="GameBalance"/> tuning values by field name (e.g. "tipHappiest").
    /// Changes live in memory only. Originals are remembered, so <see cref="Reset"/> always restores the game's value.
    /// Supports float, int and bool fields.
    /// </summary>
    public static class BalanceApi
    {
        private static readonly Dictionary<string, object> originals = new Dictionary<string, object>();

        /// <summary>Names of all tunable fields.</summary>
        public static IEnumerable<string> Names()
        {
            foreach (var f in typeof(GameBalance).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (IsSupported(f.FieldType)) yield return f.Name;
        }

        public static T Get<T>(string name) => (T)Convert.ChangeType(Field(name).GetValue(RequireBalance()), typeof(T));

        public static void Set(string name, object value)
        {
            var f = Field(name);
            var b = RequireBalance();
            if (!originals.ContainsKey(name)) originals[name] = f.GetValue(b);
            f.SetValue(b, Convert.ChangeType(value, f.FieldType));
        }

        /// <summary>Multiplies the ORIGINAL value (not the current one), so calling it twice does not compound.</summary>
        public static void Scale(string name, float factor)
        {
            var f = Field(name);
            if (f.FieldType != typeof(float) && f.FieldType != typeof(int))
                throw new ArgumentException($"'{name}' is not numeric.");
            var b = RequireBalance();
            if (!originals.ContainsKey(name)) originals[name] = f.GetValue(b);
            double orig = Convert.ToDouble(originals[name]);
            f.SetValue(b, Convert.ChangeType(orig * factor, f.FieldType));
        }

        public static void Reset(string name)
        {
            if (!originals.TryGetValue(name, out var o)) return;
            Field(name).SetValue(RequireBalance(), o);
            originals.Remove(name);
        }

        public static void ResetAll()
        {
            foreach (var n in new List<string>(originals.Keys)) Reset(n);
        }

        private static GameBalance RequireBalance() =>
            Game.Balance ?? throw new InvalidOperationException("Game is not ready yet (use Game.Ready).");

        private static bool IsSupported(Type t) => t == typeof(float) || t == typeof(int) || t == typeof(bool);

        private static FieldInfo Field(string name)
        {
            var f = typeof(GameBalance).GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f == null || !IsSupported(f.FieldType))
                throw new ArgumentException($"GameBalance has no float/int/bool field '{name}'.");
            return f;
        }
    }
}
