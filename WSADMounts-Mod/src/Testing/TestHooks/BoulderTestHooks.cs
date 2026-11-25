using System.Collections.Generic;
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
    // lab a4bld1: rock4_forest's collision box (27.8 x 16.8 x 24.2 m) is far bigger than the rock you see, so it floated as a thin slab;
    // a rock is used only when its colliders match its visible mesh (BoulderSpawn checks it)
    private static readonly string[] Rocks = { "rock1_mountain", "rock2_mountain", "rock3_mountain", "rock4_coast", "rock2_heath", "rock3_ice", "rock4_forest", "Rock_3", "Rock_4" };
    private static readonly List<GameObject> Placed = new List<GameObject>();
    private static readonly List<Bounds> PlacedBounds = new List<Bounds>();
    private static GameObject _rock;
    private static Vector3 _forward = Vector3.forward;

    /// <summary>
    /// A boulder `ahead` metres in front of the test mount's front, its centre `side` metres to the right, `height` x `size` x `size` metres.
    /// With `behind` true it stands right behind the last boulder instead (a step: user 2026-11-25, D-039 "in 2 jumps to get on top"); boulder
    /// numbers count from 1 in spawn order. Returns "boulder 1 rock1_mountain top 1.30 m, 6.0 x 6.0 m, ...".
    /// </summary>
    [TestHook("WSADMounts.boulderSpawn")]
    private static string BoulderSpawn(double ahead = 12d, double side = 0d, double height = 1.3d, double size = 6d, bool behind = false)
    {
        Character mount = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<Character>() : null;
        if (mount == null)
        {
            ModLog.Warn("BoulderTestHooks.BoulderSpawn: no test mount");
            return "no mount";
        }

        if (!behind || PlacedBounds.Count == 0)
        {
            Remove("new boulder");
            _forward = Flat(mount.transform.forward);
        }

        Vector3 forward = _forward;
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float half = (float)size / 2f;
        Vector3 centre = behind && PlacedBounds.Count > 0
            ? Behind(PlacedBounds[PlacedBounds.Count - 1], forward, half)
            : mount.transform.position + (forward * (mount.GetRadius() + (float)ahead + half)) + (right * (float)side);
        float ground = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(centre, out float h) ? h : centre.y;

        string name = PickRock(centre, forward);
        if (_rock == null)
        {
            ModLog.Warn($"BoulderTestHooks.BoulderSpawn: no rock whose colliders match its mesh among {Rocks.Length}");
            return "no fitting rock";
        }

        Bounds raw = Visible(_rock);
        Vector3 scale = _rock.transform.localScale;
        scale.x *= (float)size / Mathf.Max(0.1f, raw.size.x);
        scale.y *= (float)height / Mathf.Max(0.1f, raw.size.y);
        scale.z *= (float)size / Mathf.Max(0.1f, raw.size.z);
        _rock.transform.localScale = scale;
        Physics.SyncTransforms();
        Bounds scaled = Visible(_rock);
        _rock.transform.position += new Vector3(0f, ground - scaled.min.y + 0.05f, 0f);   // the visible base 5 cm above the ground
        Physics.SyncTransforms();
        Bounds rockBounds = BoundsOf(_rock);
        Placed.Add(_rock);
        PlacedBounds.Add(rockBounds);
        ModLog.Check("BoulderTestHooks: boulder collider top above the ground", rockBounds.max.y - ground, (float)height, 0.3f, "m");

        BoulderWatch watch = mount.GetComponent<BoulderWatch>() ?? mount.gameObject.AddComponent<BoulderWatch>();
        watch.Begin(PlacedBounds);
        string answer = $"boulder {PlacedBounds.Count} {name} top {rockBounds.max.y - ground:0.00} m, {rockBounds.size.x:0.0} x {rockBounds.size.z:0.0} m, "
            + $"near edge {Vector3.Dot(rockBounds.center - mount.transform.position, forward) - (rockBounds.size.z / 2f):0.0} m ahead of the mount, side {side:0.0} m, behind the last {behind}";
        ModLog.Info($"BoulderTestHooks.BoulderSpawn: {answer} (raw size {raw.size}, ground {ground:0.00})");
        return answer;
    }

    /// <summary>
    /// "jump" once the mount stands on the ground and, at its speed now, would land in the middle of boulder `rock` (from 1) after `air` seconds
    /// in the air; "wait" before. The lab polls it with `wait until` and then presses Jump.
    /// </summary>
    [TestHook("WSADMounts.boulderJumpPoint")]
    private static string BoulderJumpPoint(double air = 0.8d, int rock = 1)
    {
        Character mount = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<Character>() : null;
        if (mount == null || rock < 1 || rock > PlacedBounds.Count)
        {
            ModLog.Warn($"BoulderTestHooks.BoulderJumpPoint: mount {mount != null}, boulder {rock} of {PlacedBounds.Count}");
            return "no mount or no boulder";
        }

        Vector3 forward = Flat(mount.transform.forward);
        float toMiddle = Vector3.Dot(PlacedBounds[rock - 1].center - mount.transform.position, forward);
        Vector3 v = mount.GetVelocity();
        float speed = new Vector2(v.x, v.z).magnitude;
        bool grounded = mount.IsOnGround();
        bool now = grounded && toMiddle <= speed * (float)air;
        if (now || ModLog.IsDue("BoulderTestHooks.JumpPoint", 0.5d))
        {
            ModLog.Info($"BoulderTestHooks.BoulderJumpPoint: boulder {rock} middle {toMiddle:0.0} m ahead, speed {speed:0.0} m/s x {air:0.0} s = {speed * air:0.0} m, on ground {grounded} -> {(now ? "jump" : "wait")}");
        }

        return now ? "jump" : "wait";
    }

    /// <summary>Whether the mount stood on top of boulder `rock` (from 1: grounded, over it, at its height) since boulderSpawn. Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.boulderLanded")]
    private static string BoulderLanded(int rock = 1)
    {
        BoulderWatch watch = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<BoulderWatch>() : null;
        string facts = watch != null ? watch.Facts() : "no watch";
        bool landed = watch != null && watch.LandedOn(rock - 1);
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
        int had = 0;
        foreach (GameObject rock in Placed)
        {
            if (rock == null) continue;
            had++;
            ZNetView view = rock.GetComponent<ZNetView>();
            if (view != null && view.IsValid()) view.Destroy();
            else Object.Destroy(rock);
        }

        Placed.Clear();
        PlacedBounds.Clear();
        _rock = null;
        ModLog.Verbose($"BoulderTestHooks.Remove: {why}, removed {had} boulder(s)");
    }

    /// <summary>The centre for a boulder of half depth `half` standing right behind `last` (along `forward`).</summary>
    private static Vector3 Behind(Bounds last, Vector3 forward, float half)
    {
        float lastHalf = Mathf.Abs(Vector3.Dot(last.extents, new Vector3(Mathf.Abs(forward.x), 0f, Mathf.Abs(forward.z))));
        Vector3 centre = new Vector3(last.center.x, last.min.y, last.center.z) + (forward * (lastHalf + half));
        ModLog.Verbose($"BoulderTestHooks.Behind: last boulder half depth {lastHalf:0.0} m -> centre {centre}");
        return centre;
    }

    /// <summary>Instantiates the first rock of <see cref="Rocks"/> whose collider box is within 30 % of its visible mesh box. Expected outcome: its prefab name.</summary>
    private static string PickRock(Vector3 at, Vector3 forward)
    {
        foreach (string rock in Rocks)
        {
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(rock) : null;
            if (source == null) continue;
            _rock = Object.Instantiate(source, at, Quaternion.LookRotation(forward));
            Physics.SyncTransforms();
            Vector3 seen = Visible(_rock).size, solid = BoundsOf(_rock).size;
            bool fits = seen.y > 0.1f && Close(seen.x, solid.x) && Close(seen.y, solid.y) && Close(seen.z, solid.z);
            ModLog.Info($"BoulderTestHooks.PickRock: {rock} visible {seen}, colliders {solid} -> {(fits ? "used" : "skipped")}");
            if (fits)
            {
                return rock;
            }

            Object.Destroy(_rock);
            _rock = null;
        }

        ModLog.Warn("BoulderTestHooks.PickRock: none fits");
        return "<none>";
    }

    private static bool Close(float a, float b) => Mathf.Abs(a - b) <= 0.3f * Mathf.Max(a, b);

    private static Bounds Visible(GameObject go)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        ModLog.Verbose($"BoulderTestHooks.Visible: {go.name} -> {bounds.size} at {bounds.center}");
        return bounds;
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
