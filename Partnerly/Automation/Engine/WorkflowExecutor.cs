using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Engine
{
    public class WorkflowExecutor
    {
        private readonly StepRunner _stepRunner;
        private readonly IVariableBinder _binder;
        private readonly IAutomationStore _store;
        private readonly IAutomationClock _clock;
        private readonly IAutomationRunNotifier _notifier;

        public WorkflowExecutor(
            StepRunner stepRunner,
            IVariableBinder binder,
            IAutomationStore store,
            IAutomationClock clock,
            IAutomationRunNotifier notifier)
        {
            _stepRunner = stepRunner;
            _binder = binder;
            _store = store;
            _clock = clock;
            _notifier = notifier;
        }

        public async Task AdvanceUntilBlockOrEndAsync(
            AutomationRun run,
            AutomationDefinition definition,
            WorkflowGraph graph,
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            using var depth = RecursionGuard.Enter();
            var variables = ParseObject(run.ContextJson);
            var completed = ParseIds(run.CompletedNodeIdsJson);
            var ports = ParsePorts(run.ChosenPortsJson);
            var steps = (await _store.GetStepsAsync(run.Id, cancellationToken)).ToList();

            foreach (var trigger in graph.Nodes.Where(node => node.Kind == "trigger"))
            {
                completed.Add(trigger.Id);
            }

            var startedNow = run.StartedAt == null;
            run.Status = AutomationStatuses.Running;
            run.StartedAt ??= _clock.UtcNow;
            run.HeartbeatAt = _clock.UtcNow;
            if (startedNow)
            {
                await _notifier.RunStartedAsync(run, cancellationToken);
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                run.HeartbeatAt = _clock.UtcNow;
                if (run.CancelledBy != null)
                {
                    run.Status = AutomationStatuses.Cancelled;
                    run.EndedAt = _clock.UtcNow;
                    break;
                }

                if (run.StartedAt != null
                    && definition.RunTimeoutSeconds > 0
                    && _clock.UtcNow - run.StartedAt.Value > TimeSpan.FromSeconds(definition.RunTimeoutSeconds))
                {
                    run.Status = AutomationStatuses.Failed;
                    run.Error = "Run timed out.";
                    run.EndedAt = _clock.UtcNow;
                    break;
                }

                var ready = graph.Nodes
                    .Where(node => node.Kind == "action" && IsReady(graph, node, completed, ports))
                    .OrderBy(node => node.Id, StringComparer.Ordinal)
                    .ToList();

                if (ready.Count == 0)
                {
                    run.Status = AutomationStatuses.Succeeded;
                    run.EndedAt = _clock.UtcNow;
                    break;
                }

                var node = ready[0];
                var context = new ActionContext
                {
                    Run = run,
                    Node = node,
                    Variables = variables,
                    IsDryRun = run.IsDryRun,
                    Depth = run.Depth,
                    UtcNow = _clock.UtcNow,
                    Services = services
                };

                await _notifier.StepAsync(run, node.Id, "started", cancellationToken);
                var result = await _stepRunner.RunAsync(context, steps, definition.RetryLimit, cancellationToken);
                if (result.Status != AutomationStatuses.StepWaiting)
                {
                    await _notifier.StepAsync(run, node.Id, result.Status, cancellationToken);
                }

                steps = (await _store.GetStepsAsync(run.Id, cancellationToken)).ToList();

                if (result.Status == AutomationStatuses.StepFailed)
                {
                    if (result.WakeupKind == AutomationStatuses.WakeupRetry)
                    {
                        await EnqueueWakeupAsync(run, node.Id, result, cancellationToken);
                        run.Status = AutomationStatuses.Waiting;
                        run.RetryCount++;
                    }
                    else
                    {
                        run.Status = AutomationStatuses.Failed;
                        run.Error = result.Error;
                        run.EndedAt = _clock.UtcNow;
                    }

                    break;
                }

                WriteStepOutput(variables, node, result);
                if (!string.IsNullOrEmpty(result.Port))
                {
                    ports[node.Id] = result.Port;
                }

                if (result.Status == AutomationStatuses.StepWaiting)
                {
                    await EnqueueWakeupAsync(run, node.Id, result, cancellationToken);
                    run.Status = AutomationStatuses.Waiting;
                    break;
                }

                completed.Add(node.Id);
            }

            run.ContextJson = variables.ToJsonString();
            run.CompletedNodeIdsJson = JsonSerializer.Serialize(completed);
            run.ChosenPortsJson = JsonSerializer.Serialize(ports);
            run.HeartbeatAt = _clock.UtcNow;
            await _store.UpdateRunAsync(run, cancellationToken);
            if (run.Status is AutomationStatuses.Succeeded or AutomationStatuses.Failed or AutomationStatuses.Cancelled)
            {
                await _notifier.RunFinishedAsync(run, cancellationToken);
            }
        }

        private async Task EnqueueWakeupAsync(AutomationRun run, string nodeId, StepResult result, CancellationToken cancellationToken)
        {
            await _store.AddWakeupAsync(new AutomationWakeup
            {
                Id = Guid.NewGuid(),
                Kind = result.WakeupKind ?? AutomationStatuses.WakeupDelay,
                DueAtUtc = result.ResumeAtUtc ?? _clock.UtcNow,
                AutomationId = run.AutomationId,
                VersionId = run.VersionId,
                RunId = run.Id,
                NodeId = nodeId,
                Status = AutomationStatuses.WakeupPending
            }, cancellationToken);
        }

        private static void WriteStepOutput(JsonObject variables, WorkflowNode node, StepResult result)
        {
            var steps = variables["steps"] as JsonObject ?? new JsonObject();
            var record = new JsonObject();
            if (result.Output != null)
            {
                record["result"] = JsonNodes.Clone(result.Output);
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                record["error"] = result.Error;
            }

            steps[node.Name] = record;
            variables["steps"] = steps;
        }

        private static bool IsReady(
            WorkflowGraph graph,
            WorkflowNode node,
            HashSet<string> completed,
            Dictionary<string, string> ports)
        {
            if (completed.Contains(node.Id))
            {
                return false;
            }

            var incoming = graph.Edges.Where(edge => edge.To == node.Id).ToList();
            if (incoming.Count == 0)
            {
                return false;
            }

            return incoming.All(edge => IncomingSatisfied(graph, edge, completed, ports));
        }

        private static bool IncomingSatisfied(
            WorkflowGraph graph,
            WorkflowEdge edge,
            HashSet<string> completed,
            Dictionary<string, string> ports)
        {
            if (!completed.Contains(edge.From))
            {
                return false;
            }

            if (string.IsNullOrEmpty(edge.Port))
            {
                return true;
            }

            if (!ports.TryGetValue(edge.From, out var chosen))
            {
                return false;
            }

            if (string.Equals(chosen, edge.Port, StringComparison.Ordinal))
            {
                return true;
            }

            var fromNode = graph.FindNode(edge.From);
            return fromNode?.Type == AutomationTypeCodes.Switch
                && edge.Port == "default"
                && !graph.Edges.Any(item => item.From == edge.From && item.Port == chosen);
        }

        private static JsonObject ParseObject(string json)
        {
            return JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json) as JsonObject ?? new JsonObject();
        }

        private static HashSet<string> ParseIds(string json)
        {
            var list = JsonSerializer.Deserialize<List<string>>(string.IsNullOrWhiteSpace(json) ? "[]" : json)
                       ?? new List<string>();
            return new HashSet<string>(list, StringComparer.Ordinal);
        }

        private static Dictionary<string, string> ParsePorts(string json)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(string.IsNullOrWhiteSpace(json) ? "{}" : json)
                   ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
