namespace WSADMounts.Features.Riding.Domain;

/// <summary>How a mount looks while it walks backwards (D-013, D-035): the player's drop-down; other players see the rider's choice (D-012).</summary>
internal enum BackwardAnimation
{
    /// <summary>The mod's hand-made backward walk (Lox, Asksvin, Moose; every other mount falls back to <see cref="ReversedWalk"/>, D-035). Default.</summary>
    BackwardWalk = 0,

    /// <summary>The game's own walk clip played in reverse (D-032 / D-035).</summary>
    ReversedWalk = 1,

    /// <summary>The game's forward walk keeps playing while the mount moves backwards (D-035, the look of D-023).</summary>
    Moonwalk = 2,
}
