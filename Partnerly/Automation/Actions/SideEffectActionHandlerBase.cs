using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Actions
{
    public abstract class SideEffectActionHandlerBase : IActionHandler
    {
        public abstract string Type { get; }
        public bool HasSideEffects => true;
        public abstract string DisplayName { get; }

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = DisplayName,
            HasSideEffects = true
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (context.IsDryRun)
            {
                return Task.FromResult(StepResult.Skipped("dryRun"));
            }

            return ExecuteCoreAsync(context, cancellationToken);
        }

        protected virtual Task<StepResult> ExecuteCoreAsync(ActionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(StepResult.Failed($"{Type} needs Partnerly connectors in PHASE 5."));
        }
    }
}
