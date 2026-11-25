namespace WSADMounts.Features.Riding.Domain;

/// <summary>How a mount looks while it walks backwards (D-013): the player's drop-down; other players see the rider's choice (D-012).</summary>
internal enum BackwardAnimation
{
    /// <summary>A real backward walk (the mod's clips for Lox, Asksvin and Moose; every other mount falls back to <see cref="Moonwalk"/>, D-014).</summary>
    BackwardWalk = 0,

    /// <summary>The normal forward walk while the mount moves backwards.</summary>
    Moonwalk = 1,
}
