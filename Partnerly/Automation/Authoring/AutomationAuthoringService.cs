using System.Security.Cryptography;
using System.Text.Json;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Helpers;

namespace Partnerly.Automation.Authoring
{
    public class AutomationAuthoringService : IAutomationAuthoringService
    {
        private readonly IAutomationStore _store;
        private readonly IAutomationClock _clock;
        private readonly IAutomationAccessService _access;
        private readonly GraphValidator _validator;
        private readonly IAutomationRunService _runs;
        private readonly IAutomationScheduler _scheduler;
        private readonly ISecretProtector _secrets;

        public AutomationAuthoringService(
            IAutomationStore store,
            IAutomationClock clock,
            IAutomationAccessService access,
            GraphValidator validator,
            IAutomationRunService runs,
            IAutomationScheduler scheduler,
            ISecretProtector secrets)
        {
            _store = store;
            _clock = clock;
            _access = access;
            _validator = validator;
            _runs = runs;
            _scheduler = scheduler;
            _secrets = secrets;
        }

        public async Task<AutomationDefinition> CreateDraftAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
        {
            var now = _clock.UtcNow;
            var definition = new AutomationDefinition
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                Name = string.IsNullOrWhiteSpace(name) ? "Untitled" : name.Trim(),
                CreatedBy = ownerUserId,
                CreatedDate = now,
                UpdatedBy = ownerUserId,
                UpdatedDate = now
            };
            var version = new AutomationVersion
            {
                Id = Guid.NewGuid(),
                AutomationId = definition.Id,
                Number = 1,
                GraphJson = new WorkflowGraph().ToJson(),
                CreatedBy = ownerUserId,
                CreatedDate = now
            };
            await _store.AddDefinitionAsync(definition, cancellationToken);
            await _store.AddVersionAsync(version, cancellationToken);
            definition.DraftVersionId = version.Id;
            await _store.UpdateDefinitionAsync(definition, cancellationToken);
            return definition;
        }

        public Task<AutomationDefinition> GetAsync(Guid automationId, CancellationToken cancellationToken = default) =>
            RequireAsync(automationId, cancellationToken);

