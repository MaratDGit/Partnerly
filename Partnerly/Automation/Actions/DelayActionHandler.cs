using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    public class DelayActionHandler : IActionHandler
    {
        private readonly IVariableBinder _binder;

        public DelayActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.Delay;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Delay"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (!DelayDuration.TryRead(context.Node, _binder, context.Variables, out var delay))
            {
                return Task.FromResult(StepResult.Failed("Delay value is invalid."));
            }

            if (delay <= TimeSpan.Zero)
            {
                return Task.FromResult(StepResult.Succeeded());
            }

            return Task.FromResult(StepResult.Waiting(
                context.UtcNow.Add(delay),
                AutomationStatuses.WakeupDelay));
        }
    }
}
