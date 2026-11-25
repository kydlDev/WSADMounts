using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The answer of <see cref="MountActionRules.Decide"/>: what the mount does, and which presses the mod takes away from the game. A press the
/// mod keeps reaches the game, which dismounts the rider on it (vanilla Player.SetControls).
/// </summary>
internal readonly struct MountActionDecision
{
    internal MountActionDecision(MountActionKind action, bool takeJump, bool takeAttack, bool takeSecondary, bool takeDodge, string reason)
    {
        Action = action;
        TakeJump = takeJump;
        TakeAttack = takeAttack;
        TakeSecondary = takeSecondary;
        TakeDodge = takeDodge;
        Reason = reason;

        if (RuleLog.Due("MountActionDecision.ctor", 2d))
        {
            RuleLog.Verbose($"MountActionDecision: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>What the mount does.</summary>
    internal MountActionKind Action { get; }

    /// <summary>The mod takes the jump press (it does not dismount).</summary>
    internal bool TakeJump { get; }

    /// <summary>The mod takes the attack press (no dismount, the rider does not swing).</summary>
    internal bool TakeAttack { get; }

    /// <summary>The mod takes the secondary attack press.</summary>
    internal bool TakeSecondary { get; }

    /// <summary>The mod takes the dodge press.</summary>
    internal bool TakeDodge { get; }

    /// <summary>The branch that decided (for the log).</summary>
    internal string Reason { get; }

    /// <summary>Whether the game would still see a press that dismounts.</summary>
    internal bool AnyTaken => TakeJump || TakeAttack || TakeSecondary || TakeDodge;

    public override string ToString() =>
        $"action={Action} take(jump={TakeJump} attack={TakeAttack} secondary={TakeSecondary} dodge={TakeDodge}) reason={Reason}";
}
