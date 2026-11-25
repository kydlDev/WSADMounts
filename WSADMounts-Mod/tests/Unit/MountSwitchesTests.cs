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
    public void ToString_NamesEverySwitch()
    {
        Assert.That(new MountSwitches(true, false, true).ToString(), Is.EqualTo("jumpOn=True attackOn=False controlsOn=True"));
    }
}
