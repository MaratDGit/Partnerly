using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Actions
{
    public class ExecuteWorkflowActionHandler : IActionHandler
    {
        public string Type => AutomationTypeCodes.ExecuteWorkflow;
        public bool HasSideEffects => true;

        public HandlerDescriptor Describe() => new()
        {
            Type = Type,
            Kind = "action",
            DisplayName = "Execute workflow",
            HasSideEffects = true
        };

        public async Task<StepResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
        {
            if (context.Depth >= RecursionGuard.MaxDepth || !RecursionGuard.CanEnter)
            {
                return StepResult.Failed("Workflow depth limit reached.", retryable: false);
            }

            if (context.IsDryRun)
            {
                return StepResult.Skipped("dryRun");
            }

            var raw = context.Services.GetService(typeof(IVariableBinder)) is IVariableBinder binder
                ? binder.Interpolate(NodeConfig.GetString(context.Node, "automationId"), context.Variables)
                : NodeConfig.GetString(context.Node, "automationId");
            if (!Guid.TryParse(raw, out var childId))
            {
                return StepResult.Failed("Child automation id is required.", retryable: false);
            }

            var store = context.Services.GetService(typeof(IAutomationStore)) as IAutomationStore;
            var runs = context.Services.GetService(typeof(IAutomationRunService)) as IAutomationRunService;
            var clock = context.Services.GetService(typeof(IAutomationClock)) as IAutomationClock;
            if (store == null || runs == null || clock == null)
            {
                return StepResult.Failed("Automation services are not available.", retryable: false);
            }

            var child = await store.GetDefinitionAsync(childId, cancellationToken);
            if (child == null
                || child.IsDeleted
                || child.OwnerUserId != context.Run.OwnerUserId)
            {
                return StepResult.Failed("Automation was not found.", retryable: false);
            }

            if (!child.IsActive || child.PublishedVersionId == null)
            {
                return StepResult.Failed("Publish the child automation before execute_workflow.", retryable: false);
            }

            var version = await store.GetVersionAsync(child.PublishedVersionId.Value, cancellationToken);
            if (version == null)
            {
                return StepResult.Failed("Published version was not found.", retryable: false);
            }

            var payload = new JsonObject
            {
                ["parentRunId"] = context.Run.Id.ToString(),
                ["parentAutomationId"] = context.Run.AutomationId.ToString()
            };
            if (context.Variables["vars"] is JsonNode vars)
            {
                payload["vars"] = JsonNodes.Clone(vars);
            }

            var childRun = await runs.EnqueueAsync(
                child,
                version,
                AutomationTypeCodes.ExecuteWorkflow,
                "execute_workflow",
                dryRun: false,
                new TriggerContext
                {
                    Type = AutomationTypeCodes.ExecuteWorkflow,
                    OwnerUserId = child.OwnerUserId,
                    Payload = payload
                },
                cancellationToken);
            childRun.Depth = context.Depth + 1;
            await store.UpdateRunAsync(childRun, cancellationToken);
            await store.AddWakeupAsync(new AutomationWakeup
            {
                Id = Guid.NewGuid(),
                Kind = AutomationStatuses.WakeupStart,
                DueAtUtc = clock.UtcNow,
                AutomationId = child.Id,
                VersionId = version.Id,
                RunId = childRun.Id,
                Status = AutomationStatuses.WakeupPending
            }, cancellationToken);

            return StepResult.Succeeded(new JsonObject
            {
                ["runId"] = childRun.Id.ToString(),
                ["automationId"] = child.Id.ToString()
            });
        }
    }
}
