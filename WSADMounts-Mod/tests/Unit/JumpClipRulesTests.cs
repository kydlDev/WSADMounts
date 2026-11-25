using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-040: which mount has a jump clip, and where the clip is while the mount takes off, flies and lands.</summary>
public class JumpClipRulesTests
{
    [TestCase("Lox(Clone)", "WSADMounts_Lox_Jump")]
    [TestCase("Asksvin", "WSADMounts_Asksvin_Jump")]
    [TestCase("Moose(Clone)", "WSADMounts_Moose_Jump")]
    [TestCase("SomeModdedHorse(Clone)", "")]
    [TestCase("", "")]
    public void ClipName_OnlyTheGamesThreeMounts(string prefab, string expected)
    {
        Assert.That(JumpClipRules.ClipName(prefab), Is.EqualTo(expected));
    }

    [Test]
    public void TakeOff_PlaysOnTheGround()
    {
        Assert.That(JumpClipRules.NextTime(0.1f, 0.1f, true, false, 0.1f), Is.EqualTo(0.2f).Within(1e-5f));
    }

    [Test]
    public void InTheAir_PlaysUntilTheHoldPose_ThenHolds()
    {
        Assert.That(JumpClipRules.NextTime(0.5f, 0.1f, false, true, 0.5f), Is.EqualTo(0.6f).Within(1e-5f));
        Assert.That(JumpClipRules.NextTime(JumpClipRules.HoldTime - 0.01f, 0.1f, false, true, 1.2f), Is.EqualTo(JumpClipRules.HoldTime).Within(1e-5f));
        Assert.That(JumpClipRules.NextTime(JumpClipRules.HoldTime, 0.1f, false, true, 2.5f), Is.EqualTo(JumpClipRules.HoldTime).Within(1e-5f));
    }

    [Test]
    public void Landing_EarlyOrLate_GoesToTheTouchDownFrame()
    {
        Assert.That(JumpClipRules.NextTime(0.6f, 0.02f, true, true, 0.6f), Is.EqualTo(JumpClipRules.TouchTime).Within(1e-5f));
        Assert.That(JumpClipRules.NextTime(JumpClipRules.HoldTime, 0.02f, true, true, 2f), Is.EqualTo(JumpClipRules.TouchTime).Within(1e-5f));
    }

    [Test]
    public void OnTheGroundBeforeTheMountLeftIt_IsNoLanding()
    {
        Assert.That(JumpClipRules.NextTime(0.2f, 0.02f, true, false, 0.2f), Is.EqualTo(0.22f).Within(1e-5f));
    }

    [Test]
    public void AfterTheTouchDown_PlaysToTheEnd_ThenFinishes()
    {
        Assert.That(JumpClipRules.NextTime(1.4f, 0.1f, true, true, 2f), Is.EqualTo(1.5f).Within(1e-5f));
        Assert.That(JumpClipRules.NextTime(1.4f, 0.1f, false, true, 2f), Is.EqualTo(1.5f).Within(1e-5f));
        Assert.That(JumpClipRules.NextTime(JumpClipRules.EndTime - 0.01f, 0.1f, true, true, 2f), Is.EqualTo(JumpClipRules.Finished));
    }

    [Test]
    public void TooLong_Finishes()
    {
        Assert.That(JumpClipRules.NextTime(JumpClipRules.HoldTime, 0.02f, false, true, JumpClipRules.MaxSeconds + 0.1f), Is.EqualTo(JumpClipRules.Finished));
    }

    [Test]
    public void ExactlyMaxSeconds_StillPlays_OnlyLongerFinishes()
    {
        Assert.That(JumpClipRules.NextTime(JumpClipRules.HoldTime, 0.02f, false, true, JumpClipRules.MaxSeconds), Is.EqualTo(JumpClipRules.HoldTime).Within(1e-5f));
    }

    [Test]
    public void WhileFalling_AtTheTouchDownFrame_KeepsPlaying()
    {
        // at the touch-down frame the clip is past the "hold" window: it must not jump back to the hold pose
        Assert.That(JumpClipRules.NextTime(JumpClipRules.TouchTime, 0.02f, false, true, 2f), Is.EqualTo(JumpClipRules.TouchTime + 0.02f).Within(1e-5f));
    }

    [Test]
    public void ReachingTheEndFrameExactly_Finishes()
    {
        Assert.That(JumpClipRules.NextTime(JumpClipRules.EndTime, 0f, true, true, 2f), Is.EqualTo(JumpClipRules.Finished));
    }

    [Test]
    public void NegativeDeltaTime_DoesNotRunBackwards()
    {
        Assert.That(JumpClipRules.NextTime(0.5f, -0.1f, true, false, 0.5f), Is.EqualTo(0.5f).Within(1e-5f));
    }
}
