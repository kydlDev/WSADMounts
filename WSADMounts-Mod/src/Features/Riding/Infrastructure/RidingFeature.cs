using KydlCore.Common;
using KydlCore.Core;
using WSADMounts.Configuration;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// The riding feature (D-004..D-019): the adapter that tells the Harmony patches in src/Integrations/Harmony whether they act. Gate: Enable Mod only
/// (the player's own switch, user 2026-11-27; was admin D-010), because a player must SEE other riders walk backwards even with their own custom controls off (D-012); the player's own
/// steering additionally needs [General] Enable Custom Controls, read at call time by <see cref="SteeringOn"/>.
/// </summary>
internal sealed class RidingFeature : IFeature
{
    internal const string FeatureId = "riding";

    /// <summary>The running feature, or null while it is stopped (the patches then leave the game alone).</summary>
    internal static RidingFeature Running { get; private set; }

    public string Id => FeatureId;

    /// <summary>The local player's custom steering is active now (mod on, feature running, the player's own switch on).</summary>
    internal static bool SteeringOn => Running != null && ModSettings.FeatureOn(ModSettings.CustomControlsEnable);

    /// <summary>The local player's mount jump is on now (D-037).</summary>
    internal static bool JumpOn => Running != null && ModSettings.FeatureOn(ModSettings.MountJumpEnable);

    /// <summary>The local player's mount attack is on now (D-037).</summary>
    internal static bool AttackOn => Running != null && ModSettings.FeatureOn(ModSettings.MountAttackEnable);

    /// <summary>The faster sprint is on now (D-037).</summary>
    internal static bool SprintOn => Running != null && ModSettings.FeatureOn(ModSettings.MountSprintEnable);

    /// <summary>The mod is on and the player switched the sprint OFF: the mount does not run at all (user 2026-11-25, D-037).</summary>
    internal static bool SprintBlocked => Running != null && ModSettings.On && ModSettings.MountSprintEnable != null && !ModSettings.MountSprintEnable.Value;

    public bool IsEnabled()
    {
        bool enabled = ModSettings.FeatureOn(null);
        ModLog.Verbose($"RidingFeature.IsEnabled: {enabled} (Enable Mod {ModSettings.On})");
        return enabled;
    }

    public bool Start()
    {
        Running = this;
        ModLog.State("riding feature", "stopped", "started", $"Enable Mod on, custom controls {ModSettings.CustomControlsEnable?.Value}, backward animation {ModSettings.BackwardAnimationStyle?.Value}");
        return Running == this;
    }

    public bool Stop()
    {
        if (Running == this)
        {
            Running = null;
        }

        ModLog.State("riding feature", "started", "stopped", "Enable Mod off or mod unloading: mounts ride as in the normal game");
        return Running == null;
    }
}
