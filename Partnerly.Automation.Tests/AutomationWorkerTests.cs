using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Hosting;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Tests
{
    public class AutomationWorkerTests
    {
        [Fact]
        public async Task Tick_advances_queued_manual_run()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "worker");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""flag"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "tick", dryRun: false);
            Assert.Equal(AutomationStatuses.Queued, queued.Status);

            var worker = new AutomationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AutomationWorker>.Instance);
            await worker.TickAsync();

            var run = await store.GetRunAsync(queued.Id);
            Assert.Equal(AutomationStatuses.Succeeded, run!.Status);
        }

        [Fact]
        public async Task Expired_lease_returns_to_pending_then_runs()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "lease");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""flag"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "lease", dryRun: false);
            var due = await store.GetPendingDueAsync(clock.UtcNow, 10);
            var wakeup = due.Single();
            wakeup.Status = AutomationStatuses.WakeupLeased;
            wakeup.LeaseUntil = clock.UtcNow.AddSeconds(-1);
            wakeup.LeaseOwner = "dead-worker";
            await store.UpdateWakeupAsync(wakeup);

            var worker = new AutomationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AutomationWorker>.Instance);
            await worker.TickAsync();

            var run = await store.GetRunAsync(queued.Id);
            Assert.Equal(AutomationStatuses.Succeeded, run!.Status);
        }

        [Fact]
        public async Task Delay_trigger_arms_once_on_publish_and_starts_after_wait()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "later");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""delay"", ""name"": ""later"", ""config"": { ""amount"": 5, ""unit"": ""minutes"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""flag"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);

            Assert.Empty(await store.GetPendingDueAsync(clock.UtcNow, 10));
            var armed = await store.GetPendingDueAsync(clock.UtcNow.AddMinutes(5), 10);
            Assert.Contains(armed, item =>
                item.AutomationId == automation.Id
                && item.Kind == AutomationStatuses.WakeupDelay
                && item.RunId == null);

            clock.UtcNow = clock.UtcNow.AddMinutes(5);
            var worker = new AutomationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AutomationWorker>.Instance);
            await worker.TickAsync();

            Assert.Equal(1, await store.CountRunsSinceAsync(automation.Id, clock.UtcNow.AddHours(-1)));
            var leftover = await store.GetPendingDueAsync(clock.UtcNow.AddDays(1), 50);
            Assert.DoesNotContain(leftover, item =>
                item.AutomationId == automation.Id && item.Kind == AutomationStatuses.WakeupDelay && item.RunId == null);
        }
    }
}
