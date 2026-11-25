using KydlCore.Core;
using WSADMounts.Features.Example.Domain;
using WSADMounts.Features.Example.Infrastructure;
using WSADMounts.Presentation.Hub.Commands;
using WSADMounts.Ui;
using WSADMounts.Core;
using KydlCore.Common;

namespace WSADMounts.Presentation.Hub.ViewModels
{
    /// <summary>
    /// WHAT the example tab shows (MIGRATION_DECISIONS A6): built from the feature's state every time the tab opens. The data contract
    /// (<see cref="ExamplePage"/>) lives in the Unity project's Runtime folder so the Unity preview can fill it too; HOW it is drawn is
    /// ExampleTabView there. A UI change goes through the command dispatcher, never straight into the feature.
    /// </summary>
    internal static class ExamplePageModel
    {
        internal static ExamplePage Build(ExampleFeature feature, CommandDispatcher commands)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ExamplePageModel.Build(feature={RuleLog.Shown(feature)}, commands={RuleLog.Shown(commands)})");
            return new ExamplePage(feature.Value, ExampleRules.Min, ExampleRules.Max, v => ExampleCommands.SetValue(commands, v));
        }
    }
}
