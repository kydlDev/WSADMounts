using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-037: what a rider's jump / attack / secondary / dodge press does, and which presses no longer dismount.</summary>
public class MountActionRulesTests
{
    private static MountActionDecision Decide(bool jump, bool attack, bool secondary, bool dodge, bool jumpOn = true, bool attackOn = true, bool controlsOn = true) =>
        MountActionRules.Decide(new MountActionInput(jump, attack, secondary, dodge, new MountSwitches(jumpOn, attackOn, controlsOn)));

    [Test]
    public void NoPress_NothingTaken()
    {
        MountActionDecision d = Decide(false, false, false, false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.None));
        Assert.That(d.AnyTaken, Is.False);
        Assert.That(d.Reason, Is.EqualTo(MountActionRules.NoPress));
    }

    [Test]
    public void Jump_On_MountJumps_NoDismount()
    {
        MountActionDecision d = Decide(true, false, false, false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.Jump));
        Assert.That(d.TakeJump, Is.True);
        Assert.That(d.Reason, Is.EqualTo(MountActionRules.Jump));
    }

    [Test]
    public void Jump_Off_LeftToTheGame_WhichDismounts()
    {
        MountActionDecision d = Decide(true, false, false, false, jumpOn: false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.None));
        Assert.That(d.TakeJump, Is.False);
        Assert.That(d.Reason, Is.EqualTo(MountActionRules.Game));
    }

    [Test]
    public void Attack_On_LightAttack()
    {
        MountActionDecision d = Decide(false, true, false, false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.LightAttack));
        Assert.That(d.TakeAttack, Is.True);
        Assert.That(d.TakeSecondary, Is.False);
    }

    [Test]
    public void Secondary_On_HeavyAttack()
    {
        MountActionDecision d = Decide(false, false, true, false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.HeavyAttack));
        Assert.That(d.TakeSecondary, Is.True);
        Assert.That(d.TakeAttack, Is.False);
    }

    [Test]
    public void Attacks_Off_LeftToTheGame()
    {
        MountActionDecision d = Decide(false, true, true, false, attackOn: false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.None));
        Assert.That(d.TakeAttack, Is.False);
        Assert.That(d.TakeSecondary, Is.False);
    }

    [Test]
    public void Dodge_ControlsOn_TakenWithoutAction_ControlsOff_LeftToTheGame()
    {
        MountActionDecision on = Decide(false, false, false, true);
        Assert.That(on.Action, Is.EqualTo(MountActionKind.None));
        Assert.That(on.TakeDodge, Is.True);
        Assert.That(on.Reason, Is.EqualTo(MountActionRules.DodgeOnly));

        MountActionDecision off = Decide(false, false, false, true, controlsOn: false);
        Assert.That(off.TakeDodge, Is.False);
        Assert.That(off.Reason, Is.EqualTo(MountActionRules.Game));
    }

    [Test]
    public void SeveralPresses_OneAction_AttackFirst_AllTaken()
    {
        MountActionDecision d = Decide(true, true, true, true);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.LightAttack));
        Assert.That(d.TakeJump && d.TakeAttack && d.TakeSecondary && d.TakeDodge, Is.True);

        MountActionDecision jumpAndHeavy = Decide(true, false, true, false);
        Assert.That(jumpAndHeavy.Action, Is.EqualTo(MountActionKind.HeavyAttack));
        Assert.That(jumpAndHeavy.TakeJump, Is.True);
    }

    [Test]
    public void JumpOff_AttackOn_OnlyAttackTaken()
    {
        MountActionDecision d = Decide(true, true, false, false, jumpOn: false);
        Assert.That(d.Action, Is.EqualTo(MountActionKind.LightAttack));
        Assert.That(d.TakeJump, Is.False);
        Assert.That(d.TakeAttack, Is.True);
    }

    // Lox: 2 attacks (Bite, Stomp); Moose / Asksvin: 3 (light + 2 heavy)
    [TestCase(2, 0, 1)]
    [TestCase(2, 1, 1)]
    [TestCase(3, 0, 1)]
    [TestCase(3, 1, 2)]
    [TestCase(3, 2, 1)]
    [TestCase(4, 3, 1)]
    [TestCase(3, 7, 1)]
    [TestCase(3, -1, 1)]
    [TestCase(1, 0, 0)]
    [TestCase(0, 0, -1)]
    [TestCase(-2, 0, -1)]
    public void NextHeavy_CyclesThroughTheAttacksAfterTheLightOne(int count, int last, int expected)
    {
        Assert.That(MountActionRules.NextHeavy(count, last), Is.EqualTo(expected));
    }

    [Test]
    public void Decision_ToString_NamesActionAndTakenPresses()
    {
        string text = Decide(true, false, false, false).ToString();
        Assert.That(text, Does.Contain("action=Jump"));
        Assert.That(text, Does.Contain("jump=True"));
        Assert.That(new MountActionInput(true, false, true, false, new MountSwitches(true, false, true)).ToString(),
            Is.EqualTo("jump=True attack=False secondary=True dodge=False jumpOn=True attackOn=False controlsOn=True"));
    }
}
