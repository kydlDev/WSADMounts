using System;
using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The hand-made jump clips (D-040, approved by the user 2026-11-25): which mount has one and where the clip is at each frame. The clip
/// (30 fps, frames 0..52) keys the take-off (front legs lift, the hind-leg push at frame 8), the flight, the front-feet touch-down at frame 38
/// and the recovery. The real flight decides the landing: the clip holds the "coming down" pose (frame 35) while the mount is still in the
/// air, and goes to the touch-down frame the moment it lands - also early, on a rock. Pure (tests/Unit/JumpClipRulesTests.cs).
/// </summary>
internal static class JumpClipRules
{
    internal const float Fps = 30f;
    internal const float PushTime = 8f / Fps;
    internal const float HoldTime = 35f / Fps;
    internal const float TouchTime = 38f / Fps;
    internal const float EndTime = 52f / Fps;

    /// <summary>The longest a jump clip may run (a mount that never lands, e.g. falling off a cliff into water).</summary>
    internal const float MaxSeconds = 6f;

    /// <summary>Clip time meaning "the clip is over".</summary>
    internal const float Finished = -1f;

    /// <summary>The jump clip of this mount ("WSADMounts_Lox_Jump"), or "" for a mount without one (it keeps the game's pose).</summary>
    internal static string ClipName(string prefabName)
    {
        string name = BackwardWalkRules.PrefabName(prefabName);
        string result = Array.IndexOf(BackwardWalkRules.HandMadeMounts, name) >= 0 ? $"WSADMounts_{name}_Jump" : string.Empty;
        if (RuleLog.IsOn()) RuleLog.Verbose($"JumpClipRules.ClipName({prefabName}) -> '{result}'");
        return result;
    }

    /// <summary>
    /// The clip time after <paramref name="dt"/> seconds. <paramref name="leftGround"/>: the mount was seen in the air since the jump began.
    /// Expected outcome: the next time, or <see cref="Finished"/> when the clip is over (end reached or <see cref="MaxSeconds"/> passed).
    /// </summary>
    internal static float NextTime(float time, float dt, bool onGround, bool leftGround, float elapsed)
    {
        float next = time + Math.Max(0f, dt);

        // Stryker disable String : the reason texts only go to the debug line below (the clip time is what the tests check)
        string why;
        if (elapsed > MaxSeconds)
        {
            next = Finished;
            why = "too long";
        }
        // Stryker disable once Equality : at time == TouchTime Math.Max(next, TouchTime) is next, so "<" and "<=" give the same clip time
        else if (time < TouchTime && leftGround && onGround)
        {
            next = Math.Max(next, TouchTime);
            why = "landed: touch-down";
        }
        // Stryker disable once Equality : next == HoldTime is held at HoldTime either way; time == TouchTime is pinned by the test WhileFalling_AtTheTouchDownFrame_KeepsPlaying
        else if (time < TouchTime && next > HoldTime && !onGround)
        {
            next = HoldTime;
            why = "still in the air: hold";
        }
        else
        {
            why = "play";
        }

        if (next >= EndTime)
        {
            next = Finished;
            why = "end";
        }

        // Stryker restore String
        // Stryker disable once all : only decides whether the debug line is written
        if (RuleLog.Due("JumpClipRules.NextTime", 1d)) RuleLog.Verbose($"JumpClipRules.NextTime: {time:0.000} +{dt:0.000} s (on ground {onGround}, left ground {leftGround}, {elapsed:0.00} s) -> {next:0.000} ({why})");
        return next;
    }
}
