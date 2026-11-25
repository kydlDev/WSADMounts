namespace WSADMounts.Features.Riding.Domain;

/// <summary>What a rider's button press makes the mount do (D-037). The int values travel in the RPC WSADMounts_MountAction_v1.</summary>
internal enum MountActionKind
{
    /// <summary>Nothing for the mount (the press may still be kept from dismounting the rider).</summary>
    None = 0,

    /// <summary>The mount jumps (Space while riding).</summary>
    Jump = 1,

    /// <summary>The mount's light attack: its first attack (Lox Bite, Moose Horns, Asksvin Bite).</summary>
    LightAttack = 2,

    /// <summary>The next of the mount's other attacks, in turn (Lox Stomp; Moose Hooves, Horn sweep; Asksvin Headbutt, Pounce).</summary>
    HeavyAttack = 3,
}
