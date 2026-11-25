using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-031: which mount plays its walk backwards, which clip, and the backward clip time.</summary>
public class BackwardWalkRulesTests
{
    [TestCase("Lox", true)]
    [TestCase("Lox(Clone)", true)]
    [TestCase("Asksvin(Clone)", true)]
    [TestCase("Moose", true)]
    [TestCase("Moose_calf", false)]
    [TestCase("SomeModdedHorse(Clone)", false)]
    [TestCase("", false)]
    public void UsesReversedWalk_OnlyTheGamesThreeMounts_WithBackwardWalk(string prefab, bool expected)
    {
        Assert.That(BackwardWalkRules.UsesReversedWalk(prefab, BackwardAnimation.BackwardWalk), Is.EqualTo(expected));
    }

    [Test]
    public void UsesReversedWalk_NeverForMoonwalk()
    {
        Assert.That(BackwardWalkRules.UsesReversedWalk("Lox", BackwardAnimation.Moonwalk), Is.False);
    }

    [TestCase(new[] { "Idle", "Walk", "Canter" }, 1)]
    [TestCase(new[] { "Walk Left", "Walk Right", "Walk_28", "Run" }, 2)]
    [TestCase(new[] { "Walk Left", "walk (2)" }, 1)]
    [TestCase(new[] { "Idle", "Walking", "Walk Left" }, 1)]
    [TestCase(new[] { "Walk Left", "Walk Right", "Turn L" }, -1)]
    [TestCase(new string[0], -1)]
    public void PickWalkClip_ThePlainWalk(string[] names, int expected)
    {
        Assert.That(BackwardWalkRules.PickWalkClip(names), Is.EqualTo(expected));
    }

    [Test]
    public void PickWalkClip_NullList_IsNone()
    {
        Assert.That(BackwardWalkRules.PickWalkClip(null), Is.EqualTo(-1));
    }

    [TestCase(0.5f, 0.1f, 1f, 1f, 0.4f)]
    [TestCase(0.5f, 0.1f, 1f, 0.5f, 0.45f)]
    [TestCase(0.05f, 0.1f, 1f, 1f, 0.95f)]
    [TestCase(0.5f, 2.3f, 1f, 1f, 0.2f)]
    [TestCase(0.5f, 0.1f, 0f, 1f, 0f)]
    public void NextPhase_RunsBackwardsAndWraps(float phase, float dt, float length, float rate, float expected)
    {
        Assert.That(BackwardWalkRules.NextPhase(phase, dt, length, rate), Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase("Lox(Clone)", "Lox")]
    [TestCase("  Moose ", "Moose")]
    [TestCase(null, "")]
    public void PrefabName_DropsTheCloneSuffix(string name, string expected)
    {
        Assert.That(BackwardWalkRules.PrefabName(name), Is.EqualTo(expected));
    }
}
