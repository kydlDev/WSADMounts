using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TestLab hooks for the mount actions and the ride feel (T-010, D-037): whether the player still rides, how high the mount got after a jump,
/// which action the owner last ran, the movement values the ridden mount carries and its speed. Judging hooks return "ok" or "bad: ..."
/// (the lab ignores min / max on `do: hook`), reading hooks return short stable text. TEST ONLY.
/// </summary>
internal static class MountActionTestHooks
{
    private static Character Mount => RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<Character>() : null;

    /// <summary>Whether the local player rides a saddle now. Returns "riding True" / "riding False".</summary>
    [TestHook("WSADMounts.riding")]
    private static string Riding()
    {
        Player p = Player.m_localPlayer;
        bool riding = p != null && p.IsRiding();
        ModLog.Info($"MountActionTestHooks.Riding: player {p != null}, riding {riding}, attached {p != null && p.IsAttached()}");
        return $"riding {riding}";
    }

    /// <summary>Starts watching the test mount's height (highest point from now on, see AirSampler). Returns "air marked at Y m".</summary>
    [TestHook("WSADMounts.airMark")]
    private static string AirMark()
    {
        Character c = Mount;
        if (c == null)
        {
            ModLog.Warn("MountActionTestHooks.AirMark: no test mount");
            return "no mount";
        }

        AirSampler sampler = c.GetComponent<AirSampler>() ?? c.gameObject.AddComponent<AirSampler>();
        sampler.Begin();
        string answer = $"air marked at {sampler.StartY:0.00} m";
        ModLog.Info($"MountActionTestHooks.AirMark: {c.m_name} {answer}");
        return answer;
    }

    /// <summary>The highest the mount got above its height at airMark: at least `min` metres. Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.airPeak")]
    private static string AirPeak(double min)
    {
        AirSampler sampler = Mount != null ? Mount.GetComponent<AirSampler>() : null;
        float rise = sampler != null ? sampler.PeakY - sampler.StartY : -1f;
        string answer = rise >= min ? "ok" : $"bad: rose {rise:0.00} m, {min:0.##} m expected (sampler {sampler != null})";
        ModLog.Info($"MountActionTestHooks.AirPeak({min:0.##}): rose {rise:0.00} m -> {answer}");
        return answer;
    }

    /// <summary>The last action the mount's owner ran ("LightAttack started lox_bite"), then forgets it. Returns that text or "none".</summary>
    [TestHook("WSADMounts.lastMountAction")]
    private static string LastMountAction()
    {
        string answer = MountActions.LastAction;
        MountActions.ForgetLast("lastMountAction hook");
        ModLog.Info($"MountActionTestHooks.LastMountAction: {answer}");
        return answer;
    }

    /// <summary>Whether the test mount is free for a new attack (the game refuses one during an attack). Returns "idle" or "in attack".</summary>
    [TestHook("WSADMounts.mountIdle")]
    private static string MountIdle()
    {
        Character c = Mount;
        string answer = c == null ? "no mount" : c.InAttack() ? "in attack" : "idle";
        if (ModLog.IsDue("MountActionTestHooks.MountIdle", 1d)) ModLog.Verbose($"MountActionTestHooks.MountIdle: {answer}");
        return answer;
    }

    /// <summary>The movement values the test mount carries now. Returns "tuned True acceleration=... turn=... deg/s ...".</summary>
    [TestHook("WSADMounts.rideTuning")]
    private static string RideTuning()
    {
        Character c = Mount;
        Sadle saddle = c != null ? c.GetComponentInChildren<Sadle>(true) : null;
        if (c == null)
        {
            ModLog.Warn("MountActionTestHooks.RideTuning: no test mount");
            return "no mount";
        }

        string answer = $"tuned {MountTuning.IsTuned(c)} acceleration={c.m_acceleration:0.##} turn={c.m_turnSpeed:0.#} deg/s runTurn={c.m_runTurnSpeed:0.#} deg/s "
            + $"run={c.m_runSpeed:0.##} m/s drain={(saddle != null ? saddle.m_runStaminaDrain : -1f):0.##}/s";
        ModLog.Info($"MountActionTestHooks.RideTuning: {c.m_name} {answer}");
        return answer;
    }

    /// <summary>The test mount's ground speed now, between `min` and `max` m/s. Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.speed")]
    private static string Speed(double min, double max)
    {
        Character c = Mount;
        Vector3 v = c != null ? c.GetVelocity() : Vector3.zero;
        float ground = new Vector2(v.x, v.z).magnitude;
        bool ok = c != null && ground >= min && ground <= max;
        string answer = ok ? "ok" : $"bad: {ground:0.00} m/s, {min:0.##}..{max:0.##} m/s expected (mount {c != null}, run {c != null && c.IsRunning()})";
        ModLog.Info($"MountActionTestHooks.Speed({min:0.##}..{max:0.##}): {ground:0.00} m/s, running {c != null && c.IsRunning()} -> {(ok ? "ok" : "bad")}");
        return answer;
    }
}
