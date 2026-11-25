using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// A mount that steps back keeps facing ahead (D-005). Character.UpdateRotation turns every creature toward m_moveDir whenever it is not zero
/// (decompiled, Valheim 1.0.21), which would turn a backing mount round. For a backing mount the move direction is hidden during the turn only, so the
/// turn follows the look direction (the camera), and restored right after for the movement. Same approach as GunPowdered's AsksvinReverseTurnPatch.
/// Runs for every character every frame: returns at once unless one of this client's mounts steps back (rule 38).
/// </summary>
[HarmonyPatch(typeof(Character), "UpdateRotation")]
internal static class CharacterUpdateRotationPatch
{
    private static void Prefix(Character __instance, ref Vector3 ___m_moveDir, out Vector3 __state)
    {
        __state = Vector3.zero;
        if (MountSteering.AnyBacking && MountSteering.IsBacking(__instance))
        {
            __state = ___m_moveDir;
            ___m_moveDir = Vector3.zero;
            ModLog.Every("CharacterUpdateRotationPatch.hide", 2d, $"CharacterUpdateRotationPatch: {__instance.m_name} turns by its look direction while stepping back (move ({__state.x:0.##},{__state.z:0.##}) hidden for the turn)");
        }
    }

    private static void Postfix(ref Vector3 ___m_moveDir, Vector3 __state)
    {
        if (__state != Vector3.zero)
        {
            ___m_moveDir = __state;
            ModLog.Every("CharacterUpdateRotationPatch.restore", 2d, $"CharacterUpdateRotationPatch: move direction ({__state.x:0.##},{__state.z:0.##}) restored after the turn");
        }
    }
}
