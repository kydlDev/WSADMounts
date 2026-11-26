using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Core;
using WSADMounts.Core;
using WSADMounts.Features.Riding.Infrastructure;
using WSADMounts.Presentation.Hub.Registration;

namespace WSADMounts.Bootstrap;

/// <summary>
/// The composition root (spec "Bootstrap"): builds the shared services (<see cref="DependencyRegistry"/>), the mod's KY hub page and every
/// feature, and hands the features to the <see cref="FeatureRegistry"/> (MIGRATION_PLAN A4), which starts / stops each one live by
/// its Enable switch. A new feature = one <c>Add(new XFeature(...))</c> line here.
/// </summary>
internal sealed class ModInstaller
{
    private readonly ConfigFile _config;

    internal ModInstaller(ConfigFile config)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"ModInstaller.ModInstaller(config={RuleLog.Shown(config)})");
        _config = config;
        Deps = new DependencyRegistry();
        Features = new FeatureRegistry(Deps.Trace);
    }

    /// <summary>The running installer (test hooks and Harmony patches ask it which features run). Null before Awake / after unload.</summary>
    internal static ModInstaller Current { get; private set; }

    internal DependencyRegistry Deps { get; }

    internal FeatureRegistry Features { get; }

    /// <summary>Expected outcome: true when the hub page is registered and every enabled feature started.</summary>
    internal bool Install()
    {
        Current = this;
        bool page = HubTabs.RegisterModPage(_config);
        Add(new RidingFeature());
        bool started = Refresh();
        ModLog.Verbose($"ModInstaller: installed {Features.Count} feature(s), hub page {page}, started {started}");
        return page && started;
    }

    /// <summary>Called on every config change. Expected outcome: true when every feature is in the state its switch asks for.</summary>
    internal bool Refresh()
    {
        OperationResponse<int> r = Features.Refresh();
        if (r.Status == ResponseStatus.Failed)
        {
            ModLog.Warn($"ModInstaller: {r.Reason}");
        }
        else
        {
            ModLog.Verbose($"ModInstaller: refresh - {r}");
        }

        if (RuleLog.IsOn()) ModLog.Verbose($"ModInstaller.Refresh: returns {r.IsSuccess}");
        return r.IsSuccess;
    }

    /// <summary>Mod unload. Expected outcome: true when every feature stopped and the hub page is gone.</summary>
    internal bool Uninstall()
    {
        OperationResponse<int> r = Features.StopAll();
        if (!r.IsSuccess)
        {
            ModLog.Warn($"ModInstaller: {r.Reason}");
        }

        bool gone = HubTabs.UnregisterModPage();
        if (Current == this)
        {
            Current = null;
        }

        bool result = r.IsSuccess && gone;
        if (RuleLog.IsOn()) ModLog.Verbose($"ModInstaller.Uninstall: returns {result} (features stopped {r.IsSuccess}, hub page gone {gone})");
        return result;
    }

    private void Add(IFeature feature)
    {
        OperationResponse<int> r = Features.Add(feature);
        if (!r.IsSuccess)
        {
            ModLog.Error($"ModInstaller: feature '{feature.Id}' not added - {r.Reason}");
        }
        else
        {
            ModLog.Verbose($"ModInstaller: feature '{feature.Id}' added");
        }
    }
}
