using System.Text.Json;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Actions;
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
            // Steps that failed for good. The rest of the flow goes on; only the cards behind a
            // failed one wait forever (a join needs all its inputs). Kept in the ports map
            // under "failed:<nodeId>" so no new column is needed.
            var failed = new HashSet<string>(
                ports.Keys.Where(key => key.StartsWith(FailedPrefix, StringComparison.Ordinal)).Select(key => key.Substring(FailedPrefix.Length)),
                StringComparer.Ordinal);
            var steps = (await _store.GetStepsAsync(run.Id, cancellationToken)).ToList();

            foreach (var trigger in graph.Nodes.Where(node => node.Kind == "trigger"))
            {
                completed.Add(trigger.Id);
            }

            var startedNow = run.StartedAt == null;
            if (startedNow && graph.Settings?.Variables is { Count: > 0 } constants)
            {
                // agent-level constants: {{vars.name}} from the first card on
                var vars = variables["vars"] as JsonObject ?? new JsonObject();
                foreach (var pair in constants)
                {
                    if (!vars.ContainsKey(pair.Key))
                    {
                        vars[pair.Key] = pair.Value;
                    }
                }

                variables["vars"] = vars;
            }

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
                    .Where(node => node.Kind == "action" && !failed.Contains(node.Id) && IsReady(graph, node, completed, ports))
                    .OrderBy(node => node.Id, StringComparer.Ordinal)
                    .ToList();

                if (ready.Count == 0)
                {
                    if (AdvanceLoops(graph, variables, ports, completed, failed))
                    {
                        continue;
                    }

                    if (failed.Count == 0)
                    {
                        run.Status = AutomationStatuses.Succeeded;
                    }
                    else
                    {
                        run.Status = AutomationStatuses.Failed;
                        run.Error = FailureSummary(graph, steps, failed);
                    }

                    run.EndedAt = _clock.UtcNow;
                    break;
                }

                var node = ready[0];
                if (node.Disabled)
                {
                    await SkipDisabledAsync(run, graph, node, variables, ports, steps, cancellationToken);
                    steps = (await _store.GetStepsAsync(run.Id, cancellationToken)).ToList();
                    completed.Add(node.Id);
                    continue;
                }

                if (node.Type == AutomationTypeCodes.Loop)
                {
                    await StartLoopAsync(run, graph, node, variables, ports, completed, failed, cancellationToken);
                    steps = (await _store.GetStepsAsync(run.Id, cancellationToken)).ToList();
                    continue;
                }

                var repeated = ActiveLoopBodies(graph, variables).Contains(node.Id);
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
                var result = await _stepRunner.RunAsync(context, steps, definition.RetryLimit, cancellationToken, repeated);
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
                        // Final failure of this card: by default its chain stops and the other chains go on;
                        // "On error" can route it to an error path or let the flow carry on.
                        ApplyFinalFailure(graph, node, result, variables, ports, completed, failed);
                        continue;
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

            if (run.Status == AutomationStatuses.Failed && !run.IsDryRun
                && services.GetService(typeof(IRunFailureAlert)) is IRunFailureAlert alert)
            {
                try
                {
                    await alert.RunFailedAsync(run, definition, graph, cancellationToken);
                }
                catch (Exception)
                {
                    // an alert that cannot be sent must never change the result of the run
                }
            }
        }

        // ---- "On error" -------------------------------------------------------
        // stop (default): the card's chain ends, the run is marked failed at the end.
        // continue: the error is recorded, the flow goes on along the normal path.
        // route (or a card wired to the red "error" output): the flow goes on along the error path only.
        private static void ApplyFinalFailure(
            WorkflowGraph graph,
            WorkflowNode node,
            StepResult result,
            JsonObject variables,
            Dictionary<string, string> ports,
            HashSet<string> completed,
            HashSet<string> failed)
        {
            WriteStepOutput(variables, node, result);
            var mode = NodeConfig.GetString(node, "onError");
            var errorWired = graph.Edges.Any(edge => edge.From == node.Id && edge.Port == ErrorPort);
            if (errorWired || string.Equals(mode, "route", StringComparison.Ordinal))
            {
                ports[node.Id] = ErrorPort;
                completed.Add(node.Id);
                return;
            }

            if (string.Equals(mode, "continue", StringComparison.Ordinal))
            {
                completed.Add(node.Id);
                return;
            }

            failed.Add(node.Id);
            ports[FailedPrefix + node.Id] = "1";
        }

        private const string ErrorPort = "error";

        // ---- "For each" ---------------------------------------------------------
        private const string LoopsKey = "_loops";

        private static HashSet<string> Reach(WorkflowGraph graph, IEnumerable<string> starts, string stopAt)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>(starts);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (id == stopAt || !seen.Add(id))
                {
                    continue;
                }

                foreach (var edge in graph.Edges.Where(item => item.From == id))
                {
                    queue.Enqueue(edge.To);
                }
            }

            return seen;
        }

        // The cards one pass of a "For each" repeats: everything after its "each" output that the "done" path does not also reach.
        public static HashSet<string> LoopBody(WorkflowGraph graph, string loopId)
        {
            var body = Reach(graph, graph.Edges.Where(edge => edge.From == loopId && edge.Port == "each").Select(edge => edge.To), loopId);
            var after = Reach(graph, graph.Edges.Where(edge => edge.From == loopId && edge.Port != "each").Select(edge => edge.To), loopId);
            body.ExceptWith(after);
            return body;
        }

        private static HashSet<string> ActiveLoopBodies(WorkflowGraph graph, JsonObject variables)
        {
            var all = new HashSet<string>(StringComparer.Ordinal);
            if (variables[LoopsKey] is JsonObject loops)
            {
                foreach (var pair in loops)
                {
                    all.UnionWith(LoopBody(graph, pair.Key));
                }
            }

            return all;
        }

        private static void SetLoopVariable(JsonObject variables, WorkflowNode node, JsonArray items, int index)
        {
            variables["loop"] = new JsonObject
            {
                ["name"] = node.Name,
                ["item"] = JsonNodes.Clone(items[index]),
                ["index"] = index,
                ["number"] = index + 1,
                ["count"] = items.Count
            };
        }

        private async Task StartLoopAsync(
            AutomationRun run,
            WorkflowGraph graph,
            WorkflowNode node,
            JsonObject variables,
            Dictionary<string, string> ports,
            HashSet<string> completed,
            HashSet<string> failed,
            CancellationToken cancellationToken)
        {
            var started = _clock.UtcNow;
            var existing = await _store.GetStepsAsync(run.Id, cancellationToken);
            await _notifier.StepAsync(run, node.Id, "started", cancellationToken);
            var ok = LoopItems.TryRead(node, _binder, variables, out var items, out var error);
            var result = ok
                ? StepResult.Succeeded(new JsonObject { ["count"] = items.Count })
                : StepResult.Failed(error, retryable: false);
            await _store.AddStepAsync(new AutomationRunStep
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                NodeId = node.Id,
                ActionType = node.Type,
                Status = result.Status,
                Attempt = existing.Count(step => step.NodeId == node.Id) + 1,
                OutputJson = result.Output?.ToJsonString(),
                Error = result.Error,
                StartedAt = started,
                EndedAt = _clock.UtcNow
            }, cancellationToken);
            await _notifier.StepAsync(run, node.Id, result.Status, cancellationToken);

            if (!ok)
            {
                ApplyFinalFailure(graph, node, result, variables, ports, completed, failed);
                return;
            }

            WriteStepOutput(variables, node, result);
            completed.Add(node.Id);
            if (items.Count == 0)
            {
                ports[node.Id] = "done";
                return;
            }

            var loops = variables[LoopsKey] as JsonObject ?? new JsonObject();
            loops[node.Id] = new JsonObject { ["i"] = 0, ["n"] = items.Count, ["items"] = items };
            variables[LoopsKey] = loops;
            SetLoopVariable(variables, node, items, 0);
            ports[node.Id] = "each";
        }

        // Called when nothing is ready: the pass over the current item is finished, so start the next
        // item, or - after the last one - open the "done" path. True when it changed something.
        private static bool AdvanceLoops(
            WorkflowGraph graph,
            JsonObject variables,
            Dictionary<string, string> ports,
            HashSet<string> completed,
            HashSet<string> failed)
        {
            if (variables[LoopsKey] is not JsonObject loops || loops.Count == 0)
            {
                return false;
            }

            var open = loops.FirstOrDefault(pair => pair.Value is JsonObject item && item["failed"]?.GetValue<bool>() != true);
            if (open.Key == null)
            {
                return false;
            }

            var key = open.Key;
            var node = graph.FindNode(key);
            var state = (JsonObject)loops[key]!;
            var body = LoopBody(graph, key);
            if (node == null || body.Any(failed.Contains))
            {
                // an item failed for good: stop going through the list, the loop counts as failed.
                // The state stays so "Resume" can continue with this very item.
                state["failed"] = true;
                failed.Add(key);
                ports[FailedPrefix + key] = "1";
                return true;
            }

            var index = state["i"]!.GetValue<int>();
            var count = state["n"]!.GetValue<int>();
            if (index + 1 < count)
            {
                index++;
                state["i"] = index;
                foreach (var id in body)
                {
                    completed.Remove(id);
                    ports.Remove(id);
                }

                SetLoopVariable(variables, node, (JsonArray)state["items"]!, index);
                return true;
            }

            loops.Remove(key);
            ports[key] = "done";
            if (loops.Count == 0)
            {
                variables.Remove("loop");
            }

            return true;
        }

        // A card that is turned off does nothing and the flow goes on past it, whatever
        // number of cards lead into it or out of it. A branch / switch has no answer to give,
        // so the flow follows its first connected output.
        private async Task SkipDisabledAsync(
            AutomationRun run,
            WorkflowGraph graph,
            WorkflowNode node,
            JsonObject variables,
            Dictionary<string, string> ports,
            IReadOnlyList<AutomationRunStep> steps,
            CancellationToken cancellationToken)
        {
            var result = StepResult.Skipped("disabled");
            await _store.AddStepAsync(new AutomationRunStep
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                NodeId = node.Id,
                ActionType = node.Type,
                Status = result.Status,
                Attempt = steps.Count(step => step.NodeId == node.Id) + 1,
                OutputJson = result.Output?.ToJsonString(),
                StartedAt = _clock.UtcNow,
                EndedAt = _clock.UtcNow
            }, cancellationToken);
            await _notifier.StepAsync(run, node.Id, result.Status, cancellationToken);
            WriteStepOutput(variables, node, result);
            var port = graph.Edges.FirstOrDefault(edge => edge.From == node.Id && !string.IsNullOrEmpty(edge.Port))?.Port;
            if (port != null)
            {
                ports[node.Id] = port;
            }
        }

        private const string FailedPrefix = "failed:";

        private static string FailureSummary(WorkflowGraph graph, IReadOnlyList<AutomationRunStep> steps, HashSet<string> failed)
        {
            var lines = failed
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id =>
                {
                    var last = steps.Where(step => step.NodeId == id).OrderBy(step => step.Attempt).LastOrDefault();
                    return $"{graph.FindNode(id)?.Name ?? id}: {last?.Error ?? "failed"}";
                });
            var text = string.Join(" | ", lines);
            return text.Length > 1000 ? text.Substring(0, 1000) : text;
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

            ports.TryGetValue(edge.From, out var chosen);
            if (string.IsNullOrEmpty(edge.Port))
            {
                // the normal path is closed after an error that was routed, or when a filter said stop
                return chosen != ErrorPort && chosen != FilterActionHandler.StoppedPort;
            }

            if (chosen == null)
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
