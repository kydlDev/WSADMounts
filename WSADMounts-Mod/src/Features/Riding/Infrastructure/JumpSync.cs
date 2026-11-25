using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Tells every client that a mount jumps (D-040): the mount's owner sends one routed RPC to everybody at the key press, each client starts
/// <see cref="JumpClipPlayer"/> on its own copy. A jump is short, so a player who arrives in the middle of one simply sees the game's pose.
/// </summary>
internal static class JumpSync
{
    internal const string Rpc = "WSADMounts_Jump_v1";

    private static ZRoutedRpc _registered;

    /// <summary>Registers the RPC once per network session. Expected outcome: true when registered.</summary>
    internal static bool Register()
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (rpc != null && !ReferenceEquals(rpc, _registered))
        {
            rpc.Register<ZDOID>(Rpc, OnJump);
            _registered = rpc;
            ModLog.Verbose($"JumpSync: RPC {Rpc} registered");
        }

        bool done = rpc != null && ReferenceEquals(rpc, _registered);
        if (ModLog.IsDue("JumpSync.Register", 10d)) ModLog.Verbose($"JumpSync.Register: {(rpc == null ? "no ZRoutedRpc yet" : "ok")} -> {done}");
        return done;
    }

    /// <summary>The owner's mount starts a jump: tell everybody (this client included). Expected outcome: true when sent.</summary>
    internal static bool Broadcast(Character mount)
    {
        ZNetView view = mount != null ? mount.GetComponent<ZNetView>() : null;
        bool send = ZRoutedRpc.instance != null && view != null && view.IsValid();
        if (send)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpc, view.GetZDO().m_uid);
        }

        ModLog.Verbose($"JumpSync.Broadcast: {(mount != null ? mount.m_name : "<none>")} sent {send}");
        return send;
    }

    private static void OnJump(long sender, ZDOID id)
    {
        GameObject go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(id) : null;
        bool started = go != null && JumpClipPlayer.StartOn(go);
        ModLog.Decision($"jump of {(go != null ? go.name : id.ToString())} seen here", started ? "jump clip" : "game pose", $"RPC from {sender}, loaded {go != null}");
    }
}
