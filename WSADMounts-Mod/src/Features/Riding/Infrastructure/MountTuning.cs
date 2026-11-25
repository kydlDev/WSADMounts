using System.Collections.Generic;
using KydlCore.Common;
using WSADMounts.Configuration;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// The ride feel (D-037): while a mount has a rider, its owner gives it the values of <see cref="RideTuningRules"/> (faster acceleration and
/// turning, faster sprint with less stamina drain); when nobody rides it any more, or the mod is off, the mount gets its own values back.
/// Called from the Sadle.UpdateRiding postfix, which runs only on the mount's owner (MonsterAI.UpdateAI). Factors are read every tick, so a
/// change in KydlTools applies at once (PROJECT_RULES 17); nothing is written while they stay the same.
/// </summary>
internal static class MountTuning
{
    private sealed class Applied
    {
        internal RideTuning Own;
        internal RideFactors Factors;
    }

    private static readonly Dictionary<Character, Applied> Tuned = new Dictionary<Character, Applied>();

    /// <summary>Whether this mount carries the ridden values now (lab hooks).</summary>
    internal static bool IsTuned(Character mount) => mount != null && Tuned.ContainsKey(mount);

    /// <summary>One AI tick of a saddled mount on its owner. Expected outcome: the mount carries the ridden values exactly while it is ridden.</summary>
    internal static bool Update(Sadle saddle, Character mount, bool ridden)
    {
        if (!ridden)
        {
            // rule 38: an unridden mount that was never tuned costs one dictionary lookup (and a cheap throttle check)
            if (ModLog.IsDue("MountTuning.unridden", 10d)) ModLog.Verbose($"MountTuning: {mount.m_name} not ridden, {Tuned.Count} mount(s) carry ridden values");
            return Tuned.Count == 0 || !Tuned.ContainsKey(mount) || Restore(saddle, mount, "nobody rides it");
        }

        RideFactors factors = Current();
        if (Tuned.TryGetValue(mount, out Applied applied) && applied.Factors.SameAs(factors))
        {
            if (ModLog.IsDue("MountTuning.same", 10d)) ModLog.Verbose($"MountTuning: {mount.m_name} ridden, values unchanged ({factors})");
            return applied.Factors.SameAs(factors);
        }

        if (applied == null)
        {
            Prune();
            applied = new Applied { Own = Read(saddle, mount) };
            Tuned[mount] = applied;
        }

        applied.Factors = factors;
        RideTuning tuned = RideTuningRules.Tune(applied.Own, factors);
        Write(saddle, mount, tuned);
        ModLog.State($"ride feel of {mount.m_name}", "own values", "ridden values", $"{factors}: {applied.Own} -> {tuned}");
        return Tuned.ContainsKey(mount);
    }

    /// <summary>Gives the mount its own values back. Expected outcome: the mount is no longer tuned.</summary>
    internal static bool Restore(Sadle saddle, Character mount, string why)
    {
        if (mount != null && Tuned.TryGetValue(mount, out Applied applied))
        {
            Write(saddle, mount, applied.Own);
            Tuned.Remove(mount);
            ModLog.State($"ride feel of {mount.m_name}", "ridden values", "own values", $"{why}: {applied.Own}");
        }

        ModLog.Verbose($"MountTuning.Restore: {(mount != null ? mount.m_name : "<none>")} ({why}) -> tuned {IsTuned(mount)}");
        return !IsTuned(mount);
    }

    private static RideFactors Current() => new RideFactors(
        ModSettings.SprintSpeedFactor.Value, ModSettings.SprintStaminaFactor.Value, ModSettings.RideAccelerationFactor.Value,
        ModSettings.RideTurnFactor.Value, RidingFeature.SprintOn);

    private static RideTuning Read(Sadle saddle, Character mount) =>
        new RideTuning(mount.m_acceleration, mount.m_turnSpeed, mount.m_runTurnSpeed, mount.m_runSpeed, saddle != null ? saddle.m_runStaminaDrain : 0f);

    private static void Write(Sadle saddle, Character mount, RideTuning values)
    {
        mount.m_acceleration = values.Acceleration;
        mount.m_turnSpeed = values.TurnSpeed;
        mount.m_runTurnSpeed = values.RunTurnSpeed;
        mount.m_runSpeed = values.RunSpeed;
        if (saddle != null)
        {
            saddle.m_runStaminaDrain = values.RunStaminaDrain;
        }

        ModLog.Verbose($"MountTuning.Write: {mount.m_name} now {Read(saddle, mount)}");
    }

    private static void Prune()
    {
        if (Tuned.Count < 16)
        {
            ModLog.Verbose($"MountTuning.Prune: {Tuned.Count} mount(s) tuned, nothing to forget yet");
            return;
        }

        var gone = new List<Character>();
        foreach (Character known in Tuned.Keys)
        {
            if (known == null) gone.Add(known);
        }

        gone.ForEach(g => Tuned.Remove(g));
        ModLog.Verbose($"MountTuning.Prune: {gone.Count} unloaded mount(s) forgotten, {Tuned.Count} left");
    }
}
