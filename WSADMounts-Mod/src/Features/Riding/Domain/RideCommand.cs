using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The rule's answer for one control tick: where the mount should face, where it should move, how fast, and whether it steps backwards.
/// Directions are flat world directions (x, z), normalised, or (0, 0) for "keep the current one".
/// </summary>
internal readonly struct RideCommand
{
    internal RideCommand(float faceX, float faceZ, float moveX, float moveZ, RideSpeed speed, bool back, bool cruising, string reason)
    {
        FaceX = faceX;
        FaceZ = faceZ;
        MoveX = moveX;
        MoveZ = moveZ;
        Speed = speed;
        Back = back;
        Cruising = cruising;
        Reason = reason;

        if (RuleLog.Due("RideCommand.ctor", 2d))
        {
            RuleLog.Verbose($"RideCommand: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>Where the mount should face, world x.</summary>
    internal float FaceX { get; }

    /// <summary>Where the mount should face, world z.</summary>
    internal float FaceZ { get; }

    /// <summary>Where the mount should move, world x (equal to Face unless it steps backwards).</summary>
    internal float MoveX { get; }

    /// <summary>Where the mount should move, world z.</summary>
    internal float MoveZ { get; }

    /// <summary>The speed order (the game's own values).</summary>
    internal RideSpeed Speed { get; }

    /// <summary>The mount steps backwards: it keeps facing <see cref="FaceX"/>/<see cref="FaceZ"/> and moves the other way.</summary>
    internal bool Back { get; }

    /// <summary>Whether the rider is cruising (auto-run) after this tick.</summary>
    internal bool Cruising { get; }

    /// <summary>Which branch decided it (one of the RideSteeringRules reason constants).</summary>
    internal string Reason { get; }

    public override string ToString() =>
        $"{Reason}: speed={Speed} back={Back} face=({FaceX:0.##},{FaceZ:0.##}) move=({MoveX:0.##},{MoveZ:0.##}) cruising={Cruising}";
}
