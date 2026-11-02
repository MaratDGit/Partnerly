using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    public interface IAutomationRunService
    {
        Task<AutomationRun> EnqueueAsync(
            AutomationDefinition definition,
            AutomationVersion version,
            string triggerType,
            string reason,
            bool dryRun,
            TriggerContext? trigger,
            CancellationToken cancellationToken = default);

        Task AdvanceAsync(Guid runId, CancellationToken cancellationToken = default);
        Task CancelAsync(Guid runId, Guid cancelledBy, CancellationToken cancellationToken = default);
    }
}
