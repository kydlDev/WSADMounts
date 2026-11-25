using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// The look of a back step (D-013, D-014). Character.UpdateWalking writes the walk animation's "forward_speed" as velocity . forward, which is
/// negative while stepping back, and the mounts' walk blend trees start at 0: the mount would slide in its idle pose. Moonwalk = the same speed
/// written as a positive number, so the normal walk plays; ZSyncAnimation sends it to every other player. BackwardWalk has no clips yet (T-006)
/// and therefore shows the Moonwalk, as every mount without a clip does. Runs for every character every frame: it returns at once unless one
/// of this client's mounts steps back (rule 38). Valheim 1.0.21.
/// </summary>
[HarmonyPatch(typeof(Character), "UpdateWalking")]
internal static class CharacterUpdateWalkingPatch
{
    private static readonly int ForwardSpeed = ZSyncAnimation.GetHash("forward_speed");

    private static void Postfix(Character __instance, Vector3 ___m_currentVel, ZSyncAnimation ___m_zanim)
    {
        if (MountSteering.AnyBacking && MountSteering.IsBacking(__instance) && ___m_zanim != null)
        {
            float forward = Vector3.Dot(___m_currentVel, __instance.transform.forward);
            float shown = Mathf.Abs(forward);
            ___m_zanim.SetFloat(ForwardSpeed, shown);
            ModLog.Every("CharacterUpdateWalkingPatch.moonwalk", 2d, $"CharacterUpdateWalkingPatch: {__instance.m_name} forward_speed {forward:0.##} -> {shown:0.##} (moonwalk)");
        }
    }
}
