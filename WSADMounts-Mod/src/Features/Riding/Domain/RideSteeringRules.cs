using System;
using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The steering rule of a ridden mount with the custom controls on (D-004, D-005, D-018): direction keys / the stick move the mount
/// camera-relative like walking on foot, backwards input steps it backwards while it keeps facing the camera direction, releasing every
/// key stops it, auto-run cruises ahead until a direction key is used, run alone runs ahead and block alone turns in place (vanilla).
/// Pure: facts in, decision out (unit-tested in tests/Unit/RideSteeringRulesTests.cs). Runs every control tick: its debug lines are throttled
/// (PROJECT_RULES 38), except the line that says the decided branch changed.
/// </summary>
internal static class RideSteeringRules
{
    /// <summary>Stick input below this length counts as no input (a resting controller stick drifts a little).</summary>
    internal const float DeadZone = 0.1f;

    /// <summary>Forward input below minus this value is a backward step (keys give -1; a stick pulled back a third of the way).</summary>
    internal const float BackThreshold = 0.3f;

    /// <summary>Seconds between two debug lines of the same kind while the branch stays the same.</summary>
    internal const double LogEverySeconds = 2d;

    internal const string NoLook = "no look direction", Stopped = "no input, stop", Cruise = "cruising ahead",
        RunAhead = "run key alone, run ahead", TurnInPlace = "block alone, turn in place", StepBack = "backward input, step back",
        MoveCameraRelative = "direction input, move camera-relative";

    private static string _lastReason = string.Empty;

    /// <summary>
    /// Expected outcome: one <see cref="RideCommand"/> per branch (the Reason names it). A zero look direction gives NoChange, so a broken
    /// camera reading never turns the mount.
    /// </summary>
    internal static RideCommand Decide(RideInput input)
    {
        RideCommand result;
        float lookLength = (float)Math.Sqrt((input.LookX * input.LookX) + (input.LookZ * input.LookZ));
        if (lookLength < 0.0001f || float.IsNaN(lookLength))
        {
            result = new RideCommand(0f, 0f, 0f, 0f, RideSpeed.NoChange, false, input.Cruising, NoLook);
        }
        else
        {
            float fx = input.LookX / lookLength;
            float fz = input.LookZ / lookLength;
            float moveLength = (float)Math.Sqrt((input.MoveX * input.MoveX) + (input.MoveZ * input.MoveZ));
            bool hasMove = moveLength >= DeadZone;
            bool cruising = Cruising(input, hasMove);
            result = hasMove ? WithMove(input, fx, fz) : NoMove(input, fx, fz, cruising);
        }

        bool changed = !string.Equals(result.Reason, _lastReason, StringComparison.Ordinal);
        _lastReason = result.Reason;
        if (changed || RuleLog.Due("RideSteeringRules.Decide", LogEverySeconds)) { RuleLog.Verbose($"RideSteeringRules.Decide: {(changed ? $"branch {_lastReason} -> {result.Reason}" : "same branch")} - input {RuleLog.Shown(input)} -> {RuleLog.Shown(result)}"); }
        return result;
    }

    /// <summary>Auto-run on foot: a press toggles it, any direction input ends it. Expected outcome: the cruise state after this tick.</summary>
    private static bool Cruising(RideInput input, bool hasMove)
    {
        bool next = !hasMove && (input.AutoRunPressed ? !input.Cruising : input.Cruising);
        if (next != input.Cruising) { RuleLog.Verbose($"RideSteeringRules.Cruising: {input.Cruising} -> {next} (hasMove={hasMove}, autoRunPressed={input.AutoRunPressed})"); }
        return next;
    }

    /// <summary>A direction is held: camera-relative like the player on foot (move.z * look + move.x * right, right = (look.z, -look.x)).</summary>
    private static RideCommand WithMove(RideInput input, float fx, float fz)
    {
        float wx = (input.MoveZ * fx) + (input.MoveX * fz);
        float wz = (input.MoveZ * fz) - (input.MoveX * fx);
        float worldLength = (float)Math.Sqrt((wx * wx) + (wz * wz));
        wx /= worldLength;
        wz /= worldLength;
        bool back = input.MoveZ < -BackThreshold;
        RideSpeed speed = !back && input.Run ? RideSpeed.Run : RideSpeed.Walk;
        RideCommand result = back
            ? new RideCommand(fx, fz, wx, wz, speed, true, false, StepBack)
            : new RideCommand(wx, wz, wx, wz, speed, false, false, MoveCameraRelative);
        if (RuleLog.Due("RideSteeringRules.WithMove", LogEverySeconds)) { RuleLog.Verbose($"RideSteeringRules.WithMove: world move ({wx:0.###},{wz:0.###}) from length {worldLength:0.###}, back={back} (moveZ {input.MoveZ:0.##} vs -{BackThreshold})"); }
        return result;
    }

    /// <summary>No direction is held: cruise, run ahead, turn in place or stop, in that order.</summary>
    private static RideCommand NoMove(RideInput input, float fx, float fz, bool cruising)
    {
        RideCommand result;
        if (cruising)
        {
            result = new RideCommand(fx, fz, fx, fz, input.Run ? RideSpeed.Run : RideSpeed.Walk, false, true, Cruise);
        }
        else if (input.Run)
        {
            result = new RideCommand(fx, fz, fx, fz, RideSpeed.Run, false, false, RunAhead);
        }
        else if (input.Block)
        {
            result = new RideCommand(fx, fz, 0f, 0f, RideSpeed.Turn, false, false, TurnInPlace);
        }
        else
        {
            result = new RideCommand(0f, 0f, 0f, 0f, RideSpeed.Stop, false, false, Stopped);
        }

        if (RuleLog.Due("RideSteeringRules.NoMove", LogEverySeconds)) { RuleLog.Verbose($"RideSteeringRules.NoMove: cruising={cruising} run={input.Run} block={input.Block} -> {result.Reason}"); }
        return result;
    }
}
