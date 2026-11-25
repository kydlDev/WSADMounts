using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-037: the movement values a ridden mount gets (sprint, stamina, acceleration, turning).</summary>
public class RideTuningRulesTests
{
    // the Lox's own values (rip prefab): acceleration 0.5, turn 70 / 80 deg/s, run 7 m/s; saddle drain 10 per s
    private static readonly RideTuning Lox = new RideTuning(0.5f, 70f, 80f, 7f, 10f);

    [Test]
    public void SprintOn_EveryValueTimesItsFactor()
    {
        RideTuning t = RideTuningRules.Tune(Lox, new RideFactors(1.3f, 0.7f, 3f, 1.8f, true));
        Assert.That(t.Acceleration, Is.EqualTo(1.5f).Within(0.0001f));
        Assert.That(t.TurnSpeed, Is.EqualTo(126f).Within(0.001f));
        Assert.That(t.RunTurnSpeed, Is.EqualTo(144f).Within(0.001f));
        Assert.That(t.RunSpeed, Is.EqualTo(9.1f).Within(0.001f));
        Assert.That(t.RunStaminaDrain, Is.EqualTo(7f).Within(0.001f));
    }

    [Test]
    public void SprintOff_RunValuesStayTheMounts_FeelStillApplies()
    {
        RideTuning t = RideTuningRules.Tune(Lox, new RideFactors(1.3f, 0.7f, 2f, 2f, false));
        Assert.That(t.RunSpeed, Is.EqualTo(7f));
        Assert.That(t.RunStaminaDrain, Is.EqualTo(10f));
        Assert.That(t.Acceleration, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(t.TurnSpeed, Is.EqualTo(140f).Within(0.001f));
    }

    [Test]
    public void FactorOne_TheGamesValues()
    {
        RideTuning t = RideTuningRules.Tune(Lox, new RideFactors(1f, 1f, 1f, 1f, true));
        Assert.That(t.ToString(), Is.EqualTo(Lox.ToString()));
    }

    [Test]
    public void NegativeFactor_CountsAsZero_ZeroStaysZero()
    {
        RideTuning t = RideTuningRules.Tune(Lox, new RideFactors(-1f, -2f, -3f, -4f, true));
        Assert.That(t.RunSpeed, Is.EqualTo(0f));
        Assert.That(t.RunStaminaDrain, Is.EqualTo(0f));
        Assert.That(t.Acceleration, Is.EqualTo(0f));
        Assert.That(t.TurnSpeed, Is.EqualTo(0f));
        Assert.That(t.RunTurnSpeed, Is.EqualTo(0f));

        RideTuning zero = RideTuningRules.Tune(Lox, new RideFactors(0f, 0f, 0f, 0f, true));
        Assert.That(zero.RunSpeed, Is.EqualTo(0f));
        Assert.That(zero.Acceleration, Is.EqualTo(0f));
    }

    [Test]
    public void SameAs_EveryFieldCounts()
    {
        var a = new RideFactors(1.3f, 0.7f, 3f, 1.8f, true);
        Assert.That(a.SameAs(new RideFactors(1.3f, 0.7f, 3f, 1.8f, true)), Is.True);
        Assert.That(a.SameAs(new RideFactors(1.4f, 0.7f, 3f, 1.8f, true)), Is.False);
        Assert.That(a.SameAs(new RideFactors(1.3f, 0.8f, 3f, 1.8f, true)), Is.False);
        Assert.That(a.SameAs(new RideFactors(1.3f, 0.7f, 4f, 1.8f, true)), Is.False);
        Assert.That(a.SameAs(new RideFactors(1.3f, 0.7f, 3f, 1.9f, true)), Is.False);
        Assert.That(a.SameAs(new RideFactors(1.3f, 0.7f, 3f, 1.8f, false)), Is.False);
    }

    [Test]
    public void ToString_ShowsUnits()
    {
        Assert.That(Lox.ToString(), Is.EqualTo("acceleration=0.5 turn=70 deg/s runTurn=80 deg/s run=7 m/s drain=10/s"));
        Assert.That(new RideFactors(1.3f, 0.7f, 3f, 1.8f, true).ToString(), Is.EqualTo("sprint x1.3 stamina x0.7 acceleration x3 turn x1.8 sprintOn=True"));
    }
}
