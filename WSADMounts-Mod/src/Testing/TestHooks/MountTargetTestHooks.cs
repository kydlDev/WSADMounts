using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TestLab hooks for "the mount's attack really hits" (T-010, D-037): a target creature right in front of the ridden test mount (its AI switched
/// off so it stands still), whether it lost health, and whether the rider stayed unhurt (the Lox Stomp is an area attack). TEST ONLY.
/// </summary>
internal static class MountTargetTestHooks
{
    private static Character _target;
    private static float _targetStart;
    private static float _riderStart;

    /// <summary>Spawns `prefab` (default Boar) `gap` metres in front of the test mount's body, AI off. Returns "target Boar health 10 at 3.2 m".</summary>
    [TestHook("WSADMounts.targetSpawn")]
    private static string TargetSpawn(string prefab = "Boar", double gap = 1.2d)
    {
        Character mount = RidingTestHooks.TestMount != null ? RidingTestHooks.TestMount.GetComponent<Character>() : null;
        GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
        if (mount == null || source == null)
        {
            ModLog.Warn($"MountTargetTestHooks.TargetSpawn: mount {mount != null}, prefab '{prefab}' {source != null}");
            return "no mount or no prefab";
        }

        Vector3 forward = mount.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        float ahead = mount.GetRadius() + (float)gap;
        Vector3 at = mount.transform.position + (forward * ahead);
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float h))
        {
            at.y = h + 0.1f;
        }

        GameObject go = Object.Instantiate(source, at, Quaternion.LookRotation(-forward));
        _target = go.GetComponent<Character>();
        BaseAI ai = go.GetComponent<BaseAI>();
        if (ai != null)
        {
            ai.enabled = false;   // stands still, does not flee or fight back
        }

        _targetStart = _target != null ? _target.GetHealth() : -1f;
        string answer = $"target {prefab} health {_targetStart:0} at {ahead:0.0} m (mount radius {mount.GetRadius():0.0} m), AI off {ai != null}";
        ModLog.Info($"MountTargetTestHooks.TargetSpawn: {answer}");
        return answer;
    }

    /// <summary>The target lost health (or died) since targetSpawn. Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.targetHit")]
    private static string TargetHit()
    {
        bool gone = _target == null || _target.IsDead();
        float now = gone ? 0f : _target.GetHealth();
        bool hit = gone || now < _targetStart - 0.01f;
        string answer = hit ? "ok" : $"bad: target health {now:0.#} of {_targetStart:0.#}, not hit";
        ModLog.Info($"MountTargetTestHooks.TargetHit: health {_targetStart:0.#} -> {now:0.#} (dead or removed {gone}) -> {(hit ? "ok" : "bad")}");
        return answer;
    }

    /// <summary>Remembers the rider's health. Returns "rider at N".</summary>
    [TestHook("WSADMounts.riderMark")]
    private static string RiderMark()
    {
        Player p = Player.m_localPlayer;
        _riderStart = p != null ? p.GetHealth() : -1f;
        ModLog.Info($"MountTargetTestHooks.RiderMark: rider health {_riderStart:0.#}");
        return $"rider at {_riderStart:0}";
    }

    /// <summary>The rider lost no health since riderMark (the mount's own attacks must not hurt its rider). Returns "ok" or "bad: ...".</summary>
    [TestHook("WSADMounts.riderUnhurt")]
    private static string RiderUnhurt()
    {
        Player p = Player.m_localPlayer;
        float now = p != null ? p.GetHealth() : -1f;
        bool unhurt = p != null && now >= _riderStart - 0.01f;
        string answer = unhurt ? "ok" : $"bad: rider health {now:0.#} of {_riderStart:0.#}";
        ModLog.Info($"MountTargetTestHooks.RiderUnhurt: {_riderStart:0.#} -> {now:0.#} -> {(unhurt ? "ok" : "bad")}");
        return answer;
    }
}
