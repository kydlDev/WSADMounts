using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-037: the three switches that decide the mount actions keep what they were given.</summary>
public class MountSwitchesTests
{
    [TestCase(true, false, true)]
    [TestCase(false, true, false)]
    public void Switches_KeepTheirValues(bool jump, bool attack, bool controls)
    {
        var switches = new MountSwitches(jump, attack, controls);

        Assert.Multiple(() =>
        {
            Assert.That(switches.JumpOn, Is.EqualTo(jump));
            Assert.That(switches.AttackOn, Is.EqualTo(attack));
            Assert.That(switches.ControlsOn, Is.EqualTo(controls));
        });
    }

    [Test]
    public void Constructor_WhenDebugLoggingIsOn_LogsTheValuesItWasGiven()
    {
        var lines = new System.Collections.Generic.List<string>();
        var oldSink = WSADMounts.Core.RuleLog.Sink;
        var oldOn = WSADMounts.Core.RuleLog.DebugOn;
        WSADMounts.Core.RuleLog.Sink = lines.Add;
        WSADMounts.Core.RuleLog.DebugOn = () => true;
        try
        {
            _ = new MountSwitches(true, false, true);
        }
        finally
        {
            WSADMounts.Core.RuleLog.Sink = oldSink;
            WSADMounts.Core.RuleLog.DebugOn = oldOn;
        }

        Assert.That(lines, Has.Some.Contains("jumpOn=True attackOn=False controlsOn=True"));
    }

    [Test]
    public void ToString_NamesEverySwitch()
    {
        Assert.That(new MountSwitches(true, false, true).ToString(), Is.EqualTo("jumpOn=True attackOn=False controlsOn=True"));
    }
}
