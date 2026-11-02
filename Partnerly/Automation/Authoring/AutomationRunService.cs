using System.Text.Json;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Authoring
{
    public class AutomationRunService : IAutomationRunService
    {
        private readonly IAutomationStore _store;
        private readonly IAutomationClock _clock;
        private readonly WorkflowExecutor _executor;
        private readonly IServiceProvider _services;

        public AutomationRunService(
            IAutomationStore store,
            IAutomationClock clock,
            WorkflowExecutor executor,
            IServiceProvider services)
        {
            _store = store;
            _clock = clock;
            _executor = executor;
            _services = services;
        }

        public async Task<AutomationRun> EnqueueAsync(
            AutomationDefinition definition,
            AutomationVersion version,
            string triggerType,
            string reason,
            bool dryRun,
            TriggerContext? trigger,
            CancellationToken cancellationToken = default)
        {
            var run = new AutomationRun
            {
                Id = Guid.NewGuid(),
                AutomationId = definition.Id,
                VersionId = version.Id,
                OwnerUserId = definition.OwnerUserId,
                TriggerType = triggerType,
                TriggerReason = reason,
                Status = AutomationStatuses.Queued,
                IsDryRun = dryRun,
                Depth = trigger == null ? 0 : Math.Max(0, RecursionGuard.Current),
                CreatedDate = _clock.UtcNow
            };
            run.ContextJson = ExecutionContextFactory.Create(run, trigger, _clock.UtcNow).ToJsonString();
            await _store.AddRunAsync(run, cancellationToken);
            return run;
        }

        public async Task AdvanceAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            var run = await _store.GetRunAsync(runId, cancellationToken)
                      ?? throw new InvalidOperationException("Run was not found.");
            if (run.Status is AutomationStatuses.Cancelled
                or AutomationStatuses.Succeeded
                or AutomationStatuses.Failed)
            {
                return;
            }

            var definition = await _store.GetDefinitionAsync(run.AutomationId, cancellationToken)
                             ?? throw new InvalidOperationException("Automation was not found.");
            var version = await _store.GetVersionAsync(run.VersionId, cancellationToken)
                          ?? throw new InvalidOperationException("Version was not found.");
            var graph = WorkflowGraph.Parse(version.GraphJson);

            if (run.Status == AutomationStatuses.Waiting)
            {
                await CompleteWaitingNodeAsync(run, cancellationToken);
            }

            await _executor.AdvanceUntilBlockOrEndAsync(run, definition, graph, _services, cancellationToken);
        }

        public async Task CancelAsync(Guid runId, Guid cancelledBy, CancellationToken cancellationToken = default)
        {
            var run = await _store.GetRunAsync(runId, cancellationToken)
                      ?? throw new InvalidOperationException("Run was not found.");
            run.CancelledBy = cancelledBy;
            if (run.Status is AutomationStatuses.Queued or AutomationStatuses.Waiting or AutomationStatuses.Running)
            {
                run.Status = AutomationStatuses.Cancelled;
                run.EndedAt = _clock.UtcNow;
            }

            await _store.UpdateRunAsync(run, cancellationToken);
            await _store.CancelPendingWakeupsForRunAsync(run.Id, cancellationToken);
        }

        private async Task CompleteWaitingNodeAsync(AutomationRun run, CancellationToken cancellationToken)
        {
            var steps = await _store.GetStepsAsync(run.Id, cancellationToken);
            var waiting = steps
                .Where(step => step.Status == AutomationStatuses.StepWaiting)
                .OrderByDescending(step => step.StartedAt)
                .FirstOrDefault();
            var completed = JsonSerializer.Deserialize<List<string>>(
                                string.IsNullOrWhiteSpace(run.CompletedNodeIdsJson) ? "[]" : run.CompletedNodeIdsJson)
                            ?? new List<string>();
            if (waiting != null
                && waiting.ActionType != AutomationTypeCodes.WaitUntil
                && !completed.Contains(waiting.NodeId))
            {
                completed.Add(waiting.NodeId);
            }

            run.CompletedNodeIdsJson = JsonSerializer.Serialize(completed);
            run.Status = AutomationStatuses.Running;
        }
    }
}
