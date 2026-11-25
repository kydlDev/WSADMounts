using HarmonyLib;
using KydlCore.Common;
using WSADMounts.Features.Riding.Domain;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Integrations.Harmony;

/// <summary>
/// Rider side (D-037): while the local player rides a saddle, the game's Player.SetControls dismounts on jump, attack, secondary attack and
/// dodge. This prefix asks <see cref="MountActionRules"/>; the presses of switched-on parts are taken from the game (no dismount, the rider
/// does not swing) and become the mount's jump / attack (<see cref="MountActions"/>). Use (E) still dismounts (Player.Update, not here).
/// Runs every frame: returns at once without one of the four presses (PROJECT_RULES 38). Valheim 1.0.30.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class PlayerSetControlsPatch
{
    private static readonly AccessTools.FieldRef<Sadle, Character> MountOf = AccessTools.FieldRefAccess<Sadle, Character>("m_character");
    private static readonly AccessTools.FieldRef<Sadle, ZNetView> ViewOf = AccessTools.FieldRefAccess<Sadle, ZNetView>("m_nview");

    private static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool jump, ref bool dodge)
    {
        if ((jump || attack || secondaryAttack || dodge) && RidingFeature.Running != null && __instance == Player.m_localPlayer
            && __instance.GetDoodadController() is Sadle saddle)
        {
            if (ModLog.IsDue("PlayerSetControlsPatch.press", 5d)) ModLog.Verbose($"PlayerSetControlsPatch: press while riding {saddle.name} (jump {jump}, attack {attack}, secondary {secondaryAttack}, dodge {dodge})");
            Take(saddle, ref attack, ref attackHold, ref secondaryAttack, ref secondaryAttackHold, ref jump, ref dodge);
        }
    }

    private static void Take(Sadle saddle, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool jump, ref bool dodge)
    {
        var switches = new MountSwitches(RidingFeature.JumpOn, RidingFeature.AttackOn, RidingFeature.SteeringOn);
        MountActionDecision decision = MountActionRules.Decide(new MountActionInput(jump, attack, secondaryAttack, dodge, switches));
        if (decision.TakeJump) jump = false;
        if (decision.TakeAttack) { attack = false; attackHold = false; }
        if (decision.TakeSecondary) { secondaryAttack = false; secondaryAttackHold = false; }
        if (decision.TakeDodge) dodge = false;

        bool requested = MountActions.Request(MountOf(saddle), ViewOf(saddle), decision.Action);
        ModLog.Verbose($"PlayerSetControlsPatch: {decision} -> requested {requested}, presses left for the game: jump {jump}, attack {attack}, secondary {secondaryAttack}, dodge {dodge}");
    }
}
