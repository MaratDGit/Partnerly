using Microsoft.EntityFrameworkCore;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Persistence
{
    public class AutomationStore : IAutomationStore
    {
        private readonly AppDbContext _db;

        public AutomationStore(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AutomationDefinition> AddDefinitionAsync(AutomationDefinition definition, CancellationToken cancellationToken = default)
        {
            _db.Automations.Add(definition);
            await _db.SaveChangesAsync(cancellationToken);
            Detach(definition);
            return definition;
        }

        public async Task UpdateDefinitionAsync(AutomationDefinition definition, CancellationToken cancellationToken = default)
        {
            await SaveDisconnectedAsync(_db.Automations, definition, cancellationToken);
        }

        public Task<AutomationDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.Automations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<AutomationVersion> AddVersionAsync(AutomationVersion version, CancellationToken cancellationToken = default)
        {
            _db.AutomationVersions.Add(version);
            await _db.SaveChangesAsync(cancellationToken);
            Detach(version);
            return version;
        }

        public async Task UpdateVersionAsync(AutomationVersion version, CancellationToken cancellationToken = default)
        {
            await SaveDisconnectedAsync(_db.AutomationVersions, version, cancellationToken);
        }

        public Task<AutomationVersion?> GetVersionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.AutomationVersions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task ReplaceTriggersAsync(Guid versionId, IReadOnlyList<AutomationTrigger> triggers, CancellationToken cancellationToken = default)
        {
            var existing = await _db.AutomationTriggers.Where(item => item.VersionId == versionId).ToListAsync(cancellationToken);
            _db.AutomationTriggers.RemoveRange(existing);
            _db.AutomationTriggers.AddRange(triggers);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<AutomationRun> AddRunAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            _db.AutomationRuns.Add(run);
            await _db.SaveChangesAsync(cancellationToken);
            Detach(run);
            return run;
        }

        public async Task UpdateRunAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            await SaveDisconnectedAsync(_db.AutomationRuns, run, cancellationToken);
        }

        public Task<AutomationRun?> GetRunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.AutomationRuns.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task AddStepAsync(AutomationRunStep step, CancellationToken cancellationToken = default)
        {
            _db.AutomationRunSteps.Add(step);
            await _db.SaveChangesAsync(cancellationToken);
            Detach(step);
        }

        public async Task<IReadOnlyList<AutomationRunStep>> GetStepsAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            return await _db.AutomationRunSteps
                .AsNoTracking()
                .Where(step => step.RunId == runId)
                .OrderBy(step => step.StartedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddWakeupAsync(AutomationWakeup wakeup, CancellationToken cancellationToken = default)
        {
            _db.AutomationWakeups.Add(wakeup);
            await _db.SaveChangesAsync(cancellationToken);
            Detach(wakeup);
        }

        public async Task UpdateWakeupAsync(AutomationWakeup wakeup, CancellationToken cancellationToken = default)
        {
            await SaveDisconnectedAsync(_db.AutomationWakeups, wakeup, cancellationToken);
        }

        public async Task<IReadOnlyList<AutomationWakeup>> GetPendingDueAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default)
        {
            return await _db.AutomationWakeups
                .AsNoTracking()
                .Where(item => item.Status == AutomationStatuses.WakeupPending && item.DueAtUtc <= utcNow)
                .OrderBy(item => item.DueAtUtc)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task RecoverExpiredLeasesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE [AutomationWakeups]
SET [Status] = {AutomationStatuses.WakeupPending},
    [LeaseUntil] = NULL,
    [LeaseOwner] = NULL
WHERE [Status] = {AutomationStatuses.WakeupLeased}
  AND [LeaseUntil] IS NOT NULL
  AND [LeaseUntil] < {utcNow}", cancellationToken);
        }

        public async Task<IReadOnlyList<AutomationWakeup>> LeaseDueAsync(
            DateTime utcNow,
            string owner,
            DateTime leaseUntil,
            int take,
            CancellationToken cancellationToken = default)
        {
            await RecoverExpiredLeasesAsync(utcNow, cancellationToken);

            var pending = AutomationStatuses.WakeupPending;
            var leased = AutomationStatuses.WakeupLeased;
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE [w]
SET [w].[Status] = {leased},
    [w].[LeaseUntil] = {leaseUntil},
    [w].[LeaseOwner] = {owner},
    [w].[Attempt] = [w].[Attempt] + 1
FROM [AutomationWakeups] AS [w]
INNER JOIN (
    SELECT TOP ({take}) [Id]
    FROM [AutomationWakeups] WITH (ROWLOCK, READPAST)
    WHERE [Status] = {pending} AND [DueAtUtc] <= {utcNow}
    ORDER BY [DueAtUtc]
) AS [s] ON [w].[Id] = [s].[Id]", cancellationToken);

            return await _db.AutomationWakeups
                .AsNoTracking()
                .Where(item => item.Status == AutomationStatuses.WakeupLeased
                               && item.LeaseOwner == owner
                               && item.LeaseUntil == leaseUntil)
                .OrderBy(item => item.DueAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task CancelPendingWakeupsAsync(
            Guid automationId,
            IReadOnlyCollection<string> kinds,
            CancellationToken cancellationToken = default,
            bool onlyWithoutRun = false)
        {
            var pending = await _db.AutomationWakeups
                .Where(item => item.AutomationId == automationId
                               && item.Status == AutomationStatuses.WakeupPending
                               && kinds.Contains(item.Kind)
                               && (!onlyWithoutRun || item.RunId == null))
                .ToListAsync(cancellationToken);
            foreach (var wakeup in pending)
            {
                wakeup.Status = AutomationStatuses.WakeupCancelled;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task CancelPendingWakeupsForRunAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            var open = await _db.AutomationWakeups
                .Where(item => item.RunId == runId
                               && (item.Status == AutomationStatuses.WakeupPending
                                   || item.Status == AutomationStatuses.WakeupLeased))
                .ToListAsync(cancellationToken);
            foreach (var wakeup in open)
            {
                wakeup.Status = AutomationStatuses.WakeupCancelled;
                wakeup.LeaseUntil = null;
                wakeup.LeaseOwner = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AutomationRun>> GetStaleRunningAsync(
            DateTime heartbeatOlderThan,
            CancellationToken cancellationToken = default)
        {
            return await _db.AutomationRuns
                .AsNoTracking()
                .Where(run => run.Status == AutomationStatuses.Running
                              && (run.HeartbeatAt == null || run.HeartbeatAt < heartbeatOlderThan))
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountActiveRunsAsync(Guid automationId, CancellationToken cancellationToken = default)
        {
            return _db.AutomationRuns.CountAsync(run =>
                run.AutomationId == automationId
                && !run.IsDryRun
                && (run.Status == AutomationStatuses.Queued
                    || run.Status == AutomationStatuses.Running
                    || run.Status == AutomationStatuses.Waiting), cancellationToken);
        }

        public Task<int> CountRunsSinceAsync(Guid automationId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        {
            return _db.AutomationRuns.CountAsync(run =>
                run.AutomationId == automationId
                && !run.IsDryRun
                && run.CreatedDate >= sinceUtc, cancellationToken);
        }

        public async Task<IReadOnlyList<ArmedAutomationTrigger>> GetEnabledTriggersAsync(
            string triggerType,
            CancellationToken cancellationToken = default)
        {
            var rows = await (
                from trigger in _db.AutomationTriggers.AsNoTracking()
                join version in _db.AutomationVersions.AsNoTracking() on trigger.VersionId equals version.Id
                join definition in _db.Automations.AsNoTracking() on version.AutomationId equals definition.Id
                where trigger.Type == triggerType
                      && trigger.IsEnabled
                      && definition.IsActive
                      && !definition.IsDeleted
                      && definition.PublishedVersionId == version.Id
                      && version.IsPublished
                select new { trigger, version, definition }).ToListAsync(cancellationToken);

            return rows.Select(row => new ArmedAutomationTrigger
            {
                Trigger = row.trigger,
                Version = row.version,
                Definition = row.definition
            }).ToList();
        }

        public async Task<ArmedAutomationTrigger?> GetWebhookTriggerAsync(
            string webhookPublicId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(webhookPublicId))
            {
                return null;
            }

            var row = await (
                from trigger in _db.AutomationTriggers.AsNoTracking()
                join version in _db.AutomationVersions.AsNoTracking() on trigger.VersionId equals version.Id
                join definition in _db.Automations.AsNoTracking() on version.AutomationId equals definition.Id
                where trigger.WebhookPublicId == webhookPublicId
                      && trigger.IsEnabled
                      && definition.IsActive
                      && !definition.IsDeleted
                      && definition.PublishedVersionId == version.Id
                select new { trigger, version, definition }).FirstOrDefaultAsync(cancellationToken);

            return row == null
                ? null
                : new ArmedAutomationTrigger
                {
                    Trigger = row.trigger,
                    Version = row.version,
                    Definition = row.definition
                };
        }

        public async Task<IReadOnlyList<AutomationTrigger>> GetTriggersAsync(
            Guid versionId,
            CancellationToken cancellationToken = default)
        {
            return await _db.AutomationTriggers
                .AsNoTracking()
                .Where(item => item.VersionId == versionId)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<AutomationDefinition> QueryDefinitions() =>
            _db.Automations.AsNoTracking();

        public IQueryable<AutomationRun> QueryRuns() =>
            _db.AutomationRuns.AsNoTracking();

        public IQueryable<AutomationConnection> QueryConnections() =>
            _db.AutomationConnections.AsNoTracking();

        public async Task<AutomationConnection> AddConnectionAsync(
            AutomationConnection connection,
            CancellationToken cancellationToken = default)
        {
            _db.AutomationConnections.Add(connection);
            await _db.SaveChangesAsync(cancellationToken);
            Detach(connection);
            return connection;
        }

        public async Task UpdateConnectionAsync(AutomationConnection connection, CancellationToken cancellationToken = default)
        {
            await SaveDisconnectedAsync(_db.AutomationConnections, connection, cancellationToken);
        }

        public Task<AutomationConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _db.AutomationConnections.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        private async Task SaveDisconnectedAsync<T>(DbSet<T> set, T entity, CancellationToken cancellationToken)
            where T : class
        {
            var key = _db.Entry(entity).Property("Id").CurrentValue;
            var local = set.Local.FirstOrDefault(item => Equals(_db.Entry(item).Property("Id").CurrentValue, key));
            if (local != null)
            {
                _db.Entry(local).CurrentValues.SetValues(entity);
            }
            else
            {
                set.Update(entity);
            }

            await _db.SaveChangesAsync(cancellationToken);
            Detach(entity);
            if (local != null)
            {
                Detach(local);
            }
        }

        private void Detach(object entity)
        {
            _db.Entry(entity).State = EntityState.Detached;
        }
    }
}
