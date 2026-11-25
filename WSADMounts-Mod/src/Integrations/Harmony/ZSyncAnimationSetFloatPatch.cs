using HarmonyLib;
using KydlCore.Common;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// The look of a back step (D-013, D-014): Moonwalk. Character.UpdateWalking hands the walk animation's "forward_speed" (velocity . forward) to
/// ZSyncAnimation.SetFloat, which is negative while stepping back; the mounts' walk blend trees start at 0, so the mount would slide in its idle pose.
/// The value is flipped to positive here, BEFORE the game stores it: SetFloat damps forward_speed (0.2 s) and writes it to the ZDO for every other player,
/// so one write per step keeps the smoothing and the synced value right (lab run a1ride1-1, D-023: flipping it AFTER the game's own damped write
/// made the two writes pull against each other, forward_speed stayed near 0.1). BackwardWalk has no clips yet (T-006) and shows the Moonwalk, like every
/// mount without a clip. Runs for every animated character's float parameters: returns at once unless a mount of this client steps back (rule 38).
/// ZSyncAnimation.SetFloat(int, float), Valheim 1.0.21.
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
            ModLog.Every("ZSyncAnimationSetFloatPatch.moonwalk", 2d, $"ZSyncAnimationSetFloatPatch: forward_speed {was:0.##} -> {value:0.##} (moonwalk, stepping back)");
        }
    }
}
