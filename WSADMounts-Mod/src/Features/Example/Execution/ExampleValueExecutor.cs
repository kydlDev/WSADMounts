using System;
using KydlCore.Core;
using WSADMounts.Core;

namespace WSADMounts.Features.Example.Execution
{
    /// <summary>
    /// EXAMPLE executor (Architecture V2): checks ownership FIRST, then runs the side effect through a delegate bound ONCE in the constructor
    /// (no closure per call). Returns what was issued. Game-free; the delegate is a method of the Infrastructure adapter.
    /// </summary>
    internal sealed class ExampleValueExecutor
    {
        internal const string NotOwner = "not the owner", Applied = "applied";
        readonly IOwnership _owner;
        readonly Action<float> _apply;

        internal ExampleValueExecutor(IOwnership owner, Action<float> apply)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"ExampleValueExecutor.ExampleValueExecutor(owner={RuleLog.Shown(owner)}, apply={RuleLog.Shown(apply)})");
            if (owner == null) throw new ArgumentNullException("owner");
            if (apply == null) throw new ArgumentNullException("apply");
            _owner = owner;
            _apply = apply;
        }

        /// Expected outcome: Success with the applied value; Rejected (nothing applied) when this client is not the owner.
        internal OperationResponse<float> Apply(float value)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"ExampleValueExecutor.Apply(value={RuleLog.Shown(value)})");
            if (!_owner.IsOwner) { var returnedAt30 = OperationResponse<float>.Rejected(NotOwner); if (RuleLog.IsOn()) RuleLog.Verbose($"ExampleValueExecutor.Apply: returns {RuleLog.Shown(returnedAt30)} (line 30)"); return returnedAt30; }
            _apply(value);
            { var returnedAt32 = OperationResponse<float>.Success(value, Applied); if (RuleLog.IsOn()) RuleLog.Verbose($"ExampleValueExecutor.Apply: returns {RuleLog.Shown(returnedAt32)} (line 32)"); return returnedAt32; }
        }
    }
}
