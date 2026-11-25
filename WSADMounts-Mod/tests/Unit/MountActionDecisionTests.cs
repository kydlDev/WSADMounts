using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-037: <see cref="MountActionDecision.AnyTaken"/> says whether the game would still see a press that dismounts the rider.</summary>
public class MountActionDecisionTests
{
    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, true, false)]
    [TestCase(false, false, false, true)]
    [TestCase(true, true, true, true)]
    public void AnyTaken_IsTrueWhenAtLeastOnePressIsTaken(bool jump, bool attack, bool secondary, bool dodge)
    {
        var decision = new MountActionDecision(MountActionKind.None, jump, attack, secondary, dodge, "test");

        Assert.That(decision.AnyTaken, Is.True);
    }

    [Test]
    public void AnyTaken_IsFalseWhenNoPressIsTaken()
    {
        var decision = new MountActionDecision(MountActionKind.None, false, false, false, false, "test");

        Assert.That(decision.AnyTaken, Is.False);
    }

    [Test]
    public void ToString_NamesTheActionEveryTakenPressAndTheReason()
    {
        var decision = new MountActionDecision(MountActionKind.Jump, true, false, true, false, "jump key");

        Assert.That(decision.ToString(), Is.EqualTo("action=Jump take(jump=True attack=False secondary=True dodge=False) reason=jump key"));
    }
}
