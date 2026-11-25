using System;
using System.Collections.Generic;
using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The look of a back step (D-032, D-035): <see cref="BackStepLook.ReversedWalk"/> plays the mount's own walk clip backwards (any mount with a walk
/// clip); <see cref="BackStepLook.BackwardWalk"/> plays the mod's hand-made backward clip, which exists for the game's Lox, Asksvin and Moose only -
/// every other mount, and those three while their clip is missing from the bundle, show the reversed walk; <see cref="BackStepLook.Moonwalk"/> plays
/// no clip of the mod (the game's forward walk goes on). Pure: which look, which clip, and the clip time stepping on (unit-tested).
/// </summary>
internal static class BackwardWalkRules
{
    /// <summary>The game's mounts that get a hand-made backward clip (prefab names, Valheim 1.0.30).</summary>
    internal static readonly string[] HandMadeMounts = { "Lox", "Asksvin", "Moose" };

    /// <summary>The bundle asset name of a mount's hand-made backward clip: "WSADMounts_Lox_WalkBack". Expected outcome: empty for other mounts.</summary>
    internal static string HandMadeClipName(string prefabName)
    {
        string name = PrefabName(prefabName);
        string result = Array.IndexOf(HandMadeMounts, name) >= 0 ? $"WSADMounts_{name}_WalkBack" : string.Empty;
        if (RuleLog.IsOn()) RuleLog.Verbose($"BackwardWalkRules.HandMadeClipName({prefabName}) -> '{result}'");
        return result;
    }

    /// <summary>
    /// The look this mount shows for this style. Expected outcome: Moonwalk for the Moonwalk style; BackwardWalk only when the style asks for it AND
    /// the mount's hand-made clip is loaded; ReversedWalk otherwise (the ReversedWalk style, and the BackwardWalk fallback, D-035).
    /// </summary>
    internal static BackStepLook PickLook(BackwardAnimation style, bool handMadeClipLoaded)
    {
        BackStepLook result = style == BackwardAnimation.Moonwalk
            ? BackStepLook.Moonwalk
            : (style == BackwardAnimation.BackwardWalk && handMadeClipLoaded ? BackStepLook.BackwardWalk : BackStepLook.ReversedWalk);
        if (RuleLog.IsOn()) RuleLog.Verbose($"BackwardWalkRules.PickLook(style={style}, clip loaded={handMadeClipLoaded}) -> {result}");
        return result;
    }

    /// <summary>
    /// How fast the clip time runs for <see cref="NextPhase"/> (positive = backwards). ReversedWalk: the walk clip backwards at the back-step speed
    /// factor; BackwardWalk: the hand-made clip forwards at its own speed (it is made for the back step); Moonwalk plays no clip. Expected outcome:
    /// factor, -1 or 0.
    /// </summary>
    internal static float PlaybackRate(BackStepLook look, float backStepFactor)
    {
        float result = look == BackStepLook.ReversedWalk ? backStepFactor : (look == BackStepLook.BackwardWalk ? -1f : 0f);
        if (RuleLog.IsOn()) RuleLog.Verbose($"BackwardWalkRules.PlaybackRate({look}, factor {backStepFactor:0.##}) -> {result:0.##}");
        return result;
    }

    /// <summary>
    /// The plain walk clip among the animator's clips: "walk" itself (any case, a " (n)" / "_n" copy suffix allowed), else the first clip that
    /// starts with "walk" and is not a side / turn variant. Expected outcome: its index, or -1 when there is none.
    /// </summary>
    internal static int PickWalkClip(IReadOnlyList<string> clipNames)
    {
        int exact = -1, loose = -1;
        for (int i = 0; clipNames != null && i < clipNames.Count; i++)
        {
            string n = (clipNames[i] ?? string.Empty).Trim().ToLowerInvariant();
            bool variant = n.Contains("left") || n.Contains("right") || n.Contains("turn");
            if (exact < 0 && (n == "walk" || IsCopyOfWalk(n)))
            {
                exact = i;
            }
            else if (loose < 0 && n.StartsWith("walk", StringComparison.Ordinal) && !variant)
            {
                loose = i;
            }
        }

        int result = exact >= 0 ? exact : loose;
        if (RuleLog.IsOn()) RuleLog.Verbose($"BackwardWalkRules.PickWalkClip({clipNames?.Count ?? 0} clips): exact {exact}, loose {loose} -> {result}");
        return result;
    }

    /// <summary>The clip time one frame later, running BACKWARDS at `rate` (1 = the clip's own speed, negative = forwards), wrapped into [0, length). Expected outcome: 0 for a clip without length.</summary>
    internal static float NextPhase(float phase, float deltaSeconds, float clipLength, float rate)
    {
        float result = 0f;
        if (clipLength > 0.0001f)
        {
            result = (phase - (deltaSeconds * rate)) % clipLength;
            if (result < 0f)
            {
                result += clipLength;
            }
        }

        if (RuleLog.Due("BackwardWalkRules.NextPhase", 2d)) { RuleLog.Verbose($"BackwardWalkRules.NextPhase: {phase:0.###} - {deltaSeconds:0.###} s x {rate:0.##} in {clipLength:0.###} s -> {result:0.###}"); }
        return result;
    }

    /// <summary>"Lox(Clone)" -> "Lox".</summary>
    internal static string PrefabName(string name)
    {
        string n = (name ?? string.Empty).Trim();
        int clone = n.IndexOf("(Clone)", StringComparison.Ordinal);
        string result = clone >= 0 ? n.Substring(0, clone).Trim() : n;
        if (RuleLog.Due("BackwardWalkRules.PrefabName", 5d)) { RuleLog.Verbose($"BackwardWalkRules.PrefabName: {name} -> {result}"); }
        return result;
    }

    private static bool IsCopyOfWalk(string lowerName)
    {
        bool result = lowerName.Length > 4 && lowerName.StartsWith("walk", StringComparison.Ordinal) && IsCopySuffix(lowerName.Substring(4));
        if (RuleLog.Due("BackwardWalkRules.IsCopyOfWalk", 5d)) { RuleLog.Verbose($"BackwardWalkRules.IsCopyOfWalk({lowerName}) -> {result}"); }
        return result;
    }

    private static bool IsCopySuffix(string rest)
    {
        string digits = rest.Trim().TrimStart('_', '(', ' ').TrimEnd(')');
        bool result = digits.Length > 0 && digits.Length <= 3 && int.TryParse(digits, out _);
        if (RuleLog.Due("BackwardWalkRules.IsCopySuffix", 5d)) { RuleLog.Verbose($"BackwardWalkRules.IsCopySuffix({rest}) -> {result}"); }
        return result;
    }
}
