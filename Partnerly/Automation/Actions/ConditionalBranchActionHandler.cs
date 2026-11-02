using System.Text.Json;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class ConditionalBranchActionHandler : IActionHandler
    {
        private readonly IConditionEvaluator _evaluator;
        private readonly IVariableBinder _binder;

        public ConditionalBranchActionHandler(IConditionEvaluator evaluator, IVariableBinder binder)
        {
            _evaluator = evaluator;
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.Branch;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Condition"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var condition = NodeConfig.Get<BranchConfig>(context.Node)?.Condition
                ?? NodeConfig.Get<ConditionNode>(context.Node);
            var passed = _evaluator.Evaluate(condition, context.Variables, _binder);
            return Task.FromResult(StepResult.Succeeded(port: passed ? "true" : "false"));
        }

        private sealed class BranchConfig
        {
            public ConditionNode? Condition { get; set; }
        }
    }
}
