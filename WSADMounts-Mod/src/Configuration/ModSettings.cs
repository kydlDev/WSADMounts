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

    // The player's own switches of the mount actions (D-037, default on): Space jumps, attacks attack with the mount, sprint runs faster.
    public static ConfigEntry<bool> MountJumpEnable;
    public static ConfigEntry<bool> MountAttackEnable;
    public static ConfigEntry<bool> MountSprintEnable;

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

    /// <summary>Run speed of a ridden mount as a factor of its own run speed (D-037; Lox 7, Asksvin 9, Moose 12.6 m/s in the game).</summary>
    public static readonly Tunable<float> SprintSpeedFactor = new Tunable<float>("Riding", "SprintSpeedFactor", 1.3f, 1f, 2f,
        "Run speed of a ridden mount with Enable Mount Sprint on, as a factor of the mount's own run speed (1.0-2.0; 1 = the game's run).");

    /// <summary>Saddle stamina a ridden mount uses while running, as a factor of the game's drain (D-037).</summary>
    public static readonly Tunable<float> SprintStaminaFactor = new Tunable<float>("Riding", "SprintStaminaFactor", 0.7f, 0.25f, 1f,
        "Saddle stamina a ridden mount uses per second of running with Enable Mount Sprint on, as a factor of the game's drain (0.25-1.0; 1 = the game's drain).");

    /// <summary>Acceleration of a ridden mount as a factor of its own (D-037; the game's 0.5 makes starting and stopping slow).</summary>
    public static readonly Tunable<float> RideAccelerationFactor = new Tunable<float>("Riding", "RideAccelerationFactor", 3f, 1f, 6f,
        "How fast a ridden mount speeds up and slows down, as a factor of its own acceleration (1-6; 1 = the game's value 0.5).");

    /// <summary>Turn rate of a ridden mount as a factor of its own (D-037; Lox and Moose turn 70 deg/s walking in the game).</summary>
    public static readonly Tunable<float> RideTurnFactor = new Tunable<float>("Riding", "RideTurnFactor", 1.8f, 1f, 4f,
        "How fast a ridden mount turns, walking and running, as a factor of its own turn rate (1-4; 1 = the game's value, Lox 70 deg/s walking).");

    /// <summary>Jump force of a ridden mount as a factor of its own (D-039; force 10 lifts the Lox 2.4 m, the height grows with the factor squared).</summary>
    public static readonly Tunable<float> RideJumpFactor = new Tunable<float>("Riding", "RideJumpFactor", 1f, 1f, 2f,
        "How hard a ridden mount jumps, as a factor of its own jump force (1-2; 1 = the game's jump, the Lox rises 2.4 m; 1.2 = about 3.5 m).");

    /// <summary>Seconds from Space to the push of the hind legs, when the mount leaves the ground (D-039: front feet first, then the push).</summary>
    public static readonly Tunable<float> RideJumpPushDelay = new Tunable<float>("Riding", "RideJumpPushDelay", 0.25f, 0f, 0.6f,
        "Seconds from pressing jump to the moment a ridden mount pushes off with its hind legs and leaves the ground (0-0.6 s; the front legs lift first).");

    /// <summary>Expected outcome: every player entry is bound (returns that check).</summary>
    public static bool Bind(ConfigFile c)
    {
        var savedLines = ConfigTools.Capture(c);     // before the first Bind: every Bind saves the file with what BepInEx parsed

        EnableMod = c.Bind("General", "Enable Mod", Old(c, "General", "Enable mod", "General", "EnableAll"),
            ConfigOrder.First("Server admin setting: switches the whole mod on / off for every player on the server. Off: mounts ride as in the normal game.", adminOnly: true));
        CustomControlsEnable = c.Bind("General", "Enable Custom Controls", true,
            "Your own choice: on = steer your mount with all movement keys (W/A/S/D or the stick, relative to the camera) and walk it backwards; off = the normal game's riding (S stops, no walking backwards).");
        MountJumpEnable = c.Bind("General", "Enable Mount Jump", true,
            "Your own choice: on = Space (controller: jump) makes your mount jump while you ride it; off = Space gets you off the mount, as in the normal game. You get off with Use (E) either way.");
        MountAttackEnable = c.Bind("General", "Enable Mount Attack", true,
            "Your own choice: on = attack makes your mount do its light attack (Lox, Asksvin: Bite; Moose: Horns) and secondary attack does its other attacks in turn (Lox: Stomp; Moose: Hooves, Horn sweep; Asksvin: Headbutt, Pounce); off = attacking gets you off the mount, as in the normal game.");
        MountSprintEnable = c.Bind("General", "Enable Mount Sprint", true,
            "Your own choice: on = the sprint key makes your mount run faster than in the normal game and use less saddle stamina; off = your mount does not run at all, it only walks.");
        BackwardAnimationStyle = c.Bind("Animation", "Backward Animation", BackwardAnimation.BackwardWalk,
            "How your mount looks while it walks backwards, for you and for every other player. BackwardWalk: a real backward walk, made for the Lox, Asksvin and Moose (other mounts use ReversedWalk). ReversedWalk: the mount's own walk played in reverse. Moonwalk: the mount keeps its forward walk while it moves backwards.");
        DebugLog = c.Bind("DebugLog", "Enable", false,
            "Writes detailed lines about everything the mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

        ConfigTools.ApplyCommaFix(c, savedLines);    // after every Bind: "3,5" typed by hand means 3.5, not 35
        ConfigTools.DropRemoved(c);

        EnableMod.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Mod = {EnableMod.Value}{(EnableMod.Value ? ", the mod is on" : ", the mod is inert (no effects)")}");
        CustomControlsEnable.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Custom Controls = {CustomControlsEnable.Value}");
        MountJumpEnable.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Mount Jump = {MountJumpEnable.Value}");
        MountAttackEnable.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Mount Attack = {MountAttackEnable.Value}");
        MountSprintEnable.SettingChanged += (s, e) => ModLog.Info($"Config: Enable Mount Sprint = {MountSprintEnable.Value}");
        BackwardAnimationStyle.SettingChanged += (s, e) => ModLog.Info($"Config: Backward Animation = {BackwardAnimationStyle.Value}");
        ModLog.Verbose($"Config: bound (Enable Mod {EnableMod.Value}, custom controls {CustomControlsEnable.Value}, mount jump {MountJumpEnable.Value}, attack {MountAttackEnable.Value}, sprint {MountSprintEnable.Value}, backward animation {BackwardAnimationStyle.Value}, debug log {DebugLog.Value})");
        return EnableMod != null && CustomControlsEnable != null && MountJumpEnable != null && MountAttackEnable != null && MountSprintEnable != null
            && BackwardAnimationStyle != null && DebugLog != null;
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
