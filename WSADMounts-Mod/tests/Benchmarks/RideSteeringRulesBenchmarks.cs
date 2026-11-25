using BenchmarkDotNet.Attributes;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Benchmarks;

/// <summary>
/// The steering rule runs every control tick of the rider (PROJECT_RULES 30: hot path). One case per branch family; inputs live in fields so the
/// JIT cannot fold them; every method returns a value of its result (RideCommand is internal, a public benchmark cannot return it). Comparison axis: builds (before / after a change of RideSteeringRules).
/// </summary>
[MemoryDiagnoser]
public class RideSteeringRulesBenchmarks
{
    private RideInput _forward;
    private RideInput _back;
    private RideInput _idle;

    [GlobalSetup]
    public void Setup()
    {
        _forward = new RideInput(0.7f, 0.7f, 0.6f, 0.8f, false, false, false, false);
        _back = new RideInput(0f, -1f, 0.6f, 0.8f, false, false, false, false);
        _idle = new RideInput(0f, 0f, 0.6f, 0.8f, false, false, false, false);
    }

    [Benchmark(Baseline = true)]
    public float Forward() => RideSteeringRules.Decide(_forward).MoveX;

    [Benchmark]
    public float BackStep() => RideSteeringRules.Decide(_back).MoveX;

    [Benchmark]
    public float NoInput() => RideSteeringRules.Decide(_idle).MoveX;
}
