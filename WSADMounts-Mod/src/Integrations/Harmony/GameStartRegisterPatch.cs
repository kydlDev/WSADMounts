using HarmonyLib;
using KydlCore.Common;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// D-031: registers the back-step RPC for every new network session (Game.Start runs when a world is entered; ZRoutedRpc exists by then), and
/// lets a mount that loads on this client start its reversed walk from its ZDO (Sadle.Awake). Valheim 1.0.30.
/// </summary>
[HarmonyPatch(typeof(Game), "Start")]
internal static class GameStartRegisterPatch
{
    private static void Postfix()
    {
        bool registered = BackwardWalkSync.Register();
        ModLog.Verbose($"GameStartRegisterPatch: back-step RPC registered {registered}");
    }
}
