using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The movement values of a mount that the ride feel changes (D-037): the game's Character m_acceleration, m_turnSpeed, m_runTurnSpeed,
/// m_runSpeed, m_jumpForce and the saddle's m_runStaminaDrain. Used for the prefab's own values and for the values applied while it is ridden.
/// </summary>
internal readonly struct RideTuning
{
    internal RideTuning(float acceleration, float turnSpeed, float runTurnSpeed, float runSpeed, float runStaminaDrain, float jumpForce)
    {
        Acceleration = acceleration;
        TurnSpeed = turnSpeed;
        RunTurnSpeed = runTurnSpeed;
        RunSpeed = runSpeed;
        RunStaminaDrain = runStaminaDrain;
        JumpForce = jumpForce;

        if (RuleLog.Due("RideTuning.ctor", 2d))
        {
            RuleLog.Verbose($"RideTuning: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>How fast the mount reaches its speed (the game's m_acceleration, 0.5 on Lox / Asksvin / Moose).</summary>
    internal float Acceleration { get; }

    /// <summary>Turn rate while walking, degrees per second.</summary>
    internal float TurnSpeed { get; }

    /// <summary>Turn rate while running, degrees per second.</summary>
    internal float RunTurnSpeed { get; }

    /// <summary>Run speed, metres per second.</summary>
    internal float RunSpeed { get; }

    /// <summary>Saddle stamina used per second of running.</summary>
    internal float RunStaminaDrain { get; }

    /// <summary>Upward jump speed, m/s (the game's m_jumpForce, 10 on Lox / Asksvin / Moose: the Lox rises 2.4 m).</summary>
    internal float JumpForce { get; }

    public override string ToString() =>
        $"acceleration={Acceleration:0.##} turn={TurnSpeed:0.#} deg/s runTurn={RunTurnSpeed:0.#} deg/s run={RunSpeed:0.##} m/s drain={RunStaminaDrain:0.##}/s jump={JumpForce:0.##} m/s";
}
