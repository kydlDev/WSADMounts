using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The player's switches that decide the mount actions (D-037), each already combined with Enable Mod: [General] Enable Mount Jump,
/// Enable Mount Attack and Enable Custom Controls (dodge never dismounts while the custom controls are on).
/// </summary>
internal readonly struct MountSwitches
{
    internal MountSwitches(bool jump, bool attack, bool controls)
    {
        JumpOn = jump;
        AttackOn = attack;
        ControlsOn = controls;

        if (RuleLog.Due("MountSwitches.ctor", 2d))
        {
            RuleLog.Verbose($"MountSwitches: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>Mount jump is on.</summary>
    internal bool JumpOn { get; }

    /// <summary>Mount attack is on.</summary>
    internal bool AttackOn { get; }

    /// <summary>The player's custom controls are on.</summary>
    internal bool ControlsOn { get; }

    public override string ToString() => $"jumpOn={JumpOn} attackOn={AttackOn} controlsOn={ControlsOn}";
}
