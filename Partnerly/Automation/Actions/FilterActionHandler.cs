using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    // "Only continue if": when the condition is false this branch of the flow simply ends
    // (the run still succeeds), unlike a Condition card that has a "false" path.
    public class FilterActionHandler : IActionHandler
    {
        public const string StoppedPort = "stopped";

        private readonly IConditionEvaluator _evaluator;
        private readonly IVariableBinder _binder;

        public FilterActionHandler(IConditionEvaluator evaluator, IVariableBinder binder)
        {
            _evaluator = evaluator;
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.Filter;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Only continue if"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var condition = NodeConfig.Get<FilterConfig>(context.Node)?.Condition
                ?? NodeConfig.Get<ConditionNode>(context.Node);
            var passed = _evaluator.Evaluate(condition, context.Variables, _binder);
            return Task.FromResult(StepResult.Succeeded(
                new System.Text.Json.Nodes.JsonObject { ["passed"] = passed },
                port: passed ? null : StoppedPort));
        }

        private sealed class FilterConfig
        {
            public ConditionNode? Condition { get; set; }
        }
    }
}
