using KydlCore.Common;
using KydlCore.ServerSync;
using WSADMounts.Bootstrap;

namespace WSADMounts.Configuration;

/// <summary>
/// Network check through KydlCore 1.2.0's KyNetwork (ServerSync inside KydlCore; no Jotunn). ClientOnly (user 2026-11-27): the mod works on the
/// player's own side, so a player with it joins a server without it; when the server has it too, the versions must be equal. NO setting is
/// shared with the server: [General] Enable Mod is each player's own choice.
/// </summary>
internal static class ModSync
{
    /// <summary>The mod's ServerSync instance (made by KydlCore on first use, which is Start in Plugin.Awake).</summary>
    public static readonly ConfigSync Sync = KyNetwork.Register(Plugin.ModGuid, Plugin.ModName, Plugin.ModVersion, KyNetworkLevel.ClientOnly);

    /// <summary>Starts the version check (the first use of this class does it). Called once in Plugin.Awake. Expected outcome: true.</summary>
    public static bool Start()
    {
        bool required = Sync.ModRequired;
        ModLog.Verbose($"ModSync.Start: version check active for {Sync.Name} {Sync.CurrentVersion} (required on every player: {required}; no setting is shared with the server)");
        return !required;
    }
}