        public async Task<AutomationVersion> GetDraftVersionAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            return await GetDraftAsync(definition, cancellationToken);
        }

        public async Task UpdateGraphAsync(Guid automationId, WorkflowGraph graph, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            var version = await GetDraftAsync(definition, cancellationToken);
            version.GraphJson = graph.ToJson();
            version.GraphSchemaVersion = graph.SchemaVersion;
            await _store.UpdateVersionAsync(version, cancellationToken);
            await TouchAsync(definition, cancellationToken);
        }

        public async Task UpdateMetaAsync(Guid automationId, string name, string? description, string? icon = null, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            definition.Name = string.IsNullOrWhiteSpace(name) ? definition.Name : name.Trim();
            definition.Description = description;
            if (!string.IsNullOrWhiteSpace(icon))
            {
                definition.Icon = icon;
            }

            await TouchAsync(definition, cancellationToken);
        }

        public async Task UpdateSettingsAsync(
            Guid automationId,
            string timeZoneId,
            int maxConcurrentRuns,
            int maxRunsPerHour,
            int retryLimit,
            int stepTimeoutSeconds,
            int runTimeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            var zone = string.IsNullOrWhiteSpace(timeZoneId) ? definition.TimeZoneId : timeZoneId.Trim();
            if (!TimeZoneHelper.Choices.Any(item => string.Equals(item.Id, zone, StringComparison.OrdinalIgnoreCase)))
            {
                zone = SystemSettingsDefaults.TimeZoneId;
            }

            definition.TimeZoneId = zone;
            definition.MaxConcurrentRuns = Math.Clamp(maxConcurrentRuns, 1, 20);
            definition.MaxRunsPerHour = Math.Clamp(maxRunsPerHour, 1, 1000);
            definition.RetryLimit = Math.Clamp(retryLimit, 0, 10);
            definition.StepTimeoutSeconds = Math.Clamp(stepTimeoutSeconds, 1, 300);
            definition.RunTimeoutSeconds = Math.Clamp(runTimeoutSeconds, 1, 86400);
            await TouchAsync(definition, cancellationToken);
        }

        public async Task<AutomationDefinition> CreateFromTemplateAsync(
            Guid ownerUserId,
            string templateId,
            CancellationToken cancellationToken = default)
        {
            var preset = AutomationTemplateCatalog.Get(templateId)
                         ?? throw new KeyNotFoundException("Template was not found.");
            var created = await CreateDraftAsync(ownerUserId, preset.Name, cancellationToken);
            await UpdateGraphAsync(created.Id, WorkflowGraph.Parse(preset.GraphJson), cancellationToken);
            await UpdateMetaAsync(created.Id, preset.Name, preset.Description, null, cancellationToken);
            return await GetAsync(created.Id, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> ValidateAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            var version = await GetDraftAsync(definition, cancellationToken);
            return _validator.Validate(WorkflowGraph.Parse(version.GraphJson));
        }

        public async Task PublishAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            var draft = await GetDraftAsync(definition, cancellationToken);
            var errors = _validator.Validate(WorkflowGraph.Parse(draft.GraphJson));
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            var now = _clock.UtcNow;
            draft.IsPublished = true;
            draft.PublishedAt = now;
            await _store.UpdateVersionAsync(draft, cancellationToken);

            var previous = definition.PublishedVersionId == null
                ? Array.Empty<AutomationTrigger>()
                : await _store.GetTriggersAsync(definition.PublishedVersionId.Value, cancellationToken);
            var graph = WorkflowGraph.Parse(draft.GraphJson);

            // The Nth webhook node keeps the Nth previous URL and secret (stable across publishes).
            var reusable = new Queue<AutomationTrigger>(previous.Where(item =>
                item.Type == AutomationTypeCodes.Webhook && !string.IsNullOrEmpty(item.WebhookPublicId)));
            var triggers = graph.Nodes
                .Where(node => node.Kind == "trigger")
                .Select(node => CreateTrigger(draft.Id, node, node.Type == AutomationTypeCodes.Webhook && reusable.Count > 0 ? reusable.Dequeue() : null))
                .ToList();

            // The unique index on WebhookPublicId forbids the same id in two versions at once.
            if (definition.PublishedVersionId != null)
            {
                await _store.ClearWebhookIdsAsync(definition.PublishedVersionId.Value, cancellationToken);
            }

            await _store.ReplaceTriggersAsync(draft.Id, triggers, cancellationToken);

            var next = new AutomationVersion
            {
                Id = Guid.NewGuid(),
                AutomationId = definition.Id,
                Number = draft.Number + 1,
                GraphJson = draft.GraphJson,
                GraphSchemaVersion = draft.GraphSchemaVersion,
                CreatedBy = definition.OwnerUserId,
                CreatedDate = now
            };
            await _store.AddVersionAsync(next, cancellationToken);

            definition.PublishedVersionId = draft.Id;
            definition.DraftVersionId = next.Id;
            definition.UpdatedDate = now;
            await _store.UpdateDefinitionAsync(definition, cancellationToken);
            await _scheduler.ArmPublishedAsync(definition, draft, cancellationToken);
        }

        public async Task<bool> ToggleActiveAsync(Guid automationId, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            var next = isActive ?? !definition.IsActive;
            definition.IsActive = next;
            await TouchAsync(definition, cancellationToken);
            if (!next)
            {
                await _store.CancelPendingWakeupsAsync(
                    definition.Id,
                    new[]
                    {
                        AutomationStatuses.WakeupStart,
                        AutomationStatuses.WakeupSchedule,
                        AutomationStatuses.WakeupInterval
                    },
                    cancellationToken);
                await _store.CancelPendingWakeupsAsync(
                    definition.Id,
                    new[] { AutomationStatuses.WakeupDelay },
                    cancellationToken,
                    onlyWithoutRun: true);
            }
            else if (definition.PublishedVersionId != null)
            {
                var published = await _store.GetVersionAsync(definition.PublishedVersionId.Value, cancellationToken);
                if (published != null)
                {
                    await _scheduler.ArmPublishedAsync(definition, published, cancellationToken);
                }
            }

            return definition.IsActive;
        }

        public async Task DeleteAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            definition.IsDeleted = true;
            definition.IsActive = false;
            await TouchAsync(definition, cancellationToken);
            await _store.CancelPendingWakeupsAsync(
                definition.Id,
                new[]
                {
                    AutomationStatuses.WakeupStart,
                    AutomationStatuses.WakeupDelay,
                    AutomationStatuses.WakeupRetry,
                    AutomationStatuses.WakeupWait,
                    AutomationStatuses.WakeupSchedule,
                    AutomationStatuses.WakeupInterval
                },
                cancellationToken);
        }

        public async Task<AutomationDefinition> CopyAsync(Guid automationId, Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            var source = await RequireAsync(automationId, cancellationToken);
            var copy = await CreateDraftAsync(ownerUserId, source.Name + " copy", cancellationToken);
            var sourceDraft = await GetDraftAsync(source, cancellationToken);
            await UpdateGraphAsync(copy.Id, WorkflowGraph.Parse(sourceDraft.GraphJson), cancellationToken);
            return (await _store.GetDefinitionAsync(copy.Id, cancellationToken))!;
        }

        public async Task<AutomationRun> EnqueueRunAsync(Guid automationId, string reason, bool dryRun, CancellationToken cancellationToken = default)
        {
            var definition = await RequireAsync(automationId, cancellationToken);
            var versionId = definition.PublishedVersionId ?? definition.DraftVersionId
                            ?? throw new InvalidOperationException("Automation has no version.");
            if (!dryRun && definition.PublishedVersionId == null)
            {
                throw new InvalidOperationException("Publish the automation before a live run.");
            }

            var version = await _store.GetVersionAsync(dryRun ? definition.DraftVersionId ?? versionId : definition.PublishedVersionId ?? versionId, cancellationToken)
                          ?? throw new InvalidOperationException("Version was not found.");
            var run = await _runs.EnqueueAsync(
                definition,
                version,
                AutomationTypeCodes.Manual,
                reason,
                dryRun,
                new TriggerContext { Type = AutomationTypeCodes.Manual, OwnerUserId = definition.OwnerUserId },
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
            return (await _store.GetRunAsync(run.Id, cancellationToken))!;
        }

        private AutomationTrigger CreateTrigger(
            Guid versionId,
            WorkflowNode node,
            AutomationTrigger? previousWebhook)
        {
            var trigger = new AutomationTrigger
            {
                Id = Guid.NewGuid(),
                VersionId = versionId,
                Type = node.Type,
                ConfigJson = node.Config.ValueKind == JsonValueKind.Undefined ? "{}" : node.Config.GetRawText(),
                IsEnabled = !node.Disabled
            };

            if (node.Type != AutomationTypeCodes.Webhook)
            {
                return trigger;
            }

            trigger.WebhookPublicId = previousWebhook?.WebhookPublicId ?? Guid.NewGuid().ToString("N");
            trigger.WebhookSecretHash = previousWebhook?.WebhookSecretHash
                                        ?? _secrets.Protect(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            return trigger;
        }

        private async Task TouchAsync(AutomationDefinition definition, CancellationToken cancellationToken)
        {
            definition.UpdatedDate = _clock.UtcNow;
            definition.UpdatedBy = _access.CurrentUserId == Guid.Empty ? definition.OwnerUserId : _access.CurrentUserId;
            await _store.UpdateDefinitionAsync(definition, cancellationToken);
        }

        private async Task<AutomationDefinition> RequireAsync(Guid automationId, CancellationToken cancellationToken)
        {
            var definition = await _store.GetDefinitionAsync(automationId, cancellationToken);
            var visible = _access.VisibleOrNull(definition);
            if (visible == null)
            {
                throw new KeyNotFoundException("Automation was not found.");
            }

            return visible;
        }

        private async Task<AutomationVersion> GetDraftAsync(AutomationDefinition definition, CancellationToken cancellationToken)
        {
            if (definition.DraftVersionId == null)
            {
                throw new InvalidOperationException("Draft version is missing.");
            }

            return await _store.GetVersionAsync(definition.DraftVersionId.Value, cancellationToken)
                   ?? throw new InvalidOperationException("Draft version is missing.");
        }
    }
}
