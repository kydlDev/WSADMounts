using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>The ride-feel factors (dev Tunables, D-037) and whether the faster sprint is on; equal factors = nothing to re-apply.</summary>
internal readonly struct RideFactors
{
    internal RideFactors(float sprintSpeed, float sprintStamina, float acceleration, float turn, float jump, bool sprintOn)
    {
        SprintSpeed = sprintSpeed;
        SprintStamina = sprintStamina;
        Acceleration = acceleration;
        Turn = turn;
        Jump = jump;
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

    /// <summary>Jump force x this factor while ridden (D-039; the height grows with its square).</summary>
    internal float Jump { get; }

    /// <summary>[General] Enable Mount Sprint (off: the mount does not run at all, so the run values are left as they are).</summary>
    internal bool SprintOn { get; }

    /// <summary>Same factors (the applied values need no update).</summary>
    internal bool SameAs(RideFactors other) =>
        Near(SprintSpeed, other.SprintSpeed) && Near(SprintStamina, other.SprintStamina) && Near(Acceleration, other.Acceleration)
        && Near(Turn, other.Turn) && Near(Jump, other.Jump) && SprintOn == other.SprintOn;

    // A range, not exact float equality (Sonar S1244): factors closer than this are the same setting.
    // Stryker disable once Equality : a difference of exactly 0.00001 is neither a real setting change nor reachable from the config (steps are 0.01 or more)
    private static bool Near(float a, float b) => System.Math.Abs(a - b) < 0.00001f;

    public override string ToString() =>
        $"sprint x{SprintSpeed:0.##} stamina x{SprintStamina:0.##} acceleration x{Acceleration:0.##} turn x{Turn:0.##} jump x{Jump:0.##} sprintOn={SprintOn}";
}
