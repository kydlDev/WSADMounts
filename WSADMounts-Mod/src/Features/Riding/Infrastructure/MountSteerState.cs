using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// What the mod knows about one saddled mount: the rider's cruise state and the last steering decision (back step, facing, move direction,
/// the rider's animation style). Lives in <see cref="MountSteering"/>; the rider owns the mount while riding (Sadle.RPC_RequestControl gives it
/// the ZDO), so the same client decides, moves and writes the ZDO values other players read.
/// </summary>
internal sealed class MountSteerState
{
    internal MountSteerState(Sadle saddle, Character mount)
    {
        Saddle = saddle;
        Mount = mount;
        Anim = mount != null ? mount.GetComponent<ZSyncAnimation>() : null;
        ModLog.Verbose($"MountSteerState: made for {(mount != null ? mount.m_name : "<none>")} (saddle {(saddle != null ? saddle.name : "<none>")}, animation sync {Anim != null})");
    }

    internal Sadle Saddle { get; }

    internal Character Mount { get; }

    /// <summary>The mount's animation sync (its forward_speed is flipped while it steps back: Moonwalk).</summary>
    internal ZSyncAnimation Anim { get; }

    internal bool Cruising { get; set; }

    /// <summary>S that ended an auto-run is still held: it does not step back until released (D-041).</summary>
    internal bool BackLatched { get; set; }

    internal bool Back { get; set; }

    internal Vector3 Face { get; set; }

    internal Vector3 Move { get; set; }

    internal BackwardAnimation Style { get; set; }

    internal string LastReason { get; set; } = string.Empty;

    /// <summary>The values last written to the mount's ZDO (written again only when they change).</summary>
    internal bool WrittenBack { get; set; }

    internal int WrittenStyle { get; set; } = -1;

    /// <summary>One line for the flight recorder and the debug log.</summary>
    internal string Facts() =>
        $"back={Back} style={Style} cruising={Cruising} face=({Face.x:0.##},{Face.z:0.##}) move=({Move.x:0.##},{Move.z:0.##}) last={LastReason}";
}
