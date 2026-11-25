namespace WSADMounts.Features.Riding.Domain;

/// <summary>What a back-stepping mount shows (D-032).</summary>
internal enum BackStepLook
{
    /// <summary>The mount's own walk clip played backwards.</summary>
    Moonwalk = 0,

    /// <summary>The mod's hand-made backward walk clip (Lox, Asksvin, Moose).</summary>
    BackwardWalk = 1,
}
