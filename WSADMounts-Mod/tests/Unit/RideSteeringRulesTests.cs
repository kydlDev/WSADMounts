using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>Every branch of the steering rule (D-004, D-005, D-018), in the rule's priority order.</summary>
public class RideSteeringRulesTests
{
    private const float Tol = 0.0001f;

    private static RideInput Input(float mx, float mz, float lx = 0f, float lz = 1f, bool run = false, bool autoRun = false, bool block = false, bool cruising = false) =>
        new RideInput(mx, mz, lx, lz, run, autoRun, block, cruising);

    [TestCase(0f, 0f)]
    [TestCase(float.NaN, 1f)]
    public void Decide_NoLookDirection_KeepsEverything(float lx, float lz)
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 1f, lx, lz, cruising: true));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.NoLook));
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.NoChange));
        Assert.That(c.Cruising, Is.True, "a broken camera reading must not end cruising");
        Assert.That(c.FaceX, Is.EqualTo(0f));
        Assert.That(c.FaceZ, Is.EqualTo(0f));
    }

    [TestCase(0f, 0f)]
    [TestCase(0.05f, -0.05f)]
    public void Decide_NoInput_Stops(float mx, float mz)
    {
        RideCommand c = RideSteeringRules.Decide(Input(mx, mz));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.Stopped));
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Stop));
        Assert.That(c.Back, Is.False);
        Assert.That(c.MoveX, Is.EqualTo(0f));
        Assert.That(c.MoveZ, Is.EqualTo(0f));
    }

    [Test]
    public void Decide_ForwardKey_WalksWhereTheCameraLooks()
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 1f, 3f, 4f));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.MoveCameraRelative));
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Walk));
        Assert.That(c.FaceX, Is.EqualTo(0.6f).Within(Tol));
        Assert.That(c.FaceZ, Is.EqualTo(0.8f).Within(Tol));
        Assert.That(c.MoveX, Is.EqualTo(0.6f).Within(Tol));
        Assert.That(c.MoveZ, Is.EqualTo(0.8f).Within(Tol));
    }

    [TestCase(1f, 1f, 0f)]    // camera looks +z: D = right = +x
    [TestCase(-1f, -1f, 0f)]  // A = left = -x
    public void Decide_SideKeys_MoveSidewaysOfTheCamera(float mx, float expectedX, float expectedZ)
    {
        RideCommand c = RideSteeringRules.Decide(Input(mx, 0f));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.MoveCameraRelative));
        Assert.That(c.Back, Is.False);
        Assert.That(c.MoveX, Is.EqualTo(expectedX).Within(Tol));
        Assert.That(c.MoveZ, Is.EqualTo(expectedZ).Within(Tol));
        Assert.That(c.FaceX, Is.EqualTo(expectedX).Within(Tol), "a sideways move turns the mount that way");
    }

    [Test]
    public void Decide_RightKey_WithCameraLookingEast_MovesSouth()
    {
        RideCommand c = RideSteeringRules.Decide(Input(1f, 0f, 1f, 0f));
        Assert.That(c.MoveX, Is.EqualTo(0f).Within(Tol));
        Assert.That(c.MoveZ, Is.EqualTo(-1f).Within(Tol));
    }

    [Test]
    public void Decide_Diagonal_IsNormalised()
    {
        RideCommand c = RideSteeringRules.Decide(Input(1f, 1f));
        Assert.That(c.MoveX, Is.EqualTo(0.70710678f).Within(Tol));
        Assert.That(c.MoveZ, Is.EqualTo(0.70710678f).Within(Tol));
    }

    [Test]
    public void Decide_RunWithDirection_Runs()
    {
        RideCommand c = RideSteeringRules.Decide(Input(-1f, 0f, run: true));
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Run));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.MoveCameraRelative));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Decide_BackKey_StepsBackFacingTheCamera_AtWalk(bool run)
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, -1f, 0f, 2f, run: run));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.StepBack));
        Assert.That(c.Back, Is.True);
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Walk), "the back step never runs");
        Assert.That(c.FaceX, Is.EqualTo(0f).Within(Tol));
        Assert.That(c.FaceZ, Is.EqualTo(1f).Within(Tol));
        Assert.That(c.MoveX, Is.EqualTo(0f).Within(Tol));
        Assert.That(c.MoveZ, Is.EqualTo(-1f).Within(Tol));
    }

    [Test]
    public void Decide_BackLeft_StepsBackDiagonally_StillFacingTheCamera()
    {
        RideCommand c = RideSteeringRules.Decide(Input(-1f, -1f));
        Assert.That(c.Back, Is.True);
        Assert.That(c.FaceZ, Is.EqualTo(1f).Within(Tol));
        Assert.That(c.MoveX, Is.EqualTo(-0.70710678f).Within(Tol));
        Assert.That(c.MoveZ, Is.EqualTo(-0.70710678f).Within(Tol));
    }

    [TestCase(-0.31f, true)]
    [TestCase(-0.3f, false)]
    [TestCase(-0.2f, false)]
    public void Decide_StickPulledBack_StepsBackOnlyPastTheThreshold(float mz, bool back)
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, mz));
        Assert.That(c.Back, Is.EqualTo(back));
    }

    [Test]
    public void Decide_RunAlone_RunsAhead()
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 0f, run: true));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.RunAhead));
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Run));
        Assert.That(c.MoveZ, Is.EqualTo(1f).Within(Tol));
    }

    [Test]
    public void Decide_BlockAlone_TurnsInPlace()
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 0f, block: true));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.TurnInPlace));
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Turn));
        Assert.That(c.FaceZ, Is.EqualTo(1f).Within(Tol));
        Assert.That(c.MoveX, Is.EqualTo(0f));
        Assert.That(c.MoveZ, Is.EqualTo(0f));
    }

    [Test]
    public void Decide_AutoRunPress_StartsCruising()
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 0f, autoRun: true));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.Cruise));
        Assert.That(c.Cruising, Is.True);
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Walk));
    }

    [Test]
    public void Decide_Cruising_KeepsGoing_AndRunsWithTheRunKey()
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 0f, run: true, cruising: true));
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.Cruise));
        Assert.That(c.Cruising, Is.True);
        Assert.That(c.Speed, Is.EqualTo(RideSpeed.Run));
    }

    [Test]
    public void Decide_AutoRunPressWhileCruising_Stops()
    {
        RideCommand c = RideSteeringRules.Decide(Input(0f, 0f, autoRun: true, cruising: true));
        Assert.That(c.Cruising, Is.False);
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.Stopped));
    }

    [Test]
    public void Decide_DirectionKeyWhileCruising_EndsCruising()
    {
        RideCommand c = RideSteeringRules.Decide(Input(1f, 0f, cruising: true));
        Assert.That(c.Cruising, Is.False);
        Assert.That(c.Reason, Is.EqualTo(RideSteeringRules.MoveCameraRelative));
    }

    // Mutation-driven checks (Stryker, 2026-11-25): every branch sets Back and Cruising explicitly.
    [Test]
    public void Decide_EveryBranch_SetsBackAndCruisingAsDocumented()
    {
        RideCommand noLook = RideSteeringRules.Decide(Input(0f, 1f, 0f, 0f));
        RideCommand back = RideSteeringRules.Decide(Input(0f, -1f));
        RideCommand cruise = RideSteeringRules.Decide(Input(0f, 0f, cruising: true));
        RideCommand runAhead = RideSteeringRules.Decide(Input(0f, 0f, run: true));
        RideCommand turn = RideSteeringRules.Decide(Input(0f, 0f, block: true));
        Assert.Multiple(() =>
        {
            Assert.That(noLook.Back, Is.False, "no look");
            Assert.That(back.Back && !back.Cruising, Is.True, "back step");
            Assert.That(!cruise.Back && cruise.Cruising, Is.True, "cruise");
            Assert.That(!runAhead.Back && !runAhead.Cruising, Is.True, "run ahead");
            Assert.That(!turn.Back && !turn.Cruising, Is.True, "turn in place");
        });
    }

    [Test]
    public void Command_And_Input_DescribeThemselves_ForTheLog()
    {
        RideInput input = Input(0f, -1f);
        RideCommand c = RideSteeringRules.Decide(input);
        Assert.Multiple(() =>
        {
            Assert.That(c.ToString(), Does.StartWith(RideSteeringRules.StepBack).And.Contain("speed=Walk back=True"));
            Assert.That(input.ToString(), Does.Contain("move=(0,-1)"));
        });
    }
}
