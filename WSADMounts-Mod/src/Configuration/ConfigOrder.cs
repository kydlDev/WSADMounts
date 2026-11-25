using BepInEx.Configuration;
using KydlCore.Common;
using WSADMounts.Core;

namespace WSADMounts.Configuration;

/// <summary>
/// The tag BepInEx's Configuration Manager (F1) reads: <c>Order</c> sets where a setting sits inside its category, a HIGHER number is HIGHER in
/// the list (default 0). The KY hub settings tab (F6) lists a category in the order the entries are bound instead, so the same order is kept by
/// binding them in that order (user 2026-11-17: [General] Enable Mod is the first setting of every mod, in both menus).
/// </summary>
internal sealed class ConfigurationManagerAttributes
{
    public int? Order;
}

/// <summary>Descriptions that carry the F1 order tag. <see cref="First(string, bool)"/> = top of its category, <see cref="Last"/> = bottom of it.</summary>
internal static class ConfigOrder
{
    public const int FirstOrder = 1000, LastOrder = -1000;

    /// <summary>
    /// The first entry of its category. <paramref name="adminOnly"/> = only an admin may change it and Jotunn syncs it from the server: that flag is
    /// read from Jotunn's own (global) ConfigurationManagerAttributes class, so the tag is that class then (FocusFollowedPlayer does the same).
    /// </summary>
    public static ConfigDescription First(string description, bool adminOnly = false)
    {
        object tag = adminOnly
            ? new global::ConfigurationManagerAttributes { Order = FirstOrder, IsAdminOnly = true }
            : new ConfigurationManagerAttributes { Order = FirstOrder };
        var result = new ConfigDescription(description, null, tag);
        if (RuleLog.IsOn()) ModLog.Verbose($"ConfigOrder.First: returns a description with order {FirstOrder}, admin only {adminOnly} ({RuleLog.Shown(description)})");
        return result;
    }

    /// <summary>The last entry of its category.</summary>
    public static ConfigDescription Last(string description)
    {
        var result = new ConfigDescription(description, null, new ConfigurationManagerAttributes { Order = LastOrder });
        if (RuleLog.IsOn()) ModLog.Verbose($"ConfigOrder.Last: returns a description with order {LastOrder} ({RuleLog.Shown(description)})");
        return result;
    }
}
