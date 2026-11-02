using Microsoft.AspNetCore.SignalR;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Integration
{
    public class AutomationRunNotifier : IAutomationRunNotifier
    {
        private readonly IHubContext<AutomationHub> _hub;

        public AutomationRunNotifier(IHubContext<AutomationHub> hub)
        {
            _hub = hub;
        }

        public Task RunStartedAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            return SendAsync(run, "automation.run.started", null, run.Status, cancellationToken);
        }

        public Task StepAsync(AutomationRun run, string nodeId, string status, CancellationToken cancellationToken = default)
        {
            var eventName = status == AutomationStatuses.StepFailed
                ? "automation.step.failed"
                : status is AutomationStatuses.StepSucceeded or AutomationStatuses.StepSkipped
                    ? "automation.step.completed"
                    : "automation.step.started";
            return SendAsync(run, eventName, nodeId, status, cancellationToken);
        }

        public Task RunFinishedAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            var eventName = run.Status switch
            {
                AutomationStatuses.Failed => "automation.run.failed",
                AutomationStatuses.Cancelled => "automation.run.cancelled",
                _ => "automation.run.completed"
            };
            return SendAsync(run, eventName, null, run.Status, cancellationToken);
        }

        private Task SendAsync(
            AutomationRun run,
            string eventName,
            string? nodeId,
            string status,
            CancellationToken cancellationToken)
        {
            return _hub.Clients.User(run.OwnerUserId.ToString()).SendAsync(
                eventName,
                new
                {
                    runId = run.Id,
                    automationId = run.AutomationId,
                    nodeId,
                    status
                },
                cancellationToken);
        }
    }
}
