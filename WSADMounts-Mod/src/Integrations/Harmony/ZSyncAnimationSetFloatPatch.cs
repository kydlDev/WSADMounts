using HarmonyLib;
using KydlCore.Common;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// The base animator layer under a back step (D-023, D-032). Character.UpdateWalking hands the walk animation's "forward_speed" (velocity . forward)
/// to ZSyncAnimation.SetFloat, which is negative while stepping back; the mounts' walk blend trees start at 0, so the controller would sit in idle.
/// The value is flipped to positive here, BEFORE the game stores it (SetFloat damps it 0.2 s and writes it to the ZDO; flipping after the game's own
/// write made the two writes pull against each other, lab a1ride1-1). The controller then stays in its walk state, so the clip that
/// BackwardWalkPlayer lays over it (reversed or hand-made walk; alone it IS the Moonwalk, D-035) hands back to a walking animator, not a jump from idle, when the step ends; it
/// is also what a mount with no walk clip at all shows. Runs for every animated character's float parameters: returns at once unless a mount of
/// this client steps back (rule 38). ZSyncAnimation.SetFloat(int, float), Valheim 1.0.30.
/// </summary>
[HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetFloat), new[] { typeof(int), typeof(float) })]
internal static class ZSyncAnimationSetFloatPatch
{
    private static readonly int ForwardSpeed = ZSyncAnimation.GetHash("forward_speed");

    private static void Prefix(ZSyncAnimation __instance, int hash, ref float value)
    {
        if (MountSteering.AnyBacking && hash == ForwardSpeed && value < 0f && MountSteering.IsBacking(__instance))
        {
            float was = value;
            value = -value;
            ModLog.Every("ZSyncAnimationSetFloatPatch.base", 2d, $"ZSyncAnimationSetFloatPatch: forward_speed {was:0.##} -> {value:0.##} (base layer walks while stepping back)");
        }
    }
}
