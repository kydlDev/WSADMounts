using System;
using System.Collections.Generic;
using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The look of a back step with the BackwardWalk style (D-031): the game's Lox, Asksvin and Moose play their OWN walk clip backwards; every
/// other mount (modded ones) shows the Moonwalk (D-014). Pure: which mount, which clip, and the clip time stepping backwards (unit-tested).
/// </summary>
internal static class BackwardWalkRules
{
    /// <summary>The game's mounts that get the reversed walk (prefab names, Valheim 1.0.30).</summary>
    internal static readonly string[] ReversedWalkMounts = { "Lox", "Asksvin", "Moose" };

    /// <summary>Whether this mount shows the reversed walk for this style. Expected outcome: false for Moonwalk and for every other mount.</summary>
    internal static bool UsesReversedWalk(string prefabName, BackwardAnimation style)
    {
        string name = PrefabName(prefabName);
        bool listed = Array.IndexOf(ReversedWalkMounts, name) >= 0;
        bool result = style == BackwardAnimation.BackwardWalk && listed;
        if (RuleLog.IsOn()) RuleLog.Verbose($"BackwardWalkRules.UsesReversedWalk(prefab={prefabName} -> {name}, style={style}): listed {listed} -> {result}");
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

    /// <summary>The clip time one frame later, running BACKWARDS at `rate` (1 = the clip's own speed), wrapped into [0, length). Expected outcome: 0 for a clip without length.</summary>
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
