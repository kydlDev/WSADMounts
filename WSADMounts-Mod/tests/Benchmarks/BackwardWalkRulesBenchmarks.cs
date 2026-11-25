using BenchmarkDotNet.Attributes;
using WSADMounts.Features.Riding.Domain;

namespace WSADMounts.Benchmarks;

/// <summary>
/// BackwardWalkRules.NextPhase runs every frame on every mount that steps back (BackwardWalkPlayer.Update, PROJECT_RULES 30: hot path);
/// PickLook runs once per back step (D-035: three styles). Inputs live in fields so the JIT cannot fold them; every method returns its result.
/// Comparison axis: builds (before / after a change of BackwardWalkRules).
/// </summary>
[MemoryDiagnoser]
public class BackwardWalkRulesBenchmarks
{
    private float _phase;
    private float _delta;
    private float _length;
    private float _reversedRate;
    private float _handMadeRate;
    private BackwardAnimation _style;
    private bool _clipLoaded;

    [GlobalSetup]
    public void Setup()
    {
        _phase = 0.05f;
        _delta = 0.0167f;
        _length = 2.03f;
        _reversedRate = 0.5f;
        _handMadeRate = -1f;
        _style = BackwardAnimation.BackwardWalk;
        _clipLoaded = false;
    }

    [Benchmark(Baseline = true)]
    public float NextPhaseReversed() => BackwardWalkRules.NextPhase(_phase, _delta, _length, _reversedRate);

    [Benchmark]
    public float NextPhaseHandMade() => BackwardWalkRules.NextPhase(_phase, _delta, _length, _handMadeRate);

    [Benchmark]
    public int PickLookFallback() => (int)BackwardWalkRules.PickLook(_style, _clipLoaded);
}
