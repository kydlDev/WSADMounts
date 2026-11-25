using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TEST ONLY: keeps the highest point a mount reaches after WSADMounts.airMark (a jump lasts well under a second; the lab's next hook call
/// would miss the top). Removes itself after <see cref="MaxSeconds"/>.
/// </summary>
internal sealed class AirSampler : MonoBehaviour
{
    private const float MaxSeconds = 20f;
    private float _until;

    internal float StartY { get; private set; }

    internal float PeakY { get; private set; }

    internal void Begin()
    {
        StartY = transform.position.y;
        PeakY = StartY;
        _until = Time.time + MaxSeconds;
        ModLog.Verbose($"AirSampler.Begin: {name} at y {StartY:0.00} m for {MaxSeconds:0} s");
    }

    private void Update()
    {
        if (Time.time > _until)
        {
            ModLog.Verbose($"AirSampler: {name} done (peak {PeakY - StartY:0.00} m above the start)");
            Destroy(this);
            return;
        }

        float y = transform.position.y;
        if (y > PeakY)
        {
            PeakY = y;
        }

        ModLog.Every("AirSampler.Update", 2d, $"AirSampler: {name} y {y:0.00} m, peak {PeakY - StartY:0.00} m above the start");
    }
}
