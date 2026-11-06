using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    public interface IAutomationAuthoringService
    {
        Task<AutomationDefinition> CreateDraftAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default);
        Task<AutomationDefinition> GetAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task<AutomationVersion> GetDraftVersionAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task UpdateGraphAsync(Guid automationId, WorkflowGraph graph, CancellationToken cancellationToken = default);
        Task UpdateMetaAsync(Guid automationId, string name, string? description, string? icon = null, CancellationToken cancellationToken = default);
        Task UpdateSettingsAsync(
            Guid automationId,
            string timeZoneId,
            int maxConcurrentRuns,
            int maxRunsPerHour,
            int retryLimit,
            int stepTimeoutSeconds,
            int runTimeoutSeconds,
            CancellationToken cancellationToken = default);
        Task UpdateTagsAsync(Guid automationId, string? tags, CancellationToken cancellationToken = default);
        Task<AutomationDefinition> CreateFromTemplateAsync(Guid ownerUserId, string templateId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> ValidateAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task PublishAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task<bool> ToggleActiveAsync(Guid automationId, bool? isActive = null, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid automationId, CancellationToken cancellationToken = default);
        Task<AutomationDefinition> CopyAsync(Guid automationId, Guid ownerUserId, CancellationToken cancellationToken = default);
        Task<AutomationRun> EnqueueRunAsync(Guid automationId, string reason, bool dryRun, CancellationToken cancellationToken = default);
    }
}
