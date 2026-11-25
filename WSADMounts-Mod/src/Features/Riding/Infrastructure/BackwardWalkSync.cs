using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Tells every client when a mount starts or stops stepping back, and with which style (D-012, D-031): the mount's owner (the rider) sends one
/// routed RPC to everybody on each change; each client starts / stops <see cref="BackwardWalkPlayer"/> on its own copy of the mount. Players who
/// arrive later read the same facts from the mount's ZDO when it loads (<see cref="ApplyFromZdo"/>).
/// </summary>
internal static class BackwardWalkSync
{
    internal const string Rpc = "WSADMounts_BackStep_v1";

    private static ZRoutedRpc _registered;

    /// <summary>Registers the RPC once per network session (ZRoutedRpc is made new for every world). Expected outcome: true when registered.</summary>
    internal static bool Register()
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (rpc != null && !ReferenceEquals(rpc, _registered))
        {
            rpc.Register<ZDOID, bool, int>(Rpc, OnBackStep);
            _registered = rpc;
            ModLog.Verbose($"BackwardWalkSync: RPC {Rpc} registered");
        }

        bool done = rpc != null && ReferenceEquals(rpc, _registered);
        ModLog.Verbose($"BackwardWalkSync.Register: {(rpc == null ? "no ZRoutedRpc yet" : "ok")} -> {done}");
        return done;
    }

    /// <summary>The owner's back step changed: tell everybody (this client included).</summary>
    internal static void Broadcast(Character mount, bool back, BackwardAnimation style)
    {
        ZNetView view = mount != null ? mount.GetComponent<ZNetView>() : null;
        if (ZRoutedRpc.instance != null && view != null && view.IsValid())
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpc, view.GetZDO().m_uid, back, (int)style);
            ModLog.Verbose($"BackwardWalkSync.Broadcast: {mount.m_name} back {back} style {style}");
        }
        else
        {
            ModLog.Verbose($"BackwardWalkSync.Broadcast: not sent (rpc {ZRoutedRpc.instance != null}, view valid {view != null && view.IsValid()})");
        }
    }

    /// <summary>A mount loaded on this client (Sadle.Awake): start the reversed walk if its ZDO says it is stepping back.</summary>
    internal static void ApplyFromZdo(Character mount)
    {
        ZNetView view = mount != null ? mount.GetComponent<ZNetView>() : null;
        ZDO zdo = view != null && view.IsValid() ? view.GetZDO() : null;
        bool back = zdo != null && zdo.GetBool(MountSteering.ZdoBack);
        if (back)
        {
            Apply(mount.gameObject, true, (BackwardAnimation)zdo.GetInt(MountSteering.ZdoStyle, 0), "ZDO on load");
        }
        else
        {
            ModLog.Every("BackwardWalkSync.ApplyFromZdo", 10d, $"BackwardWalkSync: {(mount != null ? mount.m_name : "<none>")} loaded, not stepping back");
        }
    }

    private static void OnBackStep(long sender, ZDOID id, bool back, int style)
    {
        GameObject go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(id) : null;
        if (go != null)
        {
            Apply(go, back, (BackwardAnimation)style, $"RPC from {sender}");
        }
        else
        {
            ModLog.Verbose($"BackwardWalkSync.OnBackStep: mount {id} is not loaded here (back {back}) - its ZDO is read when it loads");
        }
    }

    private static void Apply(GameObject go, bool back, BackwardAnimation style, string why)
    {
        bool reversed = back && BackwardWalkRules.UsesReversedWalk(go.name, style);
        bool result = reversed ? BackwardWalkPlayer.StartOn(go) : BackwardWalkPlayer.StopOn(go, back ? $"style {style} on this mount = Moonwalk" : "back step ended");
        ModLog.Decision($"look of {go.name}'s back step", reversed ? "reversed walk" : back ? "moonwalk" : "normal", $"back {back}, style {style}, {why}, applied {result}");
    }
}
