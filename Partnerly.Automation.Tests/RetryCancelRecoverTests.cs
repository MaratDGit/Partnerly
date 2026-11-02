using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Hosting;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Tests
{
    public class RetryCancelRecoverTests
    {
        [Fact]
        public async Task Failed_create_retries_until_limit_then_fails()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "retry");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""c1"", ""kind"": ""action"", ""type"": ""create_record"", ""name"": ""make"", ""config"": { ""entity"": ""user"", ""fields"": { ""email"": ""a@b.c"" } } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""c1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "retry", dryRun: false);

            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, run.Status);
            Assert.Equal(1, run.RetryCount);

            await runs.AdvanceAsync(run.Id);
            run = (await store.GetRunAsync(run.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, run.Status);
            Assert.Equal(2, run.RetryCount);

            await runs.AdvanceAsync(run.Id);
            run = (await store.GetRunAsync(run.Id))!;
            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("Unknown entity", run.Error);

            var steps = await store.GetStepsAsync(run.Id);
            Assert.Equal(3, steps.Count(step => step.NodeId == "c1" && step.Status == AutomationStatuses.StepFailed));
        }

        [Fact]
        public async Task Cancel_queued_run_drops_wakeup_and_worker_does_not_start()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var clock = provider.GetRequiredService<IAutomationClock>();

            var automation = await authoring.CreateDraftAsync(owner, "cancel-queued");
            await authoring.UpdateGraphAsync(automation.Id, LinearManualGraph());
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "queued", dryRun: false);
            Assert.Equal(AutomationStatuses.Queued, queued.Status);

            await runs.CancelAsync(queued.Id, owner);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Cancelled, run.Status);
            Assert.Equal(owner, run.CancelledBy);
            Assert.Empty(await store.GetPendingDueAsync(clock.UtcNow, 10));

            var worker = new AutomationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AutomationWorker>.Instance);
            await worker.TickAsync();

            run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Cancelled, run.Status);
            Assert.Empty(await store.GetStepsAsync(run.Id));
        }

        [Fact]
        public async Task Cancel_waiting_delay_does_not_resume()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "cancel-wait");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""d1"", ""kind"": ""action"", ""type"": ""delay"", ""name"": ""pause"", ""config"": { ""delay"": ""00:00:05"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""after"", ""config"": { ""name"": ""done"", ""value"": ""1"" } }
              ],
              ""edges"": [
                { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""d1"" },
                { ""id"": ""e2"", ""from"": ""d1"", ""to"": ""a1"" }
              ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "wait", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, run.Status);

            await runs.CancelAsync(run.Id, owner);
            clock.UtcNow = clock.UtcNow.AddSeconds(5);
            Assert.Empty(await store.GetPendingDueAsync(clock.UtcNow, 10));

            await runs.AdvanceAsync(run.Id);
            var worker = new AutomationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AutomationWorker>.Instance);
            await worker.TickAsync();

            run = (await store.GetRunAsync(run.Id))!;
            Assert.Equal(AutomationStatuses.Cancelled, run.Status);
            var steps = await store.GetStepsAsync(run.Id);
            Assert.DoesNotContain(steps, step => step.NodeId == "a1");
        }

        [Fact]
        public async Task Worker_recovers_stale_running_run()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "stale");
            await authoring.UpdateGraphAsync(automation.Id, LinearManualGraph());
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "stale", dryRun: false);

            var start = (await store.GetPendingDueAsync(clock.UtcNow, 10)).Single();
            start.Status = AutomationStatuses.WakeupDone;
            await store.UpdateWakeupAsync(start);

            queued.Status = AutomationStatuses.Running;
            queued.HeartbeatAt = clock.UtcNow.AddMinutes(-3);
            await store.UpdateRunAsync(queued);

            var worker = new AutomationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AutomationWorker>.Instance);
            await worker.TickAsync();

            var run = await store.GetRunAsync(queued.Id);
            Assert.Equal(AutomationStatuses.Succeeded, run!.Status);
        }

        private static WorkflowGraph LinearManualGraph() => WorkflowGraph.Parse(@"{
          ""schemaVersion"": 1,
          ""nodes"": [
            { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
            { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""flag"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
          ],
          ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
        }");
    }
}
