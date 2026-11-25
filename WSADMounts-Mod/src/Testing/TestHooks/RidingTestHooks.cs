using KydlCore.Common;
using UnityEngine;
using WSADMounts.Features.Riding.Infrastructure;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TestLab hooks for the riding feature (T-005e): spawn a tame, saddled mount in front of the arena spot, mount it through the game's own saddle
/// (Sadle.Interact), feed it input (<see cref="RideTestInput"/>, read by the real control patch), and judge what happened. Judging hooks return
/// "ok" or "bad: ..." (the lab compares with expect "ok": `do: hook` ignores min / max), and log the measured numbers either way.
/// </summary>
internal static class RidingTestHooks
{
    private static GameObject _mount;
    private static Vector3 _markPos, _markForward, _markLook;
    private static float _markTime;
    private static float _forwardSpeed = -1f;
    private static float _phaseMark = -1f;

    /// <summary>The mount the last mountSpawn made (for the other hook classes).</summary>
    internal static GameObject TestMount => _mount;

    private static Character Mount => _mount != null ? _mount.GetComponent<Character>() : null;

    private static Sadle Saddle => _mount != null ? _mount.GetComponentInChildren<Sadle>(true) : null;

    /// <summary>A tame, saddled mount (default Lox) dz metres in front of the arena spot, facing +z. Returns "spawned Lox tamed True saddled True".</summary>
    [TestHook("WSADMounts.mountSpawn")]
    private static string MountSpawn(string prefab = "Lox", double dz = 18d)
    {
        GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
        if (source == null)
        {
            ModLog.Warn($"RidingTestHooks.MountSpawn: no prefab '{prefab}'");
            return $"no prefab {prefab}";
        }

        Vector3 at = TestArena.InFront(0f, (float)dz);
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float h))
        {
            at.y = h + 0.1f;
        }

        _mount = Object.Instantiate(source, at, Quaternion.LookRotation(Vector3.forward));
        Character c = Mount;
        c?.SetTamed(true);
        ZNetView view = _mount.GetComponent<ZNetView>();
        if (view != null && view.IsValid())
        {
            view.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
            view.InvokeRPC(ZNetView.Everybody, "SetSaddle", true);
        }

        bool saddled = view != null && view.IsValid() && view.GetZDO().GetBool(ZDOVars.s_haveSaddleHash);
        string answer = $"spawned {prefab} tamed {c != null && c.IsTamed()} saddled {saddled} at {at.x:0.0},{at.z:0.0}";
        ModLog.Info($"RidingTestHooks: {answer}");
        return answer;
    }

    /// <summary>The player stands beside the mount and mounts it through the game's saddle. Returns "riding True".</summary>
    [TestHook("WSADMounts.mountRide")]
    private static string MountRide()
    {
        Player p = Player.m_localPlayer;
        Sadle saddle = Saddle;
        if (p == null || saddle == null)
        {
            ModLog.Warn($"RidingTestHooks.MountRide: player {p != null}, saddle {saddle != null}");
            return "no player or no saddle";
        }

        Vector3 at = _mount.transform.position + (_mount.transform.right * 2f);
        p.transform.position = at;
        Rigidbody body = p.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = at;
            body.linearVelocity = Vector3.zero;
        }

        saddle.Interact(p, false, false);
        string answer = $"riding {p.IsRiding()}";
        ModLog.Info($"RidingTestHooks.MountRide: {answer}");
        return answer;
    }

    /// <summary>Arms the test input (see RideTestInput). Returns "armed".</summary>
    [TestHook("WSADMounts.rideInput")]
    private static string RideInput(double x, double z, double seconds, bool run = false)
    {
        RideTestInput.Arm((float)x, (float)z, (float)seconds, run);
        return RideTestInput.Armed ? "armed" : "not armed";
    }

    /// <summary>Fakes the controller's left stick (x right, y forward, -1..1) for some seconds (see RideTestStick). Returns "armed".</summary>
    [TestHook("WSADMounts.stick")]
    private static string Stick(double x, double y, double seconds)
    {
        RideTestStick.Arm((float)x, (float)y, (float)seconds);
        return RideTestStick.Armed ? "armed" : "not armed";
    }

    /// <summary>A side camera on the test mount (see SideCamera), metres to its right. Returns "side camera on".</summary>
    [TestHook("WSADMounts.sideCam")]
    private static string SideCam(double metres = 9d)
    {
        if (_mount == null)
        {
            ModLog.Warn("RidingTestHooks.SideCam: no test mount");
            return "no mount";
        }

        SideCamera.Show(_mount.transform, (float)metres);
        return "side camera on";
    }

    /// <summary>Removes the side camera. Returns "side camera off".</summary>
    [TestHook("WSADMounts.sideCamOff")]
    private static string SideCamOff()
    {
        SideCamera.Hide("sideCamOff");
        return "side camera off";
    }

    /// <summary>Remembers where the mount is, how it faces and where the camera looks now. Returns "marked".</summary>
    [TestHook("WSADMounts.mark")]
    private static string Mark()
    {
        Character c = Mount;
        Player p = Player.m_localPlayer;
        if (c == null || p == null)
        {
            ModLog.Warn("RidingTestHooks.Mark: no mount or no player");
            return "no mount";
        }

        _markPos = c.transform.position;
        _markForward = Flat(c.transform.forward);
        _markLook = Flat(p.GetLookDir());
        _markTime = Time.time;
        ModLog.Info($"RidingTestHooks.Mark: at {_markPos} forward {_markForward} look {_markLook}");
        return "marked";
    }

    /// <summary>
    /// Judges the move since <c>mark</c>, in the camera frame of the mark: dir forward / back / right / left = at least `min` metres that way (and
    /// less sideways than along), still = at most `min` metres in total. Returns "ok" or "bad: ..." with the numbers.
    /// </summary>
    [TestHook("WSADMounts.moved")]
    private static string Moved(string dir, double min)
    {
        Character c = Mount;
        if (c == null)
        {
            return "bad: no mount";
        }

        Vector3 d = c.transform.position - _markPos;
        d.y = 0f;
        Vector3 right = new Vector3(_markLook.z, 0f, -_markLook.x);
        float along = Vector3.Dot(d, _markLook), side = Vector3.Dot(d, right);
        float seconds = Time.time - _markTime;
        bool ok;
        switch (dir)
        {
            case "forward": ok = along >= min && Mathf.Abs(side) < along; break;
            case "back": ok = -along >= min && Mathf.Abs(side) < -along; break;
            case "right": ok = side >= min && Mathf.Abs(along) < side; break;
            case "left": ok = -side >= min && Mathf.Abs(along) < -side; break;
            case "still": ok = d.magnitude <= min; break;
            default: ok = false; break;
        }

        string numbers = $"along {along:0.00} m, side {side:0.00} m, total {d.magnitude:0.00} m in {seconds:0.0} s";
        if (dir == "forward" && seconds > 0.1f)
        {
            _forwardSpeed = along / seconds;
        }

        string answer = ok ? "ok" : $"bad: {dir} >= {min:0.##} m expected - {numbers}";
        ModLog.Info($"RidingTestHooks.Moved({dir}, {min:0.##}): {(ok ? "ok" : "bad")} - {numbers}");
        return answer;
    }

    /// <summary>The mount still faces (within maxDeg) the way it faced at the mark. Returns "ok" or "bad: turned N deg".</summary>
    [TestHook("WSADMounts.facingKept")]
    private static string FacingKept(double maxDeg)
    {
        Character c = Mount;
        float turned = c != null ? Vector3.Angle(_markForward, Flat(c.transform.forward)) : 999f;
        string answer = turned <= maxDeg ? "ok" : $"bad: turned {turned:0.0} deg (max {maxDeg:0})";
        ModLog.Info($"RidingTestHooks.FacingKept: turned {turned:0.0} deg, max {maxDeg:0} -> {answer}");
        return answer;
    }

    /// <summary>Back-step speed since the mark against the forward speed measured by the last `moved forward`: ratio within [lo, hi]. Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.backSpeedRatio")]
    private static string BackSpeedRatio(double lo, double hi)
    {
        Character c = Mount;
        float seconds = Time.time - _markTime;
        float back = c != null && seconds > 0.1f ? -Vector3.Dot(c.transform.position - _markPos, _markLook) / seconds : 0f;
        float ratio = _forwardSpeed > 0.01f ? back / _forwardSpeed : 0f;
        string answer = ratio >= lo && ratio <= hi ? "ok" : $"bad: ratio {ratio:0.00} (back {back:0.00} m/s, forward {_forwardSpeed:0.00} m/s), expected {lo:0.##}-{hi:0.##}";
        ModLog.Info($"RidingTestHooks.BackSpeedRatio: back {back:0.00} m/s, forward {_forwardSpeed:0.00} m/s, ratio {ratio:0.00} (factor {Configuration.ModSettings.BackStepSpeedFactor.Value:0.##}) -> {answer}");
        return answer;
    }

    /// <summary>The mod's state of the ridden mount and its ZDO values. Returns "back True style BackwardWalk zdoBack True".</summary>
    [TestHook("WSADMounts.backState")]
    private static string BackState()
    {
        Sadle saddle = Saddle;
        MountSteerState state = saddle != null ? MountSteering.Find(saddle) : null;
        ZNetView view = _mount != null ? _mount.GetComponent<ZNetView>() : null;
        bool zdoBack = view != null && view.IsValid() && view.GetZDO().GetBool(MountSteering.ZdoBack);
        string answer = state == null ? $"no state zdoBack {zdoBack}" : $"back {state.Back} style {state.Style} zdoBack {zdoBack}";
        ModLog.Info($"RidingTestHooks.BackState: {answer}");
        return answer;
    }

    /// <summary>
    /// What ANOTHER client reads of the mount (D-012): the ZDO values the owner writes - the mod's back flag and style, and the walk animation's
    /// forward_speed that ZSyncAnimation stores for other clients (key 438569 + hash, decompiled ZSyncAnimation.SetFloat). Returns
    /// "back True style 0 forwardSpeed 1.9 owner me".
    /// </summary>
    [TestHook("WSADMounts.zdoView")]
    private static string ZdoView()
    {
        ZNetView view = _mount != null ? _mount.GetComponent<ZNetView>() : null;
        ZDO zdo = view != null && view.IsValid() ? view.GetZDO() : null;
        if (zdo == null)
        {
            ModLog.Warn("RidingTestHooks.ZdoView: no mount ZDO");
            return "no zdo";
        }

        bool back = zdo.GetBool(MountSteering.ZdoBack);
        int style = zdo.GetInt(MountSteering.ZdoStyle, -1);
        float forward = zdo.GetFloat(438569 + ZSyncAnimation.GetHash("forward_speed"), -99f);
        string owner = zdo.GetOwner() == ZDOMan.GetSessionID() ? "me" : zdo.GetOwner().ToString();
        string answer = $"back {back} style {style} forwardSpeed {(forward > 0.1f ? "positive" : "not positive")} owner {owner}";
        ModLog.Info($"RidingTestHooks.ZdoView: {answer} (forward_speed {forward:0.00})");
        return answer;
    }

    /// <summary>The back-step look on the test mount (D-032): "playing True look ReversedWalk clip Walk" or "playing False" (Moonwalk style: no clip of the mod, D-035).</summary>
    [TestHook("WSADMounts.backStepLook")]
    private static string BackStepLookNow()
    {
        BackwardWalkPlayer player = _mount != null ? _mount.GetComponent<BackwardWalkPlayer>() : null;
        string answer = player != null && player.Playing ? $"playing True look {player.Look} clip {player.ClipName}" : "playing False";
        ModLog.Info($"RidingTestHooks.BackStepLook: {answer}{(player != null ? $" at {player.Phase:0.000} s" : string.Empty)}");
        return answer;
    }

    /// <summary>Remembers the back-step clip's time. Returns "marked N" (s).</summary>
    [TestHook("WSADMounts.clipMark")]
    private static string ClipMark()
    {
        BackwardWalkPlayer player = _mount != null ? _mount.GetComponent<BackwardWalkPlayer>() : null;
        _phaseMark = player != null ? player.Phase : -1f;
        ModLog.Info($"RidingTestHooks.ClipMark: {_phaseMark:0.000} s");
        return $"marked {_phaseMark:0.000}";
    }

    /// <summary>
    /// Which way the back-step clip's time went since the mark (allowing one wrap): dir "backwards" (reversed walk) or "forwards" (hand-made walk).
    /// Returns "ok" or "bad: ...".
    /// </summary>
    [TestHook("WSADMounts.clipRuns")]
    private static string ClipRuns(string dir = "backwards")
    {
        BackwardWalkPlayer player = _mount != null ? _mount.GetComponent<BackwardWalkPlayer>() : null;
        float now = player != null ? player.Phase : -1f;
        float half = player != null ? player.Length * 0.5f : 0f;
        float delta = now - _phaseMark;
        bool forwards = (delta > 0.001f && delta < half) || delta < -half;
        bool backwards = (delta < -0.001f && delta > -half) || delta > half;
        bool ok = player != null && _phaseMark >= 0f && (dir == "forwards" ? forwards : backwards);
        string answer = ok ? "ok" : $"bad: clip time {_phaseMark:0.000} -> {now:0.000} s, wanted {dir} (player {player != null})";
        ModLog.Info($"RidingTestHooks.ClipRuns({dir}): {_phaseMark:0.000} -> {now:0.000} s -> {answer}");
        return answer;
    }

    /// <summary>The walk animation of the mount plays (its animator's forward_speed above 0.1). Returns "ok" or "bad: forward_speed N".</summary>
    [TestHook("WSADMounts.walkAnimating")]
    private static string WalkAnimating()
    {
        Animator animator = _mount != null ? _mount.GetComponentInChildren<Animator>() : null;
        float value = animator != null ? animator.GetFloat("forward_speed") : -99f;
        string answer = value > 0.1f ? "ok" : $"bad: forward_speed {value:0.00}";
        ModLog.Info($"RidingTestHooks.WalkAnimating: forward_speed {value:0.00} -> {answer}");
        return answer;
    }

    /// <summary>Disarms the test input, gets the player off and removes the test mount. Returns "reset".</summary>
    [TestHook("WSADMounts.rideReset")]
    private static string RideReset()
    {
        RideTestInput.Disarm("rideReset");
        RideTestStick.Disarm("rideReset");
        SideCamera.Hide("rideReset");
        Player p = Player.m_localPlayer;
        if (p != null && p.IsRiding())
        {
            p.StopDoodadControl();
            p.AttachStop();
        }

        if (_mount != null)
        {
            ZNetView view = _mount.GetComponent<ZNetView>();
            if (view != null && view.IsValid())
            {
                view.Destroy();
            }
            else
            {
                Object.Destroy(_mount);
            }
        }

        _mount = null;
        _forwardSpeed = -1f;
        ModLog.Info($"RidingTestHooks.RideReset: done (riding {p != null && p.IsRiding()})");
        return "reset";
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }
}
