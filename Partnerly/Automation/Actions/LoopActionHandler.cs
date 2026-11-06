using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Actions
{
    // "For each". The run loop (WorkflowExecutor) drives the repetition; this handler only checks the list,
    // so a single card can also be tested on its own.
    public class LoopActionHandler : IActionHandler
    {
        private readonly IVariableBinder _binder;

        public LoopActionHandler(IVariableBinder binder)
        {
            _binder = binder;
        }

        public string Type => AutomationTypeCodes.Loop;
        public bool HasSideEffects => false;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "For each"
        };

        public Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (!LoopItems.TryRead(context.Node, _binder, context.Variables, out var items, out var error))
            {
                return Task.FromResult(StepResult.Failed(error, retryable: false));
            }

            return Task.FromResult(StepResult.Succeeded(new JsonObject { ["count"] = items.Count, ["items"] = items }));
        }
    }
}
