using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Persistence
{
    public class InMemoryAutomationStore : IAutomationStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, AutomationDefinition> _definitions = new();
        private readonly Dictionary<Guid, AutomationVersion> _versions = new();
        private readonly Dictionary<Guid, AutomationRun> _runs = new();
        private readonly List<AutomationRunStep> _steps = new();
        private readonly Dictionary<Guid, AutomationWakeup> _wakeups = new();
        private readonly Dictionary<Guid, List<AutomationTrigger>> _triggers = new();
        private readonly Dictionary<Guid, AutomationConnection> _connections = new();

        public Task<AutomationDefinition> AddDefinitionAsync(AutomationDefinition definition, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _definitions[definition.Id] = Clone(definition);
            }

            return Task.FromResult(Clone(definition));
        }

        public Task UpdateDefinitionAsync(AutomationDefinition definition, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _definitions[definition.Id] = Clone(definition);
            }

            return Task.CompletedTask;
        }

        public Task<AutomationDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_definitions.TryGetValue(id, out var item) ? Clone(item) : null);
            }
        }

        public Task<AutomationVersion> AddVersionAsync(AutomationVersion version, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _versions[version.Id] = Clone(version);
            }

            return Task.FromResult(Clone(version));
        }

        public Task UpdateVersionAsync(AutomationVersion version, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _versions[version.Id] = Clone(version);
            }

            return Task.CompletedTask;
        }

        public Task<AutomationVersion?> GetVersionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_versions.TryGetValue(id, out var item) ? Clone(item) : null);
            }
        }

        public Task ReplaceTriggersAsync(Guid versionId, IReadOnlyList<AutomationTrigger> triggers, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _triggers[versionId] = triggers.Select(Clone).ToList();
            }

            return Task.CompletedTask;
        }

        public Task ClearWebhookIdsAsync(Guid versionId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_triggers.TryGetValue(versionId, out var list))
                {
                    foreach (var item in list)
                    {
                        item.WebhookPublicId = null;
                        item.WebhookSecretHash = null;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task<AutomationRun> AddRunAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _runs[run.Id] = Clone(run);
            }

            return Task.FromResult(Clone(run));
        }

        public Task UpdateRunAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _runs[run.Id] = Clone(run);
            }

            return Task.CompletedTask;
        }

        public Task<AutomationRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_runs.TryGetValue(id, out var item) ? Clone(item) : null);
            }
        }

        public Task AddStepAsync(AutomationRunStep step, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _steps.Add(Clone(step));
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AutomationRunStep>> GetStepsAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IReadOnlyList<AutomationRunStep> items = _steps
                    .Where(step => step.RunId == runId)
                    .Select(Clone)
                    .ToList();
                return Task.FromResult(items);
            }
        }

        public Task AddWakeupAsync(AutomationWakeup wakeup, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _wakeups[wakeup.Id] = Clone(wakeup);
            }

            return Task.CompletedTask;
        }

        public Task UpdateWakeupAsync(AutomationWakeup wakeup, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _wakeups[wakeup.Id] = Clone(wakeup);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AutomationWakeup>> GetOpenWakeupsAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IReadOnlyList<AutomationWakeup> items = _wakeups.Values
                    .Where(item => item.AutomationId == automationId
                                   && (item.Status == AutomationStatuses.WakeupPending || item.Status == AutomationStatuses.WakeupLeased))
                    .OrderBy(item => item.DueAtUtc)
                    .Select(Clone)
                    .ToList();
                return Task.FromResult(items);
            }
        }

        public Task<IReadOnlyList<AutomationWakeup>> GetPendingDueAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IReadOnlyList<AutomationWakeup> items = _wakeups.Values
                    .Where(item => item.Status == AutomationStatuses.WakeupPending && item.DueAtUtc <= utcNow)
                    .OrderBy(item => item.DueAtUtc)
                    .Take(take)
                    .Select(Clone)
                    .ToList();
                return Task.FromResult(items);
            }
        }

        public Task RecoverExpiredLeasesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                foreach (var wakeup in _wakeups.Values.Where(item =>
                             item.Status == AutomationStatuses.WakeupLeased
                             && item.LeaseUntil != null
                             && item.LeaseUntil < utcNow))
                {
                    wakeup.Status = AutomationStatuses.WakeupPending;
                    wakeup.LeaseUntil = null;
                    wakeup.LeaseOwner = null;
                }
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AutomationWakeup>> LeaseDueAsync(
            DateTime utcNow,
            string owner,
            DateTime leaseUntil,
            int take,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                foreach (var wakeup in _wakeups.Values.Where(item =>
                             item.Status == AutomationStatuses.WakeupLeased
                             && item.LeaseUntil != null
                             && item.LeaseUntil < utcNow))
                {
                    wakeup.Status = AutomationStatuses.WakeupPending;
                    wakeup.LeaseUntil = null;
                    wakeup.LeaseOwner = null;
                }

                var leased = _wakeups.Values
                    .Where(item => item.Status == AutomationStatuses.WakeupPending && item.DueAtUtc <= utcNow)
                    .OrderBy(item => item.DueAtUtc)
                    .Take(take)
                    .ToList();
                foreach (var wakeup in leased)
                {
                    wakeup.Status = AutomationStatuses.WakeupLeased;
                    wakeup.LeaseUntil = leaseUntil;
                    wakeup.LeaseOwner = owner;
                    wakeup.Attempt++;
                }

                IReadOnlyList<AutomationWakeup> items = leased.Select(Clone).ToList();
                return Task.FromResult(items);
            }
        }

        public Task CancelPendingWakeupsAsync(
            Guid automationId,
            IReadOnlyCollection<string> kinds,
            CancellationToken cancellationToken = default,
            bool onlyWithoutRun = false)
        {
            lock (_gate)
            {
                foreach (var wakeup in _wakeups.Values.Where(item =>
                             item.AutomationId == automationId
                             && item.Status == AutomationStatuses.WakeupPending
                             && kinds.Contains(item.Kind)
                             && (!onlyWithoutRun || item.RunId == null)))
                {
                    wakeup.Status = AutomationStatuses.WakeupCancelled;
                }
            }

            return Task.CompletedTask;
        }

        public Task CancelPendingWakeupsForRunAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                foreach (var wakeup in _wakeups.Values.Where(IsOpenRunWakeup))
                {
                    if (wakeup.RunId != runId)
                    {
                        continue;
                    }

                    wakeup.Status = AutomationStatuses.WakeupCancelled;
                    wakeup.LeaseUntil = null;
                    wakeup.LeaseOwner = null;
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsOpenRunWakeup(AutomationWakeup item) =>
            item.Status == AutomationStatuses.WakeupPending
            || item.Status == AutomationStatuses.WakeupLeased;

        public Task<IReadOnlyList<AutomationRun>> GetStaleRunningAsync(
            DateTime heartbeatOlderThan,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IReadOnlyList<AutomationRun> items = _runs.Values
                    .Where(run => run.Status == AutomationStatuses.Running
                                  && (run.HeartbeatAt == null || run.HeartbeatAt < heartbeatOlderThan))
                    .Select(Clone)
                    .ToList();
                return Task.FromResult(items);
            }
        }

        public Task<int> CountActiveRunsAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var count = _runs.Values.Count(run =>
                    run.AutomationId == automationId
                    && !run.IsDryRun
                    && (run.Status == AutomationStatuses.Queued
                        || run.Status == AutomationStatuses.Running
                        || run.Status == AutomationStatuses.Waiting));
                return Task.FromResult(count);
            }
        }

        public Task<int> CountRunsSinceAsync(Guid automationId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var count = _runs.Values.Count(run =>
                    run.AutomationId == automationId
                    && !run.IsDryRun
                    && run.CreatedDate >= sinceUtc);
                return Task.FromResult(count);
            }
        }

        public Task<IReadOnlyList<ArmedAutomationTrigger>> GetEnabledTriggersAsync(
            string triggerType,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var items = new List<ArmedAutomationTrigger>();
                foreach (var pair in _triggers)
                {
                    if (!_versions.TryGetValue(pair.Key, out var version)
                        || !_definitions.TryGetValue(version.AutomationId, out var definition)
                        || !definition.IsActive
                        || definition.IsDeleted
                        || definition.PublishedVersionId != version.Id
                        || !version.IsPublished)
                    {
                        continue;
                    }

                    foreach (var trigger in pair.Value)
                    {
                        if (!trigger.IsEnabled
                            || !string.Equals(trigger.Type, triggerType, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        items.Add(new ArmedAutomationTrigger
                        {
                            Trigger = Clone(trigger),
                            Version = Clone(version),
                            Definition = Clone(definition)
                        });
                    }
                }

                IReadOnlyList<ArmedAutomationTrigger> result = items;
                return Task.FromResult(result);
            }
        }

        public Task<ArmedAutomationTrigger?> GetWebhookTriggerAsync(
            string webhookPublicId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(webhookPublicId))
            {
                return Task.FromResult<ArmedAutomationTrigger?>(null);
            }

            lock (_gate)
            {
                foreach (var pair in _triggers)
                {
                    if (!_versions.TryGetValue(pair.Key, out var version)
                        || !_definitions.TryGetValue(version.AutomationId, out var definition)
                        || !definition.IsActive
                        || definition.IsDeleted
                        || definition.PublishedVersionId != version.Id)
                    {
                        continue;
                    }

                    var trigger = pair.Value.FirstOrDefault(item =>
                        item.IsEnabled
                        && string.Equals(item.WebhookPublicId, webhookPublicId, StringComparison.Ordinal));
                    if (trigger == null)
                    {
                        continue;
                    }

                    return Task.FromResult<ArmedAutomationTrigger?>(new ArmedAutomationTrigger
                    {
                        Trigger = Clone(trigger),
                        Version = Clone(version),
                        Definition = Clone(definition)
                    });
                }
            }

            return Task.FromResult<ArmedAutomationTrigger?>(null);
        }

        public Task<IReadOnlyList<AutomationTrigger>> GetTriggersAsync(
            Guid versionId,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IReadOnlyList<AutomationTrigger> items = _triggers.TryGetValue(versionId, out var list)
                    ? list.Select(Clone).ToList()
                    : Array.Empty<AutomationTrigger>();
                return Task.FromResult(items);
            }
        }

        public IQueryable<AutomationDefinition> QueryDefinitions()
        {
            lock (_gate)
            {
                return _definitions.Values.Select(Clone).AsQueryable();
            }
        }

        private readonly Dictionary<string, AutomationDataItem> _dataItems = new();

        private static string DataKey(Guid automationId, string key) => automationId.ToString("N") + "|" + key;

        public Task<AutomationDataItem?> GetDataItemAsync(Guid automationId, string key, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_dataItems.TryGetValue(DataKey(automationId, key), out var item) && (item.ExpiresAt == null || item.ExpiresAt > utcNow))
                {
                    return Task.FromResult<AutomationDataItem?>(new AutomationDataItem
                    {
                        Id = item.Id, AutomationId = item.AutomationId, Key = item.Key, Value = item.Value,
                        ExpiresAt = item.ExpiresAt, CreatedDate = item.CreatedDate, UpdatedDate = item.UpdatedDate
                    });
                }

                return Task.FromResult<AutomationDataItem?>(null);
            }
        }

        public Task SetDataItemAsync(Guid automationId, string key, string value, DateTime utcNow, DateTime? expiresAt, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var id = DataKey(automationId, key);
                if (!_dataItems.TryGetValue(id, out var item))
                {
                    item = new AutomationDataItem { Id = Guid.NewGuid(), AutomationId = automationId, Key = key, CreatedDate = utcNow };
                    _dataItems[id] = item;
                }

                item.Value = value;
                item.ExpiresAt = expiresAt;
                item.UpdatedDate = utcNow;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TryAddDataItemAsync(Guid automationId, string key, string value, DateTime utcNow, DateTime? expiresAt, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var id = DataKey(automationId, key);
                if (_dataItems.TryGetValue(id, out var item) && (item.ExpiresAt == null || item.ExpiresAt > utcNow))
                {
                    return Task.FromResult(false);
                }

                _dataItems[id] = new AutomationDataItem
                {
                    Id = Guid.NewGuid(), AutomationId = automationId, Key = key, Value = value,
                    ExpiresAt = expiresAt, CreatedDate = utcNow, UpdatedDate = utcNow
                };
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteDataItemAsync(Guid automationId, string key, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_dataItems.Remove(DataKey(automationId, key)));
            }
        }

        public IQueryable<AutomationVersion> QueryVersions()
        {
            lock (_gate)
            {
                return _versions.Values.Select(Clone).ToList().AsQueryable();
            }
        }

        public IQueryable<AutomationRun> QueryRuns()
        {
            lock (_gate)
            {
                return _runs.Values.Select(Clone).AsQueryable();
            }
        }

        public IQueryable<AutomationConnection> QueryConnections()
        {
            lock (_gate)
            {
                return _connections.Values.Select(Clone).AsQueryable();
            }
        }

        public Task<AutomationConnection> AddConnectionAsync(AutomationConnection connection, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _connections[connection.Id] = Clone(connection);
            }

            return Task.FromResult(Clone(connection));
        }

        public Task UpdateConnectionAsync(AutomationConnection connection, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _connections[connection.Id] = Clone(connection);
            }

            return Task.CompletedTask;
        }

        public Task<AutomationConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_connections.TryGetValue(id, out var item) ? Clone(item) : null);
            }
        }

        private static AutomationDefinition Clone(AutomationDefinition source) => new()
        {
            Id = source.Id,
            OwnerUserId = source.OwnerUserId,
            Name = source.Name,
            Description = source.Description,
            IsActive = source.IsActive,
            IsDeleted = source.IsDeleted,
            PublishedVersionId = source.PublishedVersionId,
            DraftVersionId = source.DraftVersionId,
            TimeZoneId = source.TimeZoneId,
            MaxConcurrentRuns = source.MaxConcurrentRuns,
            MaxRunsPerHour = source.MaxRunsPerHour,
            StepTimeoutSeconds = source.StepTimeoutSeconds,
            RunTimeoutSeconds = source.RunTimeoutSeconds,
            RetryLimit = source.RetryLimit,
            Note = source.Note,
            CreatedBy = source.CreatedBy,
            CreatedDate = source.CreatedDate,
            UpdatedBy = source.UpdatedBy,
            UpdatedDate = source.UpdatedDate
        };

        private static AutomationVersion Clone(AutomationVersion source) => new()
        {
            Id = source.Id,
            AutomationId = source.AutomationId,
            Number = source.Number,
            GraphJson = source.GraphJson,
            GraphSchemaVersion = source.GraphSchemaVersion,
            IsPublished = source.IsPublished,
            PublishedAt = source.PublishedAt,
            Note = source.Note,
            CreatedBy = source.CreatedBy,
            CreatedDate = source.CreatedDate
        };

        private static AutomationTrigger Clone(AutomationTrigger source) => new()
        {
            Id = source.Id,
            VersionId = source.VersionId,
            Type = source.Type,
            ConfigJson = source.ConfigJson,
            WebhookPublicId = source.WebhookPublicId,
            WebhookSecretHash = source.WebhookSecretHash,
            IsEnabled = source.IsEnabled
        };

        private static AutomationRun Clone(AutomationRun source) => new()
        {
            Id = source.Id,
            AutomationId = source.AutomationId,
            VersionId = source.VersionId,
            OwnerUserId = source.OwnerUserId,
            TriggerType = source.TriggerType,
            TriggerReason = source.TriggerReason,
            Status = source.Status,
            IsDryRun = source.IsDryRun,
            Depth = source.Depth,
            IdempotencyKey = source.IdempotencyKey,
            ContextJson = source.ContextJson,
            ChosenPortsJson = source.ChosenPortsJson,
            CompletedNodeIdsJson = source.CompletedNodeIdsJson,
            Error = source.Error,
            StartedAt = source.StartedAt,
            EndedAt = source.EndedAt,
            HeartbeatAt = source.HeartbeatAt,
            CancelledBy = source.CancelledBy,
            RetryCount = source.RetryCount,
            CreatedDate = source.CreatedDate
        };

        private static AutomationRunStep Clone(AutomationRunStep source) => new()
        {
            Id = source.Id,
            RunId = source.RunId,
            NodeId = source.NodeId,
            ActionType = source.ActionType,
            Status = source.Status,
            Attempt = source.Attempt,
            InputJson = source.InputJson,
            OutputJson = source.OutputJson,
            Error = source.Error,
            StartedAt = source.StartedAt,
            EndedAt = source.EndedAt
        };

        private static AutomationWakeup Clone(AutomationWakeup source) => new()
        {
            Id = source.Id,
            Kind = source.Kind,
            DueAtUtc = source.DueAtUtc,
            AutomationId = source.AutomationId,
            VersionId = source.VersionId,
            TriggerId = source.TriggerId,
            RunId = source.RunId,
            NodeId = source.NodeId,
            PayloadJson = source.PayloadJson,
            LeaseUntil = source.LeaseUntil,
            LeaseOwner = source.LeaseOwner,
            Attempt = source.Attempt,
            Status = source.Status
        };

        private static AutomationConnection Clone(AutomationConnection source) => new()
        {
            Id = source.Id,
            OwnerUserId = source.OwnerUserId,
            Name = source.Name,
            Kind = source.Kind,
            EncryptedPayload = source.EncryptedPayload,
            IsDeleted = source.IsDeleted,
            Note = source.Note,
            CreatedBy = source.CreatedBy,
            CreatedDate = source.CreatedDate,
            UpdatedBy = source.UpdatedBy,
            UpdatedDate = source.UpdatedDate
        };
    }
}
