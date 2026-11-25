using HarmonyLib;
using KydlCore.Common;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>The game resets a saddle's controls when the rider leaves or loses control (Sadle.ResetControlls, private, Valheim 1.0.21): the back step ends too.</summary>
[HarmonyPatch(typeof(Sadle), "ResetControlls")]
internal static class SadleResetControllsPatch
{
    private static void Postfix(Sadle __instance)
    {
        bool stopped = MountSteering.Reset(__instance);
        ModLog.Verbose($"SadleResetControllsPatch: controls reset, back step stopped {stopped}");
    }
}
