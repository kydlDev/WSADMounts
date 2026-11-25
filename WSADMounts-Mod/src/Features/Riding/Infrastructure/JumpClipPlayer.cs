using KydlCore.Common;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Plays a mount's hand-made jump clip (D-040) on every client that sees the jump: a PlayableGraph with one AnimationClipPlayable on the
/// mount's Animator, its time driven by <see cref="JumpClipRules.NextTime"/> from the real flight (the animator's synced "onGround" bool, so
/// it works on the owner and on every other client alike). Added at the key press (JumpSync), removed when the clip is over; the game's
/// animator controller then plays on. A mount without a jump clip (modded) keeps the game's pose.
/// </summary>
internal sealed class JumpClipPlayer : MonoBehaviour
{
    // ZSyncAnimation keeps an animator bool in the mount's ZDO as an int 1 / 0 at 438569 + its hash (decompiled SetBool, Valheim 1.0.30);
    // the Animator's own parameter is NOT readable this way (lab a4jclip1: "on ground False" the whole jump, the clip held 6 s)
    private static readonly int OnGroundZdoKey = 438569 + ZSyncAnimation.GetHash("onGround");

    private PlayableGraph _graph;
    private AnimationClipPlayable _clip;
    private Animator _animator;
    private Character _character;
    private ZNetView _view;
    private float _time;
    private float _elapsed;
    private bool _leftGround;
    private float _held;

    /// <summary>What the last finished jump clip did on this client (lab hooks and logs), e.g. "WSADMounts_Lox_Jump: left the ground True, held 0.10 s, ended".</summary>
    internal static string LastReport { get; set; } = "none";

    /// <summary>Starts the jump clip on this mount (again from the start when one is still playing). Expected outcome: true when a clip plays.</summary>
    internal static bool StartOn(GameObject mount)
    {
        AnimationClip clip = mount != null ? HandMadeClips.JumpFor(mount.name) : null;
        Animator animator = mount != null ? mount.GetComponentInChildren<Animator>() : null;
        if (clip == null || animator == null)
        {
            ModLog.Verbose($"JumpClipPlayer.StartOn: {(mount != null ? mount.name : "<none>")} keeps the game's pose (clip {clip != null}, animator {animator != null})");
            return clip != null && animator != null;
        }

        BackwardWalkPlayer.StopOn(mount, "jump");
        JumpClipPlayer player = mount.GetComponent<JumpClipPlayer>() ?? mount.AddComponent<JumpClipPlayer>();
        player.Begin(animator, clip);
        bool playing = player._graph.IsValid() && player._graph.IsPlaying();
        ModLog.Verbose($"JumpClipPlayer.StartOn: {mount.name} clip {clip.name} {clip.length:0.00} s -> playing {playing}");
        return playing;
    }

    private void Begin(Animator animator, AnimationClip clip)
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }

        _animator = animator;
        _character = GetComponent<Character>();
        _view = GetComponent<ZNetView>();
        _graph = PlayableGraph.Create($"WSADMounts jump {name}");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var output = AnimationPlayableOutput.Create(_graph, "WSADMounts jump", animator);
        _clip = AnimationClipPlayable.Create(_graph, clip);
        _clip.SetApplyFootIK(false);
        output.SetSourcePlayable(_clip);
        _time = 0f;
        _elapsed = 0f;
        _held = 0f;
        _leftGround = false;
        _graph.Play();
        ModLog.State($"jump look of {name}", "none", clip.name, "jump pressed");
    }

    private void Update()
    {
        if (!_graph.IsValid())
        {
            if (ModLog.IsDue("JumpClipPlayer.noGraph", 5d)) ModLog.Verbose($"JumpClipPlayer: {name} has no graph (ended)");
            return;
        }

        bool onGround = OnGround();
        _leftGround |= !onGround && _time >= JumpClipRules.PushTime;
        _elapsed += Time.deltaTime;
        float next = JumpClipRules.NextTime(_time, Time.deltaTime, onGround, _leftGround, _elapsed);
        if (Mathf.Approximately(next, JumpClipRules.HoldTime) && Mathf.Approximately(_time, JumpClipRules.HoldTime))
        {
            _held += Time.deltaTime;
        }

        if (next < 0f)
        {
            End("clip over");
            Destroy(this);
            ModLog.Verbose($"JumpClipPlayer: {name} finished - {LastReport}");
            return;
        }

        _time = next;
        _clip.SetTime(_time);
        _graph.Evaluate(0f);
        ModLog.Every("JumpClipPlayer.Update", 0.5d, $"JumpClipPlayer: {name} at {_time:0.00}/{JumpClipRules.EndTime:0.00} s, on ground {onGround}, left ground {_leftGround}, held {_held:0.00} s");
    }

    /// <summary>The mount stands on the ground: the owner asks the game's physics, every other client reads the synced animator bool.</summary>
    private bool OnGround()
    {
        bool owner = _view != null && _view.IsValid() && _view.IsOwner();
        ZDO zdo = _view != null && _view.IsValid() ? _view.GetZDO() : null;
        bool result = owner && _character != null ? _character.IsOnGround() : zdo == null || zdo.GetInt(OnGroundZdoKey, 1) == 1;
        if (ModLog.IsDue("JumpClipPlayer.OnGround", 1d)) ModLog.Verbose($"JumpClipPlayer.OnGround: {name} owner {owner} -> {result}");
        return result;
    }

    private void End(string why)
    {
        LastReport = $"{(_clip.IsValid() ? _clip.GetAnimationClip().name : "?")}: left the ground {_leftGround}, held {_held:0.00} s, {_elapsed:0.00} s, ended ({why})";
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }

        ModLog.State($"jump look of {name}", "jump clip", "none", LastReport);
    }

    private void OnDestroy()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
            ModLog.Verbose($"JumpClipPlayer.OnDestroy: {name} graph destroyed with the mount");
        }
    }
}
