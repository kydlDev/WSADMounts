using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using WSADMounts.Configuration;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Plays a mount's own walk clip BACKWARDS while it steps back (D-031): a PlayableGraph with one AnimationClipPlayable on the mount's Animator,
/// the clip time driven down every frame (<see cref="BackwardWalkRules.NextPhase"/>). Added when the back step starts, removed when it ends
/// (the game's animator controller then plays on). Runs on every client that sees the mount (BackwardWalkSync tells it when).
/// </summary>
internal sealed class BackwardWalkPlayer : MonoBehaviour
{
    private static readonly List<string> Names = new List<string>();

    private PlayableGraph _graph;
    private AnimationClipPlayable _clip;
    private float _length;
    private float _phase;
    private float _rate;

    /// <summary>The clip being played backwards (for logs and test hooks).</summary>
    internal string ClipName { get; private set; } = string.Empty;

    /// <summary>The current clip time (seconds): it goes DOWN while the player runs.</summary>
    internal float Phase => _phase;

    /// <summary>Whether the graph is playing now.</summary>
    internal bool Playing => _graph.IsValid() && _graph.IsPlaying();

    /// <summary>Starts (or keeps) the reversed walk on this mount. Expected outcome: true when it plays.</summary>
    internal static bool StartOn(GameObject mount)
    {
        BackwardWalkPlayer player = mount.GetComponent<BackwardWalkPlayer>();
        if (player == null)
        {
            player = mount.AddComponent<BackwardWalkPlayer>();
            player.Begin();
        }

        bool playing = player != null && player.Playing;
        ModLog.Verbose($"BackwardWalkPlayer.StartOn: {mount.name} playing {playing} (clip {player?.ClipName})");
        return playing;
    }

    /// <summary>Stops the reversed walk on this mount. Expected outcome: true when none plays afterwards.</summary>
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

    private void Begin()
    {
        Animator animator = GetComponentInChildren<Animator>();
        AnimationClip[] clips = animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.animationClips : new AnimationClip[0];
        Names.Clear();
        foreach (AnimationClip c in clips)
        {
            Names.Add(c != null ? c.name : string.Empty);
        }

        int index = BackwardWalkRules.PickWalkClip(Names);
        if (index < 0)
        {
            ModLog.Warn($"BackwardWalkPlayer: no walk clip on {name} (clips: {string.Join(", ", Names)}) - the Moonwalk shows instead");
        }
        else
        {
            AnimationClip clip = clips[index];
            _graph = PlayableGraph.Create($"WSADMounts back walk {name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "WSADMounts back walk", animator);
            _clip = AnimationClipPlayable.Create(_graph, clip);
            _clip.SetApplyFootIK(false);
            output.SetSourcePlayable(_clip);
            _length = clip.length;
            _phase = _length;
            _rate = ModSettings.BackStepSpeedFactor.Value;
            ClipName = clip.name;
            _graph.Play();
            ModLog.State($"reversed walk of {name}", "off", "on", $"clip {clip.name} {_length:0.##} s at x{_rate:0.##}, {clips.Length} clips on the animator");
        }
    }

    private void Update()
    {
        if (_graph.IsValid())
        {
            _phase = BackwardWalkRules.NextPhase(_phase, Time.deltaTime, _length, _rate);
            _clip.SetTime(_phase);
            _graph.Evaluate(0f);
            ModLog.Every("BackwardWalkPlayer.Update", 2d, $"BackwardWalkPlayer: {name} clip {ClipName} at {_phase:0.00}/{_length:0.00} s (backwards x{_rate:0.##})");
        }
    }

    private void End(string why)
    {
        bool was = _graph.IsValid();
        if (was)
        {
            _graph.Destroy();
        }

        ModLog.State($"reversed walk of {name}", was ? "on" : "off", "off", why);
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
