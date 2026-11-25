using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Domain;
using WSADMounts.Features.Riding.Infrastructure;
using WSADMounts.Testing.TestHooks;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// Rider side (the local player's client): replaces the game's saddle controls while the custom controls are on. The game reads only the forward
/// key and the camera (A/D ignored, S = stop, the mount keeps walking after W is released); the mod asks <see cref="RideSteeringRules"/> and sends
/// the answer through the game's own "Controls" RPC, so the mount's owner side (vanilla RPC_Controls) needs no change. Patched: Sadle.ApplyControlls
/// (Valheim 1.0.21). Keyboard and controller arrive in the same moveDir (the game's input path), so both are covered by one code path.
/// </summary>
[HarmonyPatch(typeof(Sadle), nameof(Sadle.ApplyControlls))]
internal static class SadleApplyControllsPatch
{
    private const string ControlsRpc = "Controls";

    private static bool Prefix(Sadle __instance, Vector3 moveDir, Vector3 lookDir, ref bool run, bool autoRun, bool block, Character ___m_character, ZNetView ___m_nview)
    {
        BlockSprint(ref run, "keys");
        bool runOriginal = !RidingFeature.SteeringOn || Player.m_localPlayer == null || ___m_character == null || ___m_nview == null || !___m_nview.IsValid();
        if (runOriginal)
        {
            if (ModLog.IsDue("SadleApplyControllsPatch.vanilla", 5d)) ModLog.Verbose($"SadleApplyControllsPatch: vanilla controls (steering on {RidingFeature.SteeringOn}, player {Player.m_localPlayer != null}, view valid {___m_nview != null && ___m_nview.IsValid()})");
            return runOriginal;
        }

        bool testInput = RideTestInput.Apply(ref moveDir, ref run);      // TEST ONLY, inert unless a lab test armed it
        BlockSprint(ref run, "test input");
        MountSteerState state = MountSteering.For(__instance, ___m_character);
        RideCommand command = MountSteering.Steer(state, moveDir, lookDir, run, autoRun, block, ___m_nview.IsOwner());

        // the game's RPC: rideDir (0 = keep the last one), speed (the game's Sadle.Speed int), the rider's skill factor
        Vector3 rideDir = state.Back ? state.Move : state.Face;
        float skill = Player.m_localPlayer.GetSkills().GetSkillFactor(Skills.SkillType.Ride);
        ___m_nview.InvokeRPC(ControlsRpc, rideDir, (int)command.Speed, skill);
        ModLog.Every("SadleApplyControllsPatch.sent", 2d, $"SadleApplyControllsPatch: sent {ControlsRpc} dir=({rideDir.x:0.##},{rideDir.z:0.##}) speed={command.Speed} skill={skill:0.##} back={state.Back} testInput={testInput}");
        return runOriginal;
    }

    /// <summary>Enable Mount Sprint off: the mount does not run at all (user 2026-11-25, D-037), with or without the custom controls.</summary>
    private static void BlockSprint(ref bool run, string from)
    {
        if (run && RidingFeature.SprintBlocked)
        {
            run = false;
            if (ModLog.IsDue("SadleApplyControllsPatch.noSprint", 5d)) ModLog.Verbose($"SadleApplyControllsPatch: sprint ignored ({from}, Enable Mount Sprint off)");
        }
    }
}
