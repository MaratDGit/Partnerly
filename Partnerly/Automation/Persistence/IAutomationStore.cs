using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence
{
    public interface IAutomationStore
    {
        Task<AutomationDefinition> AddDefinitionAsync(AutomationDefinition definition, CancellationToken cancellationToken = default);
        Task UpdateDefinitionAsync(AutomationDefinition definition, CancellationToken cancellationToken = default);
        Task<AutomationDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AutomationVersion> AddVersionAsync(AutomationVersion version, CancellationToken cancellationToken = default);
        Task UpdateVersionAsync(AutomationVersion version, CancellationToken cancellationToken = default);
        Task<AutomationVersion?> GetVersionAsync(Guid id, CancellationToken cancellationToken = default);
        Task ReplaceTriggersAsync(Guid versionId, IReadOnlyList<AutomationTrigger> triggers, CancellationToken cancellationToken = default);

        // WebhookPublicId is unique across all versions: a version that stops being the published one releases its ids.
        Task ClearWebhookIdsAsync(Guid versionId, CancellationToken cancellationToken = default);

        Task<AutomationRun> AddRunAsync(AutomationRun run, CancellationToken cancellationToken = default);
        Task UpdateRunAsync(AutomationRun run, CancellationToken cancellationToken = default);
        Task<AutomationRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddStepAsync(AutomationRunStep step, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutomationRunStep>> GetStepsAsync(Guid runId, CancellationToken cancellationToken = default);

        Task AddWakeupAsync(AutomationWakeup wakeup, CancellationToken cancellationToken = default);
        Task UpdateWakeupAsync(AutomationWakeup wakeup, CancellationToken cancellationToken = default);
        // Pending / leased wakeups of one agent (next scheduled start, resume of waiting runs).
        Task<IReadOnlyList<AutomationWakeup>> GetOpenWakeupsAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutomationWakeup>> GetPendingDueAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default);
        Task RecoverExpiredLeasesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutomationWakeup>> LeaseDueAsync(
            DateTime utcNow,
            string owner,
            DateTime leaseUntil,
            int take,
            CancellationToken cancellationToken = default);
        Task CancelPendingWakeupsAsync(
            Guid automationId,
            IReadOnlyCollection<string> kinds,
            CancellationToken cancellationToken = default,
            bool onlyWithoutRun = false);
        Task CancelPendingWakeupsForRunAsync(Guid runId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutomationRun>> GetStaleRunningAsync(DateTime heartbeatOlderThan, CancellationToken cancellationToken = default);
        Task<int> CountActiveRunsAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task<int> CountRunsSinceAsync(Guid automationId, DateTime sinceUtc, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ArmedAutomationTrigger>> GetEnabledTriggersAsync(
            string triggerType,
            CancellationToken cancellationToken = default);
        Task<ArmedAutomationTrigger?> GetWebhookTriggerAsync(
            string webhookPublicId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutomationTrigger>> GetTriggersAsync(
            Guid versionId,
            CancellationToken cancellationToken = default);

        IQueryable<AutomationDefinition> QueryDefinitions();
        IQueryable<AutomationRun> QueryRuns();
        IQueryable<AutomationConnection> QueryConnections();
        Task<AutomationConnection> AddConnectionAsync(AutomationConnection connection, CancellationToken cancellationToken = default);
        Task UpdateConnectionAsync(AutomationConnection connection, CancellationToken cancellationToken = default);
        Task<AutomationConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
