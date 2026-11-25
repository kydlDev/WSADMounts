using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// What the rider's client knows in one control tick, copied out of the game (no game types, so the rule is unit-tested).
/// Move is the game's own move input (keys or the left stick: x = right, z = forward, each -1..1); Look is the camera's look direction.
/// </summary>
internal readonly struct RideInput
{
    internal RideInput(float moveX, float moveZ, float lookX, float lookZ, bool run, bool autoRunPressed, bool block, bool cruising)
    {
        MoveX = moveX;
        MoveZ = moveZ;
        LookX = lookX;
        LookZ = lookZ;
        Run = run;
        AutoRunPressed = autoRunPressed;
        Block = block;
        Cruising = cruising;

        if (RuleLog.Due("RideInput.ctor", 2d))
        {
            RuleLog.Verbose($"RideInput: {RuleLog.Shown(this)} (one sample every 2 s)");
        }
    }

    /// <summary>Sideways input, -1 (left) .. 1 (right).</summary>
    internal float MoveX { get; }

    /// <summary>Forward input, -1 (back) .. 1 (forward).</summary>
    internal float MoveZ { get; }

    /// <summary>Camera look direction, world x (any length, the rule flattens and normalises it).</summary>
    internal float LookX { get; }

    /// <summary>Camera look direction, world z.</summary>
    internal float LookZ { get; }

    /// <summary>The run key is held.</summary>
    internal bool Run { get; }

    /// <summary>The auto-run key was pressed in this tick (an edge, the game passes it once per press).</summary>
    internal bool AutoRunPressed { get; }

    /// <summary>The block key is held (vanilla: turn in place).</summary>
    internal bool Block { get; }

    /// <summary>Whether the rider was cruising (auto-run) before this tick.</summary>
    internal bool Cruising { get; }

    public override string ToString() =>
        $"move=({MoveX:0.##},{MoveZ:0.##}) look=({LookX:0.##},{LookZ:0.##}) run={Run} autoRunPressed={AutoRunPressed} block={Block} cruising={Cruising}";
}
