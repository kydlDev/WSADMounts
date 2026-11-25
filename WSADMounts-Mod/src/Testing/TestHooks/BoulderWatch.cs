using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TEST ONLY: on the ridden test mount, remembers whether it stood on the boulder's top (on the ground, its centre over the boulder, its feet at
/// the top's height) and how high it got. A landing lasts a few frames; the lab's next hook call would miss it. Removes itself after 30 s.
/// </summary>
internal sealed class BoulderWatch : MonoBehaviour
{
    private const float MaxSeconds = 30f;
    private readonly List<Bounds> _rocks = new List<Bounds>();
    private readonly List<bool> _landed = new List<bool>();
    private Character _mount;
    private float _until;
    private float _peak;
    private float _start;

    /// <summary>Whether the mount stood on top of boulder `index` (0 = the first one spawned).</summary>
    internal bool LandedOn(int index) => index >= 0 && index < _landed.Count && _landed[index];

    internal void Begin(List<Bounds> rocks)
    {
        _rocks.Clear();
        _rocks.AddRange(rocks);
        _landed.Clear();
        rocks.ForEach(_ => _landed.Add(false));
        _mount = GetComponent<Character>();
        _until = Time.time + MaxSeconds;
        _start = transform.position.y;
        _peak = _start;
        ModLog.Verbose($"BoulderWatch.Begin: {name} at y {_start:0.00} m, {rocks.Count} boulder(s), tops {string.Join(", ", rocks.ConvertAll(r => $"{r.max.y - _start:0.00} m").ToArray())}");
    }

    internal string Facts()
    {
        var parts = new List<string>();
        for (int i = 0; i < _rocks.Count; i++)
        {
            parts.Add($"boulder {i + 1} (top {_rocks[i].max.y - _start:0.00} m) landed {_landed[i]}");
        }

        string facts = $"{string.Join(", ", parts.ToArray())}; peak {_peak - _start:0.00} m above the start, now y {transform.position.y - _start:0.00} m";
        ModLog.Verbose($"BoulderWatch.Facts: {facts}");
        return facts;
    }

    private void Update()
    {
        if (Time.time > _until || _mount == null)
        {
            ModLog.Verbose($"BoulderWatch: {name} done - {Facts()}");
            Destroy(this);
            return;
        }

        Vector3 p = transform.position;
        _peak = Mathf.Max(_peak, p.y);
        bool grounded = _mount.IsOnGround();
        for (int i = 0; i < _rocks.Count; i++)
        {
            Bounds r = _rocks[i];
            bool over = p.x > r.min.x && p.x < r.max.x && p.z > r.min.z && p.z < r.max.z;
            if (!_landed[i] && over && grounded && p.y >= r.max.y - 0.35f)
            {
                _landed[i] = true;
                ModLog.Info($"BoulderWatch: {name} landed on boulder {i + 1} at y {p.y - _start:0.00} m above the start");
            }
        }

        ModLog.Every("BoulderWatch.Update", 1d, $"BoulderWatch: {name} y {p.y - _start:0.00} m, on ground {grounded}");
    }
}
