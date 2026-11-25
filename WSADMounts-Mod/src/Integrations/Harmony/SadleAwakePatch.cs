using HarmonyLib;
using KydlCore.Common;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>A saddled mount loaded on this client: read its back step from the ZDO (late joiners, zone loads).</summary>
[HarmonyPatch(typeof(Sadle), "Awake")]
internal static class SadleAwakePatch
{
    private static void Postfix(Character ___m_character)
    {
        BackwardWalkSync.Register();
        BackwardWalkSync.ApplyFromZdo(___m_character);
        ModLog.Every("SadleAwakePatch", 10d, $"SadleAwakePatch: saddle of {(___m_character != null ? ___m_character.m_name : "<none>")} loaded");
    }
}
