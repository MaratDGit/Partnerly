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

        public async Task<AutomationRun> RerunAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            var old = await _store.GetRunAsync(runId, cancellationToken)
                      ?? throw new InvalidOperationException("Run was not found.");
            var definition = await _store.GetDefinitionAsync(old.AutomationId, cancellationToken)
                             ?? throw new InvalidOperationException("Automation was not found.");
            var version = await _store.GetVersionAsync(old.VersionId, cancellationToken)
                          ?? throw new InvalidOperationException("Version was not found.");
            var payload = new System.Text.Json.Nodes.JsonObject();
            if (System.Text.Json.Nodes.JsonNode.Parse(string.IsNullOrWhiteSpace(old.ContextJson) ? "{}" : old.ContextJson) is System.Text.Json.Nodes.JsonObject oldContext
                && oldContext["trigger"] is System.Text.Json.Nodes.JsonObject oldTrigger)
            {
                payload = (System.Text.Json.Nodes.JsonObject)JsonNodes.Clone(oldTrigger)!;
            }

            var run = await EnqueueAsync(
                definition,
                version,
                old.TriggerType,
                "rerun",
                old.IsDryRun,
                new TriggerContext { Type = old.TriggerType, OwnerUserId = definition.OwnerUserId, Payload = payload },
                cancellationToken);
            await _store.AddWakeupAsync(new AutomationWakeup
            {
                Id = Guid.NewGuid(),
                Kind = AutomationStatuses.WakeupStart,
                DueAtUtc = _clock.UtcNow,
                AutomationId = definition.Id,
                VersionId = version.Id,
                RunId = run.Id,
                Status = AutomationStatuses.WakeupPending
            }, cancellationToken);
            return run;
        }

        public async Task<bool> ResumeFailedAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            var run = await _store.GetRunAsync(runId, cancellationToken)
                      ?? throw new InvalidOperationException("Run was not found.");
            if (run.Status != AutomationStatuses.Failed)
            {
                return false;
            }

            var ports = JsonSerializer.Deserialize<Dictionary<string, string>>(
                            string.IsNullOrWhiteSpace(run.ChosenPortsJson) ? "{}" : run.ChosenPortsJson)
                        ?? new Dictionary<string, string>();
            foreach (var key in ports.Keys.Where(key => key.StartsWith("failed:", StringComparison.Ordinal)).ToList())
            {
                ports.Remove(key);
            }

            run.ChosenPortsJson = JsonSerializer.Serialize(ports);
            if (System.Text.Json.Nodes.JsonNode.Parse(string.IsNullOrWhiteSpace(run.ContextJson) ? "{}" : run.ContextJson) is System.Text.Json.Nodes.JsonObject context)
            {
                // a "For each" that stopped on an item goes on with that very item
                if (context["_loops"] is System.Text.Json.Nodes.JsonObject loops)
                {
                    foreach (var pair in loops)
                    {
                        if (pair.Value is System.Text.Json.Nodes.JsonObject state)
                        {
                            state.Remove("failed");
                        }
                    }
                }

                run.ContextJson = context.ToJsonString();
            }

            run.Status = AutomationStatuses.Queued;
            run.Error = null;
            run.EndedAt = null;
            run.CancelledBy = null;
            run.RetryCount = 0;
            run.StartedAt = _clock.UtcNow; // the run timeout counts from the resume
            await _store.UpdateRunAsync(run, cancellationToken);
            await _store.AddWakeupAsync(new AutomationWakeup
            {
                Id = Guid.NewGuid(),
                Kind = AutomationStatuses.WakeupStart,
                DueAtUtc = _clock.UtcNow,
                AutomationId = run.AutomationId,
                VersionId = run.VersionId,
                RunId = run.Id,
                Status = AutomationStatuses.WakeupPending
            }, cancellationToken);
            return true;
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
                && waiting.ActionType != AutomationTypeCodes.ExecuteWorkflow
                && !completed.Contains(waiting.NodeId))
            {
                completed.Add(waiting.NodeId);
            }

            run.CompletedNodeIdsJson = JsonSerializer.Serialize(completed);
            run.Status = AutomationStatuses.Running;
        }
    }
}
