using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Automation.Scheduler;

namespace Partnerly.Automation.Hosting
{
    public class AutomationWorker : BackgroundService
    {
        private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan StaleHeartbeat = TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AutomationWorker> _logger;

        public AutomationWorker(IServiceScopeFactory scopes, ILogger<AutomationWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Automation worker tick failed.");
                }

                try
                {
                    await Task.Delay(PollDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public async Task TickAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IAutomationStore>();
            var clock = scope.ServiceProvider.GetRequiredService<IAutomationClock>();
            var leaser = new WakeupLeaser(store, clock);

            await leaser.RecoverExpiredAsync(cancellationToken);
            await EnqueueStaleRecoveriesAsync(store, clock, cancellationToken);

            var batch = await leaser.LeaseDueAsync(cancellationToken);
            foreach (var wakeup in batch)
            {
                using var inner = _scopes.CreateScope();
                await ProcessAsync(inner.ServiceProvider, wakeup, cancellationToken);
            }
        }

        private async Task EnqueueStaleRecoveriesAsync(
            IAutomationStore store,
            IAutomationClock clock,
            CancellationToken cancellationToken)
        {
            var stale = await store.GetStaleRunningAsync(clock.UtcNow - StaleHeartbeat, cancellationToken);
            foreach (var run in stale)
            {
                run.HeartbeatAt = clock.UtcNow;
                await store.UpdateRunAsync(run, cancellationToken);
                await store.AddWakeupAsync(new AutomationWakeup
                {
                    Id = Guid.NewGuid(),
                    Kind = AutomationStatuses.WakeupRecover,
                    DueAtUtc = clock.UtcNow,
                    AutomationId = run.AutomationId,
                    VersionId = run.VersionId,
                    RunId = run.Id,
                    Status = AutomationStatuses.WakeupPending
                }, cancellationToken);
            }
        }

        private async Task ProcessAsync(
            IServiceProvider services,
            AutomationWakeup wakeup,
            CancellationToken cancellationToken)
        {
            var store = services.GetRequiredService<IAutomationStore>();
            var runs = services.GetRequiredService<IAutomationRunService>();
            var scheduler = services.GetRequiredService<IAutomationScheduler>();
            var clock = services.GetRequiredService<IAutomationClock>();

            try
            {
                if (wakeup.RunId != null)
                {
                    await runs.AdvanceAsync(wakeup.RunId.Value, cancellationToken);
                    await MaybeRearmAsync(store, scheduler, wakeup, cancellationToken);
                    wakeup.Status = AutomationStatuses.WakeupDone;
                }
                else
                {
                    await StartFromTriggerAsync(store, runs, scheduler, clock, wakeup, cancellationToken);
                    if (wakeup.Status == AutomationStatuses.WakeupLeased)
                    {
                        wakeup.Status = AutomationStatuses.WakeupDone;
                    }
                }

                wakeup.LeaseUntil = null;
                wakeup.LeaseOwner = null;
                await store.UpdateWakeupAsync(wakeup, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automation wakeup {WakeupId} failed.", wakeup.Id);
                wakeup.Status = AutomationStatuses.WakeupPending;
                wakeup.DueAtUtc = clock.UtcNow.AddSeconds(WakeupLeaser.LeaseSeconds);
                wakeup.LeaseUntil = null;
                wakeup.LeaseOwner = null;
                await store.UpdateWakeupAsync(wakeup, cancellationToken);
            }
        }

        private static async Task StartFromTriggerAsync(
            IAutomationStore store,
            IAutomationRunService runs,
            IAutomationScheduler scheduler,
            IAutomationClock clock,
            AutomationWakeup wakeup,
            CancellationToken cancellationToken)
        {
            if (wakeup.AutomationId == null || wakeup.VersionId == null)
            {
                wakeup.Status = AutomationStatuses.WakeupCancelled;
                return;
            }

            var definition = await store.GetDefinitionAsync(wakeup.AutomationId.Value, cancellationToken);
            if (definition == null || definition.IsDeleted || !definition.IsActive || definition.PublishedVersionId == null)
            {
                wakeup.Status = AutomationStatuses.WakeupCancelled;
                return;
            }

            var active = await store.CountActiveRunsAsync(definition.Id, cancellationToken);
            if (active >= definition.MaxConcurrentRuns)
            {
                wakeup.DueAtUtc = clock.UtcNow.AddMinutes(1);
                wakeup.Status = AutomationStatuses.WakeupPending;
                return;
            }

            var since = clock.UtcNow.AddHours(-1);
            var recent = await store.CountRunsSinceAsync(definition.Id, since, cancellationToken);
            if (recent >= definition.MaxRunsPerHour)
            {
                wakeup.DueAtUtc = clock.UtcNow.AddMinutes(1);
                wakeup.Status = AutomationStatuses.WakeupPending;
                return;
            }

            var version = await store.GetVersionAsync(definition.PublishedVersionId.Value, cancellationToken)
                          ?? throw new InvalidOperationException("Published version was not found.");
            var triggerType = wakeup.Kind switch
            {
                AutomationStatuses.WakeupInterval => AutomationTypeCodes.Interval,
                AutomationStatuses.WakeupDelay => AutomationTypeCodes.DelayTrigger,
                _ => AutomationTypeCodes.Schedule
            };
            var run = await runs.EnqueueAsync(
                definition,
                version,
                triggerType,
                wakeup.Kind,
                dryRun: false,
                new TriggerContext { Type = triggerType, OwnerUserId = definition.OwnerUserId },
                cancellationToken);
            await runs.AdvanceAsync(run.Id, cancellationToken);
            if (wakeup.Kind != AutomationStatuses.WakeupDelay)
            {
                await RearmRecurringAsync(store, scheduler, definition, version, wakeup, cancellationToken);
            }
        }

        private static async Task MaybeRearmAsync(
            IAutomationStore store,
            IAutomationScheduler scheduler,
            AutomationWakeup wakeup,
            CancellationToken cancellationToken)
        {
            if (wakeup.Kind != AutomationStatuses.WakeupSchedule && wakeup.Kind != AutomationStatuses.WakeupInterval)
            {
                return;
            }

            if (wakeup.AutomationId == null || wakeup.VersionId == null)
            {
                return;
            }

            var definition = await store.GetDefinitionAsync(wakeup.AutomationId.Value, cancellationToken);
            var version = await store.GetVersionAsync(wakeup.VersionId.Value, cancellationToken);
            if (definition == null || version == null)
            {
                return;
            }

            await RearmRecurringAsync(store, scheduler, definition, version, wakeup, cancellationToken);
        }

        private static async Task RearmRecurringAsync(
            IAutomationStore store,
            IAutomationScheduler scheduler,
            AutomationDefinition definition,
            AutomationVersion version,
            AutomationWakeup wakeup,
            CancellationToken cancellationToken)
        {
            var graph = WorkflowGraph.Parse(version.GraphJson);
            var type = wakeup.Kind == AutomationStatuses.WakeupInterval
                ? AutomationTypeCodes.Interval
                : AutomationTypeCodes.Schedule;
            var trigger = graph.Nodes.FirstOrDefault(node => node.Kind == "trigger" && node.Type == type);
            if (trigger == null)
            {
                return;
            }

            await scheduler.ArmTriggerAsync(definition, version, trigger, cancellationToken);
        }
    }
}
