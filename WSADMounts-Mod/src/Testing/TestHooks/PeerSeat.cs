using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TEST ONLY (T-007c, user 2026-11-25 "have you tried doing this? like really tried?"): shows the lab's second player SEATED on the test
/// mount. A rider's client keeps its character on the saddle's attach point (Player.AttachStart / UpdateAttach) and sets the saddle's attach
/// animation bool, both synced to other clients through the character's ZDO. The virtual player runs no game, so this component does it for
/// her on this client: every frame her character's ZDO position / rotation = the attach point, her ZDO animation bool = the saddle's attach
/// animation. Added by the peerSeat hook, removed with the mount (rideReset) or after <see cref="MaxSeconds"/> (a failed test never resets).
/// </summary>
internal sealed class PeerSeat : MonoBehaviour
{
    private const float MaxSeconds = 120f;
    private const int ZsyncBoolKey = 438569;   // ZSyncAnimation keeps a bool as an int 1 / 0 at 438569 + hash in the ZDO (decompiled SetBool, Valheim 1.0.30)

    private Sadle _saddle;
    private ZDO _rider;
    private GameObject _riderObject;
    private int _animKey;
    private float _until;

    /// <summary>The rider's character ZDO being seated (for the hook's answer).</summary>
    internal ZDO Rider => _rider;

    /// <summary>Seats `rider` on this mount's saddle. Expected outcome: true when a saddle with an attach point was found.</summary>
    internal bool Begin(ZDO rider)
    {
        _saddle = GetComponentInChildren<Sadle>(true);
        _rider = rider;
        _riderObject = ZNetScene.instance != null && rider != null ? ZNetScene.instance.FindInstance(rider.m_uid) : null;
        _animKey = _saddle != null ? ZsyncBoolKey + ZSyncAnimation.GetHash(_saddle.m_attachAnimation) : 0;
        _until = Time.time + MaxSeconds;
        bool ready = _saddle != null && _saddle.m_attachPoint != null && _rider != null;
        if (ready)
        {
            _rider.Set(_animKey, 1);
        }

        ModLog.Info($"PeerSeat.Begin: saddle {_saddle != null}, attach point {_saddle != null && _saddle.m_attachPoint != null}, rider {_rider?.m_uid.ToString() ?? "<none>"}, rider object {_riderObject != null}, animation '{(_saddle != null ? _saddle.m_attachAnimation : "<none>")}' -> {ready}");
        return ready;
    }

    private void LateUpdate()
    {
        if (_saddle == null || _saddle.m_attachPoint == null || _rider == null || Time.time > _until)
        {
            ModLog.Info($"PeerSeat: stops (saddle {_saddle != null}, rider {_rider != null}, expired {Time.time > _until})");
            Destroy(this);
            return;
        }

        Transform seat = _saddle.m_attachPoint;
        _rider.SetPosition(seat.position);
        _rider.SetRotation(seat.rotation);
        if (_riderObject != null)
        {
            _riderObject.transform.SetPositionAndRotation(seat.position, seat.rotation);
        }

        ModLog.Every("PeerSeat.LateUpdate", 2d, $"PeerSeat: rider {_rider.m_uid} on the saddle of {name} at {seat.position}, object {_riderObject != null}");
    }

    private void OnDestroy()
    {
        if (_rider != null && _animKey != 0)
        {
            _rider.Set(_animKey, 0);
        }

        ModLog.Info($"PeerSeat.OnDestroy: rider {(_rider != null ? _rider.m_uid.ToString() : "<none>")} off the saddle of {name}");
    }
}
