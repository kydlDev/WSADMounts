using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>The ride-feel factors (dev Tunables, D-037) and whether the faster sprint is on; equal factors = nothing to re-apply.</summary>
internal readonly struct RideFactors
{
    internal RideFactors(float sprintSpeed, float sprintStamina, float acceleration, float turn, bool sprintOn)
    {
        SprintSpeed = sprintSpeed;
        SprintStamina = sprintStamina;
        Acceleration = acceleration;
        Turn = turn;
        SprintOn = sprintOn;

        if (RuleLog.Due("RideFactors.ctor", 2d))
        {
            RuleLog.Verbose($"RideFactors: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>Run speed x this factor while ridden.</summary>
    internal float SprintSpeed { get; }

    /// <summary>Saddle run stamina drain x this factor while ridden.</summary>
    internal float SprintStamina { get; }

    /// <summary>Acceleration x this factor while ridden.</summary>
    internal float Acceleration { get; }

    /// <summary>Walk and run turn rates x this factor while ridden.</summary>
    internal float Turn { get; }

    /// <summary>[General] Enable Mount Sprint (off: the mount does not run at all, so the run values are left as they are).</summary>
    internal bool SprintOn { get; }

    /// <summary>Same factors (the applied values need no update).</summary>
    internal bool SameAs(RideFactors other) =>
        SprintSpeed.Equals(other.SprintSpeed) && SprintStamina.Equals(other.SprintStamina) && Acceleration.Equals(other.Acceleration)
        && Turn.Equals(other.Turn) && SprintOn == other.SprintOn;

    public override string ToString() =>
        $"sprint x{SprintSpeed:0.##} stamina x{SprintStamina:0.##} acceleration x{Acceleration:0.##} turn x{Turn:0.##} sprintOn={SprintOn}";
}
