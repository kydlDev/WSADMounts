using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.ServerSync;
using WSADMounts.Bootstrap;

namespace WSADMounts.Configuration;

/// <summary>
/// Network compatibility and the server's say over [General] Enable Mod, through KydlCore's ServerSync (user 2026-11-25: sync through ServerSync, not
/// Jotunn). ModRequired: every player on a server needs this mod in the same version, as before (the old EveryoneMustHaveMod / Patch level).
/// IsLocked: a player who is not a server admin cannot change a synced entry; the server's value is used.
/// </summary>
internal static class ModSync
{
    /// <summary>The ServerSync instance of this mod (created when this class is first used, which is in ModSettings.Bind in Awake).</summary>
    public static readonly ConfigSync Sync = new ConfigSync(Plugin.ModGuid)
    {
        DisplayName = Plugin.ModName,
        CurrentVersion = Plugin.ModVersion,
        MinimumRequiredVersion = Plugin.ModVersion,
        ModRequired = true,
        IsLocked = true,
    };

    /// <summary>Expected outcome: the entry is synced from the server (returns that entry).</summary>
    public static ConfigEntry<T> Share<T>(ConfigEntry<T> entry)
    {
        Sync.AddConfigEntry(entry);
        ModLog.Verbose($"ModSync.Share: [{entry.Definition.Section}] {entry.Definition.Key} is synced from the server (mod version {Plugin.ModVersion}, required on every player)");
        return entry;
    }
}