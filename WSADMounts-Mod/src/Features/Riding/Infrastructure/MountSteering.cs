using System;
using System.Collections.Generic;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Contracts;
using UnityEngine;
using WSADMounts.Configuration;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// The game side of the steering (T-005): turns the game's control input into a <see cref="RideSteeringRules"/> decision, keeps the per-mount
/// state, and answers the per-frame patches quickly (rule 38: <see cref="AnyBacking"/> is checked before anything else).
/// </summary>
internal static class MountSteering
{
    /// <summary>ZDO keys other players read: the mount steps backwards, and the rider's backward animation style (D-012).</summary>
    internal const string ZdoBack = "WSADMounts_back", ZdoStyle = "WSADMounts_style";

    private static readonly Dictionary<Sadle, MountSteerState> States = new Dictionary<Sadle, MountSteerState>();
    private static readonly HashSet<Character> Backing = new HashSet<Character>();
    private static readonly HashSet<ZSyncAnimation> BackingAnims = new HashSet<ZSyncAnimation>();
    private static readonly List<Sadle> Gone = new List<Sadle>();

    /// <summary>True while at least one mount this client owns steps backwards (the per-character patch returns at once otherwise).</summary>
    internal static bool AnyBacking => Backing.Count > 0;

    /// <summary>Whether this mount steps backwards now (only mounts this client drives).</summary>
    internal static bool IsBacking(Character mount) => Backing.Contains(mount);

    /// <summary>Whether this animation sync belongs to a mount that steps back now.</summary>
    internal static bool IsBacking(ZSyncAnimation anim) => BackingAnims.Contains(anim);

    /// <summary>The mount's state, made (and tracked by KydlCore's flight recorder) on first use. Expected outcome: never null for a live saddle.</summary>
    internal static MountSteerState For(Sadle saddle, Character mount)
    {
        if (!States.TryGetValue(saddle, out MountSteerState state))
        {
            Prune();
            state = new MountSteerState(saddle, mount);
            States[saddle] = state;
            bool tracked = FlightRecorder.Track(mount, "WSADMounts ridden mount", state.Facts);
            ModLog.Verbose($"MountSteering: new state for {Name(mount)} (tracked {tracked}, {States.Count} mount(s) known)");
        }

        ModLog.Every("MountSteering.For", 5d, $"MountSteering: {States.Count} mount(s) known, {Backing.Count} stepping back");
        return state;
    }

    /// <summary>The state if the mod knows this saddle (null otherwise: the mount has never been ridden with the custom controls).</summary>
    internal static MountSteerState Find(Sadle saddle)
    {
        States.TryGetValue(saddle, out MountSteerState state);
        if (ModLog.IsDue("MountSteering.Find", 5d)) ModLog.Verbose($"MountSteering.Find: {(state == null ? "unknown saddle" : state.Facts())}");
        return state;
    }

    /// <summary>
    /// One control tick of the local rider. Expected outcome: the decision the rule took (the caller sends its speed and direction with the
    /// game's own Controls RPC). A back step is only applied while this client owns the mount; otherwise it walks that way instead.
    /// </summary>
    internal static RideCommand Steer(MountSteerState state, Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block, bool isOwner)
    {
        var input = new RideInput(moveDir.x, moveDir.z, lookDir.x, lookDir.z, run, autoRun, block, state.Cruising);
        RideCommand command = RideSteeringRules.Decide(input);
        state.Cruising = command.Cruising;
        bool back = command.Back && isOwner;
        if (command.Back && !isOwner && ModLog.IsDue("MountSteering.notOwner", 2d))
        {
            ModLog.Warn($"MountSteering: back step asked for {Name(state.Mount)} but this client does not own it yet - it walks that way instead");
        }

        state.Face = new Vector3(command.FaceX, 0f, command.FaceZ);
        state.Move = new Vector3(command.MoveX, 0f, command.MoveZ);
        state.Style = ModSettings.BackwardAnimationStyle?.Value ?? BackwardAnimation.BackwardWalk;
        SetBack(state, back);
        if (!string.Equals(command.Reason, state.LastReason, StringComparison.Ordinal))
        {
            ModLog.Decision($"steering of {Name(state.Mount)}", command.Reason, $"move=({moveDir.x:0.##},{moveDir.z:0.##}) run={run} block={block} owner={isOwner} -> speed {command.Speed}, back {back}");
            state.LastReason = command.Reason;
        }

        if (isOwner)
        {
            WriteZdo(state);
        }

        ModLog.Every("MountSteering.Steer", 2d, $"MountSteering: {Name(state.Mount)} {state.Facts()} speed={command.Speed}");
        return command;
    }

