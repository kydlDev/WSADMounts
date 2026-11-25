namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The speed order a rider sends to a saddled mount. The numbers are the game's own <c>Sadle.Speed</c> values (ilspycmd, Valheim 1.0.21):
/// they travel through the game's <c>Controls</c> RPC as an int, so they must stay equal (tests/Integration/GameEnumsTests.cs compares them with the game).
/// </summary>
internal enum RideSpeed
{
    Stop = 0,
    Walk = 1,
    Run = 2,
    Turn = 3,
    NoChange = 4,
}
