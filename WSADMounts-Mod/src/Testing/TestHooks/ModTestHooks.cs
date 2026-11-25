using KydlCore.Common;
using WSADMounts.Bootstrap;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// DrakesTestLab hooks (spec 8.7) for the suite in tests/TestLab/suites/WSADMounts. A hook is a static method with [TestHook("name")];
/// DrakesTestLab finds it by the attribute's NAME (the mod has no reference to DrakesTestLab; TestHookAttribute comes from KydlCore.dll:
/// KydlCore.Common.TestHookAttribute). Names start with "WSADMounts." so two mods never clash. Hooks call the REAL code paths, never a copy
/// of a rule. Reads return short values a suite compares with expect / contains / notContains (NOT min / max: a `do: hook` step silently
/// ignores them, PROJECT_RULES 21). A hook that returns false for a reason logs that reason.
/// </summary>
internal static class ModTestHooks
{
    /// <summary>Every test starts here: the player on the open arena spot 70 m from the lab start, facing +z, god mode on.</summary>
    [TestHook("WSADMounts.arena")]
    private static string Arena() => TestArena.MovePlayer();

    /// <summary>All Startup_* / World_* checks of InGameTests (the ones Kydl.GameProbe runs): "PASS: ..." or "FAIL: ...".</summary>
    [TestHook("WSADMounts.selftest")]
    private static string SelfTest() => TestLabSupport.RunInGameTests(typeof(InGameTests));

    /// <summary>Is the feature running (FeatureRegistry state after its Enable switches)?</summary>
    [TestHook("WSADMounts.featureRunning")]
    private static bool FeatureRunning(string id)
    {
        bool running = ModInstaller.Current != null && ModInstaller.Current.Features.IsRunning(id);
        ModLog.Verbose($"ModTestHooks.FeatureRunning: '{id}' running {running} (installer {(ModInstaller.Current != null ? "present" : "missing")})");
        return running;
    }
}
