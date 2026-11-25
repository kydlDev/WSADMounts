using System.Linq;
using Mono.Cecil;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Integration;

/// <summary>
/// The mod's copies of game enums must match the game (they travel through the game's own RPCs as ints). Read from Claude\ref with
/// Mono.Cecil (metadata only); fails after a game update that renamed or renumbered them.
/// </summary>
[TestFixture]
public class GameEnumsTests
{
    [Test]
    public void RideSpeed_MatchesTheGamesSadleSpeed()
    {
        var game = ModuleDefinition.ReadModule(System.IO.Path.Combine(TestPaths.RefDir, "assembly_valheim.dll"));
        TypeDefinition sadle = game.Types.Single(t => t.Name == "Sadle");
        TypeDefinition speed = sadle.NestedTypes.Single(t => t.Name == "Speed");
        var gameValues = speed.Fields.Where(f => f.IsLiteral).ToDictionary(f => f.Name, f => (int)f.Constant);
        var modValues = System.Enum.GetValues(typeof(RideSpeed)).Cast<RideSpeed>().ToDictionary(v => v.ToString(), v => (int)v);
        Assert.That(modValues, Is.EquivalentTo(gameValues));
    }
}