    /// <summary>The game reset the controls (rider left, saddle removed). Expected outcome: true when the mount no longer steps back.</summary>
    internal static bool Reset(Sadle saddle)
    {
        MountSteerState state = Find(saddle);
        if (state != null)
        {
            state.Cruising = false;
            state.LastReason = string.Empty;
            SetBack(state, false);
            ZNetView view = state.Mount != null ? state.Mount.GetComponent<ZNetView>() : null;
            if (view != null && view.IsValid() && view.IsOwner())
            {
                WriteZdo(state);
            }
        }

        bool stopped = state == null || !state.Back;
        ModLog.Verbose($"MountSteering.Reset: {(state == null ? "unknown saddle" : Name(state.Mount))}, stepping back now {!stopped}");
        return stopped;
    }

    private static void SetBack(MountSteerState state, bool back)
    {
        if (state.Back != back)
        {
            ModLog.State($"back step of {Name(state.Mount)}", state.Back.ToString(), back.ToString(), $"style {state.Style}, face=({state.Face.x:0.##},{state.Face.z:0.##})");
        }

        bool ended = state.Back && !back;
        state.Back = back;
        if (ended && state.Mount != null)
        {
            // the back step set a backward move direction on the creature; left there, Character.UpdateRotation (no longer hidden by
            // CharacterUpdateRotationPatch) turns the mount round toward it after the rider lets go (lab a2full1-1, D-029)
            state.Mount.SetMoveDir(Vector3.zero);
            ModLog.Verbose($"MountSteering.SetBack: back step of {Name(state.Mount)} ended - move direction cleared so it does not turn round");
        }

        if (back)
        {
            Backing.Add(state.Mount);
            if (state.Anim != null)
            {
                BackingAnims.Add(state.Anim);
            }
        }
        else
        {
            Backing.Remove(state.Mount);
            if (state.Anim != null)
            {
                BackingAnims.Remove(state.Anim);
            }
        }
    }

    private static void WriteZdo(MountSteerState state)
    {
        int style = (int)state.Style;
        bool changed = state.WrittenBack != state.Back || state.WrittenStyle != style;
        ZNetView view = changed && state.Mount != null ? state.Mount.GetComponent<ZNetView>() : null;
        ZDO zdo = view != null && view.IsValid() ? view.GetZDO() : null;
        if (zdo != null)
        {
            zdo.Set(ZdoBack, state.Back);
            zdo.Set(ZdoStyle, style);
            state.WrittenBack = state.Back;
            state.WrittenStyle = style;
            ModLog.Verbose($"MountSteering.WriteZdo: {Name(state.Mount)} {ZdoBack}={state.Back} {ZdoStyle}={state.Style}");
        }
        else if (changed)
        {
            ModLog.Verbose($"MountSteering.WriteZdo: {Name(state.Mount)} has no valid ZDO, nothing written");
        }
    }

    /// <summary>Forgets saddles whose game objects are gone (zone unloaded, mount died), so the dictionary never grows without end.</summary>
    private static void Prune()
    {
        Gone.Clear();
        foreach (KeyValuePair<Sadle, MountSteerState> pair in States)
        {
            if (pair.Key == null || pair.Value.Mount == null)
            {
                Gone.Add(pair.Key);
            }
        }

        foreach (Sadle saddle in Gone)
        {
            Backing.Remove(States[saddle].Mount);
            if (States[saddle].Anim != null)
            {
                BackingAnims.Remove(States[saddle].Anim);
            }
            States.Remove(saddle);
        }

        if (Gone.Count > 0)
        {
            ModLog.Verbose($"MountSteering.Prune: forgot {Gone.Count} gone mount(s), {States.Count} left");
        }
    }

    private static string Name(Character mount) => mount != null ? $"{mount.m_name} ({mount.GetInstanceID()})" : "<none>";
}
