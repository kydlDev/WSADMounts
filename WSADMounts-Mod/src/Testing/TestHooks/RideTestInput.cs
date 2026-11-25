using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TEST ONLY: a move input the lab hands to the rider's control step instead of the keys / stick (the game calls Sadle.ApplyControlls every frame
/// with the real input, so a single hook call would be undone the next frame). Inert unless a test arms it; it expires by itself after the armed
/// seconds and is disarmed by WSADMounts.rideReset. Lets the lab feed stick-like values (z = -0.5) the keyboard cannot make.
/// </summary>
internal static class RideTestInput
{
    private static float _until = -1f;
    private static Vector3 _move;
    private static bool _run;

    /// <summary>True while armed (Time.time before the expiry).</summary>
    internal static bool Armed => Time.time < _until;

    internal static void Arm(float x, float z, float seconds, bool run)
    {
        _move = new Vector3(x, 0f, z);
        _run = run;
        _until = Time.time + seconds;
        ModLog.Info($"RideTestInput: armed move=({x:0.##},{z:0.##}) run={run} for {seconds:0.#} s");
    }

    internal static void Disarm(string why)
    {
        bool was = Armed;
        _until = -1f;
        ModLog.Info($"RideTestInput: disarmed ({why}, was armed {was})");
    }

    /// <summary>The rider's control step: replaces move / run while armed. Expected outcome: true when the test input was used.</summary>
    internal static bool Apply(ref Vector3 moveDir, ref bool run)
    {
        bool armed = Armed;
        if (armed)
        {
            moveDir = _move;
            run = _run;
            ModLog.Every("RideTestInput.Apply", 1d, $"RideTestInput: test input move=({_move.x:0.##},{_move.z:0.##}) run={_run}, {_until - Time.time:0.0} s left");
        }

        return armed;
    }
}
