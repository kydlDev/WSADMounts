using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Features.Hub.Contracts;
using WSADMounts.Bootstrap;

namespace WSADMounts.Presentation.Hub.Registration;

/// <summary>
/// What this mod puts into the KY hub of KydlCore (F6) - the mod DECLARES its page here, KydlCore draws it (MIGRATION_DECISIONS B11).
/// There is NO menu key, window or input lock in this mod: KydlCore owns them. Row 1 of the hub shows this mod (SetModTitle), row 2 its tabs.
/// The mod has only the settings tab (KydlCore shows one tab per config category: General, Animation, DebugLog).
/// </summary>
internal static class HubTabs
{
    internal const string SettingsId = "settings";
    private const int SettingsOrder = 100;

    /// <summary>Once from ModInstaller.Install: the mod's title and its settings tab. Expected outcome: the settings tab is registered.</summary>
    internal static bool RegisterModPage(ConfigFile config)
    {
        KyHub.SetModTitle(Plugin.ModGuid, Plugin.ModName);
        var settings = KyHub.AddTab(Plugin.ModGuid, SettingsId, Loc.T("wsadmounts_tab_settings"), () => KyHub.ConfigEntries(config), SettingsOrder);
        ModLog.Verbose($"HubTabs: title + settings tab {(settings != null ? "registered" : "FAILED")}");
        return settings != null;
    }

    /// <summary>Mod unload: every tab of this mod is removed. Expected outcome: the settings tab is gone.</summary>
    internal static bool UnregisterModPage()
    {
        bool removed = KyHub.RemoveTab(Plugin.ModGuid, SettingsId);
        bool gone = !KyHub.HasTab(Plugin.ModGuid, SettingsId);
        ModLog.Verbose($"HubTabs: tab '{SettingsId}' {(removed ? "removed" : "was not registered")}{(gone ? string.Empty : " - STILL THERE")}");
        return gone;
    }
}
