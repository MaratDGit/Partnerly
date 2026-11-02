using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Scheduler
{
    public class ScheduleArmer : IAutomationScheduler
    {
        private static readonly string[] RecurringKinds =
        {
            AutomationStatuses.WakeupSchedule,
            AutomationStatuses.WakeupInterval
        };

        private readonly IAutomationStore _store;
        private readonly IAutomationClock _clock;
        private readonly CronScheduleCalculator _calculator;

        public ScheduleArmer(
            IAutomationStore store,
            IAutomationClock clock,
            CronScheduleCalculator calculator)
        {
            _store = store;
            _clock = clock;
            _calculator = calculator;
        }

        public async Task ArmPublishedAsync(
            AutomationDefinition definition,
            AutomationVersion version,
            CancellationToken cancellationToken = default)
        {
            await _store.CancelPendingWakeupsAsync(definition.Id, RecurringKinds, cancellationToken);
            await _store.CancelPendingWakeupsAsync(
                definition.Id,
                new[] { AutomationStatuses.WakeupDelay },
                cancellationToken,
                onlyWithoutRun: true);
            var graph = WorkflowGraph.Parse(version.GraphJson);
            foreach (var trigger in graph.Nodes.Where(IsArmedTrigger))
            {
                await ArmTriggerAsync(definition, version, trigger, cancellationToken);
            }
        }

        public async Task ArmTriggerAsync(
            AutomationDefinition definition,
            AutomationVersion version,
            WorkflowNode triggerNode,
            CancellationToken cancellationToken = default)
        {
            DateTime due;
            string kind;
            if (triggerNode.Type == AutomationTypeCodes.DelayTrigger)
            {
                DelayDuration.TryRead(triggerNode.Config, out var delay);
                due = _clock.UtcNow.Add(delay);
                kind = AutomationStatuses.WakeupDelay;
            }
            else
            {
                var next = await NextDueUtcAsync(triggerNode, _clock.UtcNow, definition.TimeZoneId);
                if (next == null)
                {
                    return;
                }

                due = next.Value;
                kind = triggerNode.Type == AutomationTypeCodes.Interval
                    ? AutomationStatuses.WakeupInterval
                    : AutomationStatuses.WakeupSchedule;
            }

            await _store.AddWakeupAsync(new AutomationWakeup
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                DueAtUtc = due,
                AutomationId = definition.Id,
                VersionId = version.Id,
                Status = AutomationStatuses.WakeupPending
            }, cancellationToken);
        }

        public Task<DateTime?> NextDueUtcAsync(WorkflowNode triggerNode, DateTime utcNow, string timeZoneId)
        {
            return Task.FromResult(_calculator.NextDueUtc(triggerNode, utcNow, timeZoneId));
        }

        private static bool IsArmedTrigger(WorkflowNode node)
        {
            return node.Kind == "trigger"
                   && (node.Type == AutomationTypeCodes.Schedule
                       || node.Type == AutomationTypeCodes.Interval
                       || node.Type == AutomationTypeCodes.DelayTrigger);
        }
    }
}
