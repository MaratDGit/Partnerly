using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Integration
{
    // In-app notification to the owner when a live run fails. One alert per streak: if the run before this one
    // failed too, nothing is sent (an every-minute agent must not flood the bell).
    public class RunFailureAlert : IRunFailureAlert
    {
        private readonly IAutomationStore _store;
        private readonly INotificationService _notifications;

        public RunFailureAlert(IAutomationStore store, INotificationService notifications)
        {
            _store = store;
            _notifications = notifications;
        }

        public async Task RunFailedAsync(AutomationRun run, AutomationDefinition definition, WorkflowGraph graph, CancellationToken cancellationToken = default)
        {
            if (graph.Settings?.NotifyOnFailure == false)
            {
                return;
            }

            var previous = _store.QueryRuns()
                .Where(item => item.AutomationId == run.AutomationId && !item.IsDryRun && item.Id != run.Id && item.CreatedDate < run.CreatedDate)
                .OrderByDescending(item => item.CreatedDate)
                .Select(item => item.Status)
                .FirstOrDefault();
            if (previous == AutomationStatuses.Failed)
            {
                return;
            }

            var reason = string.IsNullOrWhiteSpace(run.Error) ? "see the run history" : run.Error;
            if (reason.Length > 300)
            {
                reason = reason.Substring(0, 300) + "...";
            }

            await _notifications.CreateNotificationAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = definition.OwnerUserId,
                Type = "System",
                Message = $"Agent \"{definition.Name}\" failed: {reason}",
                Link = $"/Automation/Builder/{definition.Id}"
            });
        }
    }
}
