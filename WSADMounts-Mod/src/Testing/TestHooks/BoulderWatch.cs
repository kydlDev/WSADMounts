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
    private Bounds _rock;
    private Character _mount;
    private float _until;
    private float _peak;
    private float _start;

    internal bool LandedOnTop { get; private set; }

    internal void Begin(Bounds rock)
    {
        _rock = rock;
        _mount = GetComponent<Character>();
        _until = Time.time + MaxSeconds;
        _start = transform.position.y;
        _peak = _start;
        LandedOnTop = false;
        ModLog.Verbose($"BoulderWatch.Begin: {name} at y {_start:0.00} m, boulder top {rock.max.y:0.00} m");
    }

    internal string Facts() =>
        $"landed on top {LandedOnTop}, peak {_peak - _start:0.00} m above the start, boulder top {_rock.max.y - _start:0.00} m, now y {transform.position.y - _start:0.00} m";

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
        bool over = p.x > _rock.min.x && p.x < _rock.max.x && p.z > _rock.min.z && p.z < _rock.max.z;
        if (!LandedOnTop && over && _mount.IsOnGround() && p.y >= _rock.max.y - 0.35f)
        {
            LandedOnTop = true;
            ModLog.Info($"BoulderWatch: {name} landed on the boulder at y {p.y - _start:0.00} m above the start");
        }

        ModLog.Every("BoulderWatch.Update", 1d, $"BoulderWatch: {name} y {p.y - _start:0.00} m, over the boulder {over}, on ground {_mount.IsOnGround()}");
    }
}
