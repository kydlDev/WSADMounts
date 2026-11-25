using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Testing.TestHooks;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// TEST ONLY (D-024): lets a lab test fake the controller's left stick at the game's own input point (ZInput.GetJoyLeftStick, assembly_utils,
/// Valheim 1.0.21), so the controller path is tested without a controller. Does nothing unless a test armed <see cref="RideTestStick"/>.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetJoyLeftStick))]
internal static class ZInputJoyLeftStickPatch
{
    private static void Postfix(ref Vector2 __result)
    {
        if (RideTestStick.Armed && RideTestStick.Apply(ref __result))
        {
            ModLog.Every("ZInputJoyLeftStickPatch", 5d, $"ZInputJoyLeftStickPatch: the game reads the faked left stick ({__result.x:0.##},{__result.y:0.##})");
        }
    }
}
