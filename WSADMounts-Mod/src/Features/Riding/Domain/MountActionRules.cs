using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// Which mount action a rider's press starts, and which presses no longer dismount (D-037, user 2026-11-25): Space = the mount jumps,
/// primary attack = its light attack, secondary attack = its other attacks in turn, dodge does nothing; dismount stays on Use (E), which the
/// mod never touches. A part that is switched off leaves its press to the game, which dismounts on it as in the base game.
/// Pure: facts in, decision out (unit-tested in tests/Unit/MountActionRulesTests.cs).
/// </summary>
internal static class MountActionRules
{
    internal const string NoPress = "no press", Attack = "attack: light attack", Secondary = "secondary attack: next heavy attack",
        Jump = "jump: the mount jumps", DodgeOnly = "dodge: no dismount, nothing else", Game = "switched off: the game's dismount";

    /// <summary>The index of the light attack among the mount's attacks (its first attack item).</summary>
    internal const int LightIndex = 0;

    /// <summary>
    /// Expected outcome: the action of the first press that a switched-on part uses, in the order attack, secondary attack, jump (one action
    /// per frame; the others are still taken so they do not dismount), and every press of a switched-on part taken.
    /// </summary>
    internal static MountActionDecision Decide(MountActionInput input)
    {
        MountSwitches on = input.Switches;
        bool takeAttack = input.Attack && on.AttackOn;
        bool takeSecondary = input.Secondary && on.AttackOn;
        bool takeJump = input.Jump && on.JumpOn;
        bool takeDodge = input.Dodge && on.ControlsOn;

        MountActionKind action;
        string reason;
        if (takeAttack) { action = MountActionKind.LightAttack; reason = Attack; }
        else if (takeSecondary) { action = MountActionKind.HeavyAttack; reason = Secondary; }
        else if (takeJump) { action = MountActionKind.Jump; reason = Jump; }
        else if (takeDodge) { action = MountActionKind.None; reason = DodgeOnly; }
        else { action = MountActionKind.None; reason = input.AnyPress ? Game : NoPress; }

        var result = new MountActionDecision(action, takeJump, takeAttack, takeSecondary, takeDodge, reason);
        // Stryker disable once all : only decides whether the debug line is written
        if (input.AnyPress && RuleLog.IsOn()) RuleLog.Verbose($"MountActionRules.Decide: {RuleLog.Shown(input)} -> {RuleLog.Shown(result)}");
        return result;
    }

    /// <summary>
    /// The attack a heavy press fires, among <paramref name="count"/> attacks: the attacks after the light one in turn, starting again after the
    /// last. Expected outcome: index 1..count-1 after <paramref name="last"/>; the light attack (0) when the mount has only one attack; -1 for none.
    /// </summary>
    internal static int NextHeavy(int count, int last)
    {
        int result;
        if (count <= 0) result = -1;
        else if (count == 1) result = LightIndex;
        else result = last >= 1 && last < count - 1 ? last + 1 : 1;

        // Stryker disable once all : only decides whether the debug line is written
        if (RuleLog.IsOn()) RuleLog.Verbose($"MountActionRules.NextHeavy(count={count}, last={last}) -> {result}");
        return result;
    }
}
