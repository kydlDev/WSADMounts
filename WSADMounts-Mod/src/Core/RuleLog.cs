using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WSADMounts.Core
{
    /// <summary>
    /// Debug logging for the pure code (Core and the features' Domain): PROJECT_RULES 8 "every method logs". The pure code is also compiled into the unit
    /// tests, which run without the game, BepInEx or KydlCore, so it cannot call ModLog itself. Plugin.Awake connects this to ModLog (Sink / DebugOn);
    /// in the unit tests nothing is connected and every call does nothing.
    /// </summary>
    public static class RuleLog
    {
        static readonly Dictionary<string, long> LastWritten = new Dictionary<string, long>();
        static readonly Stopwatch Clock = Stopwatch.StartNew();

        /// <summary>Where the lines go (ModLog.Verbose in the game; null in the unit tests).</summary>
        public static Action<string> Sink { get; set; }

        /// <summary>Whether debug logging is on ([DebugLog] Enable in the game; null = off).</summary>
        public static Func<bool> DebugOn { get; set; }

        /// <summary>True when a line would be written: callers check it before building the text, so a game with debug logging off pays nothing.</summary>
        public static bool On => Sink != null && DebugOn != null && DebugOn();

        /// <summary>Same as <see cref="On"/> as a method call, so mutation testing can leave the log guard out (quality.json ignoreMethods: Stryker skips method calls, not properties; GunPowdered D-133).</summary>
        public static bool IsOn() => On;

        /// <summary>Writes the line when debug logging is on.</summary>
        public static void Verbose(string message)
        {
            if (On) Sink(message);
        }

        /// <summary>Writes the line at most once per <paramref name="seconds"/> for this key (code that runs every frame). Expected outcome: true when written.</summary>
        public static bool Every(string key, double seconds, string message)
        {
            if (!On) return false;
            long now = Clock.ElapsedMilliseconds;
            lock (LastWritten)
            {
                if (LastWritten.TryGetValue(key, out long last) && now - last < seconds * 1000d) return false;
                LastWritten[key] = now;
            }
            Sink(message);
            return true;
        }
        /// <summary>
        /// A value as one short log text: line breaks become " | ", at most 160 characters (then "... (N chars)"), and "CHECK OFF" is written "CHECK_OFF" -
        /// a logged value (a flight recorder dump, a log line passed on) must not count as a real failed check (rule log check common.no-check-off).
        /// </summary>
        public static string Shown(object value)
        {
            string text = value == null ? "null" : value.ToString();
            text = text.Replace("\r\n", " | ").Replace("\n", " | ").Replace("\r", " | ").Replace("CHECK OFF", "CHECK_OFF");
            return text.Length <= 160 ? text : text.Substring(0, 160) + "... (" + text.Length + " chars)";
        }
    }
}
