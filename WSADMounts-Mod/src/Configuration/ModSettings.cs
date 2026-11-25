using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using KydlCore.Common;
using WSADMounts.Core;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Configuration;

/// <summary>
/// All settings in one place (user rule, every mod):
///  * PLAYER CONFIG (BepInEx entries below, bound once from Plugin.Awake): only what a player should change. Clear descriptions, ranges,
///    drop-downs (enums). Descriptions never mention older versions, old names or old values.
///  * DEV-ONLY values (Tunable fields below): fixed values chosen by the user, NOT in the player's file. The user's KydlTools mod (F7) finds
///    every static Tunable here by itself and lets the user change it in game - nothing to register.
/// When the user settles a value in game, it becomes the new fixed value / default here.
/// </summary>
internal static class ModSettings
{
    // [General]
    // Admin-only and synced from the server (D-010): off = the mod is inert for every player on that server.
    public static ConfigEntry<bool> EnableMod;

    // The player's own switch of the custom controls (D-011, D-019); the KY hub (F6) is the menu, the mod has no menu key.
    public static ConfigEntry<bool> CustomControlsEnable;

    // [Animation]
    public static ConfigEntry<BackwardAnimation> BackwardAnimationStyle;

    // [DebugLog]
    public static ConfigEntry<bool> DebugLog;

    /// <summary>Is the mod switched on ([General] Enable Mod, set by the server admin)? Off = inert: no effects.</summary>
    public static bool On => EnableMod == null || EnableMod.Value;

    /// <summary>
    /// The gate of one feature (IFeature.IsEnabled): Enable Mod AND its own [General] Enable &lt;Feature&gt;. Read at call time, never cached, so a
    /// config change applies at once (PROJECT_RULES 17).
    /// </summary>
    public static bool FeatureOn(ConfigEntry<bool> ownEnable) => On && (ownEnable == null || ownEnable.Value);

    // --- dev-only fixed values (Tunables): not in the config file, KydlTools changes them for testing -----------------------------------

    /// <summary>Back-step speed as a share of the mount's forward walk speed (D-017: half).</summary>
    public static readonly Tunable<float> BackStepSpeedFactor = new Tunable<float>("Riding", "BackStepSpeedFactor", 0.5f, 0.1f, 1f,
        "Speed of a mount stepping backwards, as a share of its forward walk speed (0.1-1.0; 0.5 = half the walk speed).");

    /// <summary>Expected outcome: every player entry is bound (returns that check).</summary>
    public static bool Bind(ConfigFile c)
    {
        var savedLines = ConfigTools.Capture(c);     // before the first Bind: every Bind saves the file with what BepInEx parsed

        EnableMod = c.Bind("General", "Enable Mod", Old(c, "General", "Enable mod", "General", "EnableAll"),
            ConfigOrder.First("Server admin setting: switches the whole mod on / off for every player on the server. Off: mounts ride as in the normal game.", adminOnly: true));
        CustomControlsEnable = c.Bind("General", "Enable Custom Controls", true,
            "Your own choice: on = steer your mount with all movement keys (W/A/S/D or the stick, relative to the camera) and walk it backwards; off = the normal game's riding (S stops, no walking backwards).");
        BackwardAnimationStyle = c.Bind("Animation", "Backward Animation", BackwardAnimation.BackwardWalk,
            "How your mount looks while it walks backwards, for you and for every other player. BackwardWalk: a real backward walk (mounts without one use Moonwalk). Moonwalk: the normal forward walk while it moves backwards.");
        DebugLog = c.Bind("DebugLog", "Enable", false,
            "Writes detailed lines about everything the mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

        ConfigTools.ApplyCommaFix(c, savedLines);    // after every Bind: "3,5" typed by hand means 3.5, not 35
        ConfigTools.DropRemoved(c);

        EnableMod.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Mod = {EnableMod.Value}{(EnableMod.Value ? ", the mod is on" : ", the mod is inert (no effects)")}");
        CustomControlsEnable.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Custom Controls = {CustomControlsEnable.Value}");
        BackwardAnimationStyle.SettingChanged += (s, e) => ModLog.Info($"Config: Backward Animation = {BackwardAnimationStyle.Value}");
        ModLog.Verbose($"Config: bound (Enable Mod {EnableMod.Value}, custom controls {CustomControlsEnable.Value}, backward animation {BackwardAnimationStyle.Value}, debug log {DebugLog.Value})");
        return EnableMod != null && CustomControlsEnable != null && BackwardAnimationStyle != null && DebugLog != null;
    }

    /// <summary>
    /// The value the player had in an entry that moved (still in the file as an orphan before DropRemoved deletes it): the first of the
    /// given (section, key, section, key, ...) pairs that holds a bool; true when there is none.
    /// </summary>
    private static bool Old(ConfigFile c, params string[] sectionAndKey)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"ModSettings.Old(c={RuleLog.Shown(c)}, pairs={sectionAndKey.Length / 2})");
        // OrphanedEntries is not a public member of every BepInEx 5 build (ConfigTools.DropRemoved reaches it the same way)
        PropertyInfo orphans = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var dict = orphans?.GetValue(c, null) as IDictionary;
        bool found = false;
        bool value = true;
        for (int i = 0; dict != null && !found && i + 1 < sectionAndKey.Length; i += 2)
        {
            var def = new ConfigDefinition(sectionAndKey[i], sectionAndKey[i + 1]);
            found = dict.Contains(def) && bool.TryParse((dict[def] as string ?? string.Empty).Trim(), out value);
        }

        bool result = !found || value;
        if (RuleLog.IsOn()) ModLog.Verbose($"ModSettings.Old: returns {result} (orphan found {found})");
        return result;
    }
}
