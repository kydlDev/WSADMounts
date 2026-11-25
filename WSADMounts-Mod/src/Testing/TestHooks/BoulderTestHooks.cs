using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TestLab hooks for jumping a ridden mount onto a boulder (user 2026-11-25 17:47: "add a big boulder in front of the mounts and they jump on top
/// of it, or at least somewhat to the side ... screenshots from the side to see how the animation looks"). One of the game's rocks, scaled to a
/// flat boulder, stands in the mount's path; <see cref="BoulderWatch"/> remembers whether the mount stood on its top. TEST ONLY.
/// </summary>
internal static class BoulderTestHooks
{
    private static readonly string[] Rocks = { "rock4_forest", "rock4_coast", "rock1_mountain", "rock2_mountain", "Rock_3" };
    private static GameObject _rock;

    internal static Bounds RockBounds { get; private set; }

    /// <summary>
    /// A boulder `ahead` metres in front of the test mount's front, its centre `side` metres to the right, `height` x `size` x `size` metres.
    /// Returns "boulder rock4_forest top 1.30 m, 6.0 x 6.0 m, ...".
    /// </summary>
    [TestHook("WSADMounts.boulderSpawn")]
    private static string BoulderSpawn(double ahead = 12d, double side = 0d, double height = 1.3d, double size = 6d)
    {
        Character mount = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<Character>() : null;
        GameObject source = null;
        string name = "<none>";
        foreach (string rock in Rocks)
        {
            source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(rock) : null;
            if (source != null) { name = rock; break; }
        }

        if (mount == null || source == null)
        {
            ModLog.Warn($"BoulderTestHooks.BoulderSpawn: mount {mount != null}, rock prefab {name}");
            return "no mount or no rock";
        }

        Remove("new boulder");
        Vector3 forward = Flat(mount.transform.forward);
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float half = (float)size / 2f;
        Vector3 centre = mount.transform.position + (forward * (mount.GetRadius() + (float)ahead + half)) + (right * (float)side);
        float ground = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(centre, out float h) ? h : centre.y;

        _rock = Object.Instantiate(source, centre, Quaternion.LookRotation(forward));
        Bounds raw = BoundsOf(_rock);
        Vector3 scale = _rock.transform.localScale;
        scale.x *= (float)size / Mathf.Max(0.1f, raw.size.x);
        scale.y *= (float)height / Mathf.Max(0.1f, raw.size.y);
        scale.z *= (float)size / Mathf.Max(0.1f, raw.size.z);
        _rock.transform.localScale = scale;
        Physics.SyncTransforms();
        Bounds scaled = BoundsOf(_rock);
        _rock.transform.position += new Vector3(0f, ground - scaled.min.y, 0f);   // its base on the ground
        Physics.SyncTransforms();
        RockBounds = BoundsOf(_rock);

        BoulderWatch watch = mount.GetComponent<BoulderWatch>() ?? mount.gameObject.AddComponent<BoulderWatch>();
        watch.Begin(RockBounds);
        string answer = $"boulder {name} top {RockBounds.max.y - ground:0.00} m, {RockBounds.size.x:0.0} x {RockBounds.size.z:0.0} m, "
            + $"near edge {Vector3.Dot(RockBounds.center - mount.transform.position, forward) - (RockBounds.size.z / 2f):0.0} m ahead of the mount, side {side:0.0} m";
        ModLog.Info($"BoulderTestHooks.BoulderSpawn: {answer} (raw size {raw.size}, ground {ground:0.00})");
        return answer;
    }

    /// <summary>
    /// "jump" once the mount, at its speed now, would land in the middle of the boulder after `air` seconds in the air; "wait" before.
    /// The lab polls it with `wait until` and then presses Jump.
    /// </summary>
    [TestHook("WSADMounts.boulderJumpPoint")]
    private static string BoulderJumpPoint(double air = 0.8d)
    {
        Character mount = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<Character>() : null;
        if (mount == null || _rock == null)
        {
            ModLog.Warn($"BoulderTestHooks.BoulderJumpPoint: mount {mount != null}, boulder {_rock != null}");
            return "no mount or no boulder";
        }

        Vector3 forward = Flat(mount.transform.forward);
        float toMiddle = Vector3.Dot(RockBounds.center - mount.transform.position, forward);
        Vector3 v = mount.GetVelocity();
        float speed = new Vector2(v.x, v.z).magnitude;
        bool now = toMiddle <= speed * (float)air;
        if (now || ModLog.IsDue("BoulderTestHooks.JumpPoint", 0.5d))
        {
            ModLog.Info($"BoulderTestHooks.BoulderJumpPoint: middle {toMiddle:0.0} m ahead, speed {speed:0.0} m/s x {air:0.0} s = {speed * air:0.0} m -> {(now ? "jump" : "wait")}");
        }

        return now ? "jump" : "wait";
    }

    /// <summary>Whether the mount stood on the boulder's top (grounded, over it, at its height) since boulderSpawn. Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.boulderLanded")]
    private static string BoulderLanded()
    {
        BoulderWatch watch = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<BoulderWatch>() : null;
        string facts = watch != null ? watch.Facts() : "no watch";
        bool landed = watch != null && watch.LandedOnTop;
        ModLog.Info($"BoulderTestHooks.BoulderLanded: {facts} -> {(landed ? "ok" : "bad")}");
        return landed ? "ok" : $"bad: not on top - {facts}";
    }

    /// <summary>Stops the lab's ride input now (the mount stays where it is). Returns "input stopped".</summary>
    [TestHook("WSADMounts.rideInputStop")]
    private static string RideInputStop()
    {
        RideTestInput.Disarm("rideInputStop hook");
        return "input stopped";
    }

    /// <summary>Removes the boulder. Returns "boulder removed".</summary>
    [TestHook("WSADMounts.boulderRemove")]
    private static string BoulderRemove()
    {
        Remove("boulderRemove hook");
        return "boulder removed";
    }

    internal static void Remove(string why)
    {
        bool had = _rock != null;
        if (had)
        {
            ZNetView view = _rock.GetComponent<ZNetView>();
            if (view != null && view.IsValid()) view.Destroy();
            else Object.Destroy(_rock);
        }

        _rock = null;
        ModLog.Verbose($"BoulderTestHooks.Remove: {why}, had a boulder {had}");
    }

    private static Bounds BoundsOf(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (Collider c in colliders)
        {
            if (!c.enabled || c.isTrigger) continue;
            if (first) { bounds = c.bounds; first = false; }
            else bounds.Encapsulate(c.bounds);
        }

        ModLog.Verbose($"BoulderTestHooks.BoundsOf: {go.name} {colliders.Length} collider(s) -> {bounds.size} at {bounds.center}");
        return bounds;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }
}
