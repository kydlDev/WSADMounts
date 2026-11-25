using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The rider's presses in one control frame and the player's switches, copied out of the game (no game types, so the rule is unit-tested).
/// The presses are the game's button-down edges (Player.SetControls: jump, attack, secondaryAttack, dodge).
/// </summary>
internal readonly struct MountActionInput
{
    internal MountActionInput(bool jump, bool attack, bool secondary, bool dodge, MountSwitches switches)
    {
        Jump = jump;
        Attack = attack;
        Secondary = secondary;
        Dodge = dodge;
        Switches = switches;

        if (RuleLog.Due("MountActionInput.ctor", 2d))
        {
            RuleLog.Verbose($"MountActionInput: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>The jump button was pressed (Space / controller jump).</summary>
    internal bool Jump { get; }

    /// <summary>The attack button was pressed.</summary>
    internal bool Attack { get; }

    /// <summary>The secondary attack button was pressed.</summary>
    internal bool Secondary { get; }

    /// <summary>The dodge button was pressed.</summary>
    internal bool Dodge { get; }

    /// <summary>Which parts of the mod are on for this player.</summary>
    internal MountSwitches Switches { get; }

    /// <summary>Any of the four presses (the patch returns at once without one).</summary>
    internal bool AnyPress => Jump || Attack || Secondary || Dodge;

    public override string ToString() => $"jump={Jump} attack={Attack} secondary={Secondary} dodge={Dodge} {Switches}";
}
