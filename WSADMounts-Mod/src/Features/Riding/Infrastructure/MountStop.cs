using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Features.Riding.Infrastructure;

/// <summary>
/// Instant stop of a ridden mount (D-041, user 2026-11-25: "I want stop to be instant and to be able to precision stop on a spot"). The game
/// lets a stopped mount slide: Character.UpdateWalking eases its stored velocity (m_currentVel) toward zero by the acceleration, and
/// Sadle.UpdateRiding returns false on Stop, so MonsterAI then runs its own branches (lab a3g16c-3: the Lox walked 6.3 m on its own after
/// the stop). While <see cref="RideTuningRules.HoldStill"/> says so, the owner zeroes the stored and the body's sideways velocity and stops
/// the AI's movement every AI tick; the caller then reports the mount as ridden. Runs on the owner only (MonsterAI.UpdateAI).
/// </summary>
internal static class MountStop
{
    private const int StopSpeed = 0;   // the game's Sadle.Speed.Stop (GameEnumsTests pins RideSpeed to it)

    private static readonly AccessTools.FieldRef<Character, Vector3> CurrentVel = AccessTools.FieldRefAccess<Character, Vector3>("m_currentVel");
    private static readonly AccessTools.FieldRef<Sadle, Vector3> ControlDir = AccessTools.FieldRefAccess<Sadle, Vector3>("m_controlDir");
    private static readonly System.Reflection.FieldInfo SpeedField = AccessTools.Field(typeof(Sadle), "m_speed");

    /// <summary>Holds a stopped ridden mount still. Expected outcome: true when it was held this tick (its speed on the ground is then 0).</summary>
    internal static bool Hold(Sadle saddle, Character mount, MonsterAI ai)
    {
        int speed = SpeedField != null ? (int)SpeedField.GetValue(saddle) : -1;
        bool stop = speed == StopSpeed || ControlDir(saddle).sqrMagnitude < 1e-6f;
        bool hold = RideTuningRules.HoldStill(true, stop, mount.IsOnGround());
        if (!hold)
        {
            if (ModLog.IsDue("MountStop.moving", 5d)) ModLog.Verbose($"MountStop: {mount.m_name} not held (saddle speed {speed}, stop {stop}, on ground {mount.IsOnGround()})");
            return hold;
        }

        Vector3 before = mount.GetVelocity();
        CurrentVel(mount) = Vector3.zero;
        Rigidbody body = mount.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
        }

        ai?.StopMoving();
        mount.SetMoveDir(Vector3.zero);
        ModLog.Every("MountStop.hold", 2d, $"MountStop: {mount.m_name} held still (was {new Vector2(before.x, before.z).magnitude:0.00} m/s on the ground)");
        return hold;
    }
}
