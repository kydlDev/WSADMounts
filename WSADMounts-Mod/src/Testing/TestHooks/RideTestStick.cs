using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TEST ONLY: a fake controller left stick (D-024, the user has no controller). The game's PlayerController adds ZInput.GetJoyLeftStick() to the
/// move input, so a faked stick runs the whole real controller path (Player -> Sadle.ApplyControlls -> the mod), unlike <see cref="RideTestInput"/>.
/// Inert unless a test arms it (hook WSADMounts.stick); expires by itself and is disarmed by WSADMounts.rideReset.
/// </summary>
internal static class RideTestStick
{
    private static float _until = -1f;
    private static Vector2 _stick;

    internal static bool Armed => Time.time < _until;

    internal static void Arm(float x, float y, float seconds)
    {
        _stick = new Vector2(x, y);
        _until = Time.time + seconds;
        ModLog.Info($"RideTestStick: fake left stick ({x:0.##},{y:0.##}) for {seconds:0.#} s");
    }

    internal static void Disarm(string why)
    {
        bool was = Armed;
        _until = -1f;
        ModLog.Info($"RideTestStick: disarmed ({why}, was armed {was})");
    }

    /// <summary>The faked stick while armed. Expected outcome: true when the stick value was replaced.</summary>
    internal static bool Apply(ref Vector2 stick)
    {
        bool armed = Armed;
        if (armed)
        {
            stick = _stick;
            ModLog.Every("RideTestStick.Apply", 1d, $"RideTestStick: left stick faked ({_stick.x:0.##},{_stick.y:0.##}), {_until - Time.time:0.0} s left");
        }

        return armed;
    }
}
