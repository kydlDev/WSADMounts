using WSADMounts.Core;

namespace WSADMounts.Features.Riding.Domain;

/// <summary>
/// The movement values a ridden mount gets (D-037, user 2026-11-25: faster sprint with less stamina drain, faster acceleration and turning,
/// all adjustable; D-039: a stronger jump, adjustable): the prefab's own values times the factors. The sprint factors apply only while Enable Mount Sprint is on (off: the mount
/// cannot run, D-037). Pure (unit-tested in tests/Unit/RideTuningRulesTests.cs).
/// </summary>
internal static class RideTuningRules
{
    /// <summary>Expected outcome: every value of <paramref name="own"/> times its factor; a factor below 0 counts as 0.</summary>
    internal static RideTuning Tune(RideTuning own, RideFactors factors)
    {
        float sprint = factors.SprintOn ? NotNegative(factors.SprintSpeed) : 1f;
        float stamina = factors.SprintOn ? NotNegative(factors.SprintStamina) : 1f;
        float turn = NotNegative(factors.Turn);
        var result = new RideTuning(
            own.Acceleration * NotNegative(factors.Acceleration),
            own.TurnSpeed * turn,
            own.RunTurnSpeed * turn,
            own.RunSpeed * sprint,
            own.RunStaminaDrain * stamina,
            own.JumpForce * NotNegative(factors.Jump));

        // Stryker disable once all : only decides whether the debug line is written
        if (RuleLog.IsOn()) RuleLog.Verbose($"RideTuningRules.Tune: own {RuleLog.Shown(own)}, {RuleLog.Shown(factors)} -> {RuleLog.Shown(result)}");
        return result;
    }

    private static float NotNegative(float factor) => factor < 0f ? 0f : factor;
}
