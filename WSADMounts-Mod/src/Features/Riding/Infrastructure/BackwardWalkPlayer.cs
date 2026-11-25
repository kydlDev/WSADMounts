using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using WSADMounts.Configuration;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Shows a back step (D-032, D-035): a PlayableGraph with one AnimationClipPlayable on the mount's Animator. ReversedWalk: the mount's own walk clip
/// with its time driven BACKWARDS; BackwardWalk: the mod's hand-made backward clip played forwards (<see cref="BackwardWalkRules.NextPhase"/>);
/// Moonwalk: no player at all (the game's forward walk plays on). Added when
/// the back step starts, removed when it ends (the game's animator controller then plays on). Runs on every client that sees the mount
/// (BackwardWalkSync tells it when and which style).
/// </summary>
internal sealed class BackwardWalkPlayer : MonoBehaviour
{
    private static readonly List<string> Names = new List<string>();

    private PlayableGraph _graph;
    private AnimationClipPlayable _clip;
    private float _length;
    private float _phase;
    private float _rate;

    /// <summary>The clip being played (for logs and test hooks).</summary>
    internal string ClipName { get; private set; } = string.Empty;

    /// <summary>The look being shown.</summary>
    internal BackStepLook Look { get; private set; }

    /// <summary>The current clip time (seconds): it goes DOWN for the reversed walk, UP for the hand-made walk.</summary>
    internal float Phase => _phase;

    /// <summary>The clip length (seconds).</summary>
    internal float Length => _length;

    /// <summary>Whether the graph is playing now.</summary>
    internal bool Playing => _graph.IsValid() && _graph.IsPlaying();

    /// <summary>Starts (or switches to) this style's look on this mount. Expected outcome: true when a clip plays (Moonwalk: when none does).</summary>
    internal static bool StartOn(GameObject mount, BackwardAnimation style)
    {
        AnimationClip handMade = HandMadeClips.For(mount.name);
        BackStepLook look = BackwardWalkRules.PickLook(style, handMade != null);
        if (look == BackStepLook.Moonwalk)
        {
            // D-035: the Moonwalk is the game's own forward walk (forward_speed kept positive by ZSyncAnimationSetFloatPatch): no clip of the mod
            bool none = StopOn(mount, "Moonwalk style: the game's forward walk plays");
            ModLog.Verbose($"BackwardWalkPlayer.StartOn: {mount.name} style {style} -> {look}, no clip of the mod ({none})");
            return none;
        }

        BackwardWalkPlayer player = mount.GetComponent<BackwardWalkPlayer>();
        if (player != null && player.Look != look)
        {
            player.End($"look {player.Look} -> {look}");
            Destroy(player);
            player = null;
        }

        if (player == null)
        {
            player = mount.AddComponent<BackwardWalkPlayer>();
            player.Begin(look, handMade);
        }

        bool playing = player.Playing;
        ModLog.Verbose($"BackwardWalkPlayer.StartOn: {mount.name} style {style} -> {look}, playing {playing} (clip {player.ClipName})");
        return playing;
    }

    /// <summary>Stops the back-step look on this mount. Expected outcome: true when none plays afterwards.</summary>
    internal static bool StopOn(GameObject mount, string why)
    {
        BackwardWalkPlayer player = mount != null ? mount.GetComponent<BackwardWalkPlayer>() : null;
        if (player != null)
        {
            player.End(why);
            Destroy(player);
        }

        ModLog.Verbose($"BackwardWalkPlayer.StopOn: {(mount != null ? mount.name : "<none>")} - {why} (was playing {player != null})");
        return player == null || !player.Playing;
    }

    private void Begin(BackStepLook look, AnimationClip handMade)
    {
        Look = look;
        Animator animator = GetComponentInChildren<Animator>();
        AnimationClip clip = look == BackStepLook.BackwardWalk ? handMade : WalkClipOf(animator);
        if (animator == null || clip == null)
        {
            ModLog.Warn($"BackwardWalkPlayer: nothing to play on {name} for {look} (animator {animator != null}, clips: {string.Join(", ", Names)})");
        }
        else
        {
            _graph = PlayableGraph.Create($"WSADMounts back step {name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "WSADMounts back step", animator);
            _clip = AnimationClipPlayable.Create(_graph, clip);
            _clip.SetApplyFootIK(false);
            output.SetSourcePlayable(_clip);
            _length = clip.length;
            _rate = BackwardWalkRules.PlaybackRate(look, ModSettings.BackStepSpeedFactor.Value);
            _phase = _rate > 0f ? _length : 0f;
            ClipName = clip.name;
            _graph.Play();
            ModLog.State($"back-step look of {name}", "none", look.ToString(), $"clip {clip.name} {_length:0.##} s, rate {_rate:0.##} (positive = backwards)");
        }
    }

    private static AnimationClip WalkClipOf(Animator animator)
    {
        AnimationClip[] clips = animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.animationClips : new AnimationClip[0];
        Names.Clear();
        foreach (AnimationClip c in clips)
        {
            Names.Add(c != null ? c.name : string.Empty);
        }

        int index = BackwardWalkRules.PickWalkClip(Names);
        ModLog.Every("BackwardWalkPlayer.WalkClipOf", 10d, $"BackwardWalkPlayer: walk clip {index} of {clips.Length}");
        return index >= 0 ? clips[index] : null;
    }

    private void Update()
    {
        if (_graph.IsValid())
        {
            _phase = BackwardWalkRules.NextPhase(_phase, Time.deltaTime, _length, _rate);
            _clip.SetTime(_phase);
            ModLog.Every("BackwardWalkPlayer.Update", 2d, $"BackwardWalkPlayer: {name} {Look} clip {ClipName} at {_phase:0.00}/{_length:0.00} s (rate {_rate:0.##})");
        }
    }

    // The game's Animator evaluates its controller between Update and LateUpdate and overwrote a pose written in Update (JumpClipPlayer probe, lab a6pose
    // 2026-11-25), so the clip is evaluated here, after it.
    private void LateUpdate()
    {
        if (_graph.IsValid())
        {
            _graph.Evaluate(0f);
            ModLog.Every("BackwardWalkPlayer.LateUpdate", 2d, $"BackwardWalkPlayer.LateUpdate: {name} {ClipName} evaluated after the game's animator");
        }
    }

    private void End(string why)
    {
        bool was = _graph.IsValid();
        if (was)
        {
            _graph.Destroy();
        }

        ModLog.State($"back-step look of {name}", was ? Look.ToString() : "none", "none", why);
    }

    private void OnDestroy()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
            ModLog.Verbose($"BackwardWalkPlayer.OnDestroy: {name} graph destroyed with the mount");
        }
    }
}
