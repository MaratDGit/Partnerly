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

        // Same logic version and the same trigger data again, as a new run.
        Task<AutomationRun> RerunAsync(Guid runId, CancellationToken cancellationToken = default);

        // Continue a failed run from the cards that failed (cards that succeeded are not repeated).
        Task<bool> ResumeFailedAsync(Guid runId, CancellationToken cancellationToken = default);
    }
}
