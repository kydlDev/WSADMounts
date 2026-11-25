using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Configuration;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// Owner side: the game's Sadle.UpdateRiding walks the mount with MonsterAI.MoveTowards, which also turns it toward the walk direction. While the
/// rider steps back, this postfix turns the facing back to the camera direction and moves the mount backwards at <see cref="ModSettings.BackStepSpeedFactor"/>
/// of its walk speed (Character.UpdateWalking: velocity = moveDir * speed, so the length of moveDir scales the speed). Valheim 1.0.21.
/// </summary>
[HarmonyPatch(typeof(Sadle), nameof(Sadle.UpdateRiding))]
internal static class SadleUpdateRidingPatch
{
    private static void Postfix(Sadle __instance, bool __result, Character ___m_character)
    {
        MountSteerState state = __result && MountSteering.AnyBacking && ___m_character != null && MountSteering.IsBacking(___m_character)
            ? MountSteering.Find(__instance)
            : null;
        if (state != null)
        {
            StepBack(___m_character, state);
        }
        else if (ModLog.IsDue("SadleUpdateRidingPatch.vanilla", 10d))
        {
            ModLog.Verbose($"SadleUpdateRidingPatch: no back step (riding {__result}, any mount backing {MountSteering.AnyBacking}) - the game walks the mount");
        }
    }

    private static void StepBack(Character mount, MountSteerState state)
    {
        float factor = ModSettings.BackStepSpeedFactor.Value;
        mount.SetLookDir(state.Face);
        mount.SetMoveDir(state.Move * factor);
        mount.SetRun(false);
        ModLog.Every("SadleUpdateRidingPatch.back", 2d, $"SadleUpdateRidingPatch: {mount.m_name} steps back - face=({state.Face.x:0.##},{state.Face.z:0.##}) move=({state.Move.x:0.##},{state.Move.z:0.##}) x{factor:0.##} of walk, velocity {mount.GetVelocity().magnitude:0.##} m/s");
    }
}
