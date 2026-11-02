using System.Globalization;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class WaitUntilActionHandler : IActionHandler
    {
        private readonly IConditionEvaluator _evaluator;
        private readonly IVariableBinder _binder;

        public WaitUntilActionHandler(IConditionEvaluator evaluator, IVariableBinder binder)
        {
            _evaluator = evaluator;
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.WaitUntil;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Wait until"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            var config = NodeConfig.Get<WaitConfig>(context.Node);
            var condition = config?.Condition ?? NodeConfig.Get<ConditionNode>(context.Node);
            if (_evaluator.Evaluate(condition, context.Variables, _binder))
            {
                return Task.FromResult(StepResult.Succeeded());
            }

            var untilRaw = _binder.Interpolate(
                config?.Until ?? NodeConfig.GetString(context.Node, "until") ?? string.Empty,
                context.Variables);
            DateTime? deadline = null;
            if (DateTime.TryParse(untilRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                deadline = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            if (deadline != null && context.UtcNow >= deadline.Value)
            {
                return Task.FromResult(StepResult.Failed("Wait until timed out.", retryable: false));
            }

            var resume = context.UtcNow.AddMinutes(1);
            if (deadline != null && deadline.Value < resume)
            {
                resume = deadline.Value;
            }

            return Task.FromResult(StepResult.Waiting(resume, AutomationStatuses.WakeupWait));
        }

        private sealed class WaitConfig
        {
            public ConditionNode? Condition { get; set; }
            public string? Until { get; set; }
        }
    }
}
