namespace WSADMounts.Features.Riding.Domain;

/// <summary>What a back-stepping mount shows (D-032, D-035).</summary>
internal enum BackStepLook
{
    /// <summary>The mount's own walk clip played backwards.</summary>
    ReversedWalk = 0,

    /// <summary>The mod's hand-made backward walk clip (Lox, Asksvin, Moose).</summary>
    BackwardWalk = 1,

    /// <summary>No clip of the mod: the game's forward walk keeps playing (its forward_speed is kept positive, D-023).</summary>
    Moonwalk = 2,
}
