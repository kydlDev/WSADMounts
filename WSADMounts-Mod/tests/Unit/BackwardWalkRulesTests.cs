using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Tests.Unit;

/// <summary>D-031 / D-032: which look a back step shows, which clip, and the clip time.</summary>
public class BackwardWalkRulesTests
{
    [TestCase("Lox", "WSADMounts_Lox_WalkBack")]
    [TestCase("Lox(Clone)", "WSADMounts_Lox_WalkBack")]
    [TestCase("Asksvin(Clone)", "WSADMounts_Asksvin_WalkBack")]
    [TestCase("Moose", "WSADMounts_Moose_WalkBack")]
    [TestCase("Moose_calf", "")]
    [TestCase("SomeModdedHorse(Clone)", "")]
    [TestCase("", "")]
    public void HandMadeClipName_OnlyTheGamesThreeMounts(string prefab, string expected)
    {
        Assert.That(BackwardWalkRules.HandMadeClipName(prefab), Is.EqualTo(expected));
    }

    [TestCase(0, true, 1)]
    [TestCase(0, false, 0)]
    [TestCase(1, true, 0)]
    [TestCase(1, false, 0)]
    public void PickLook_HandMadeOnlyWhenAskedAndLoaded(int style, bool loaded, int expected)
    {
        // style 0 = BackwardWalk, 1 = Moonwalk; look 0 = Moonwalk, 1 = BackwardWalk (internal enums cannot be public test parameters)
        Assert.That((int)BackwardWalkRules.PickLook((BackwardAnimation)style, loaded), Is.EqualTo(expected));
    }

    [Test]
    public void PlaybackRate_MoonwalkBackwardsAtFactor_HandMadeForwards()
    {
        Assert.That(BackwardWalkRules.PlaybackRate(BackStepLook.Moonwalk, 0.5f), Is.EqualTo(0.5f));
        Assert.That(BackwardWalkRules.PlaybackRate(BackStepLook.BackwardWalk, 0.5f), Is.EqualTo(-1f));
    }

    [Test]
    public void NextPhase_NegativeRate_RunsForwards()
    {
        Assert.That(BackwardWalkRules.NextPhase(0.9f, 0.2f, 1f, -1f), Is.EqualTo(0.1f).Within(0.0001f));
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
