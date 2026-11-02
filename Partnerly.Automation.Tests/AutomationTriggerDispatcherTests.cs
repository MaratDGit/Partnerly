using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Tests
{
    public class AutomationTriggerDispatcherTests
    {
        [Fact]
        public async Task Entity_created_enqueues_matching_automation()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var dispatcher = provider.GetRequiredService<AutomationTriggerDispatcher>();

            var automation = await authoring.CreateDraftAsync(owner, "onUser");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""entity.created"", ""name"": ""userCreated"", ""config"": { ""entity"": ""user"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);

            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });

            Assert.Equal(1, await store.CountActiveRunsAsync(automation.Id));
            var wakeups = await store.GetPendingDueAsync(clock.UtcNow, 10);
            Assert.Contains(wakeups, item => item.AutomationId == automation.Id && item.Kind == AutomationStatuses.WakeupStart);
        }

        [Fact]
        public async Task Entity_created_honors_amount_unit_wait_before_start()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var dispatcher = provider.GetRequiredService<AutomationTriggerDispatcher>();

            var automation = await authoring.CreateDraftAsync(owner, "later-user");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""entity.created"", ""name"": ""userCreated"", ""config"": { ""entity"": ""user"", ""amount"": 5, ""unit"": ""minutes"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);

            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });

            Assert.Empty(await store.GetPendingDueAsync(clock.UtcNow, 10));
            var later = await store.GetPendingDueAsync(clock.UtcNow.AddMinutes(5), 10);
            var wakeup = Assert.Single(later, item => item.AutomationId == automation.Id);
            Assert.Equal(AutomationStatuses.WakeupStart, wakeup.Kind);
            Assert.Equal(clock.UtcNow.AddMinutes(5), wakeup.DueAtUtc);
        }

        [Fact]
        public async Task Missing_entity_in_config_does_not_match_all()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var dispatcher = provider.GetRequiredService<AutomationTriggerDispatcher>();

            var automation = await authoring.CreateDraftAsync(owner, "openTrigger");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""entity.created"", ""name"": ""anyCreated"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);

            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });

            Assert.Equal(0, await store.CountActiveRunsAsync(automation.Id));
        }

        [Fact]
        public async Task Max_concurrent_runs_blocks_second_enqueue()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var dispatcher = provider.GetRequiredService<AutomationTriggerDispatcher>();

            var automation = await CreatePublishedEntityTriggerAsync(authoring, owner, "cap");
            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });
            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });

            Assert.Equal(1, await store.CountActiveRunsAsync(automation.Id));
            Assert.Equal(1, await store.CountRunsSinceAsync(automation.Id, clock.UtcNow.AddHours(-1)));
        }

        [Fact]
        public async Task Max_runs_per_hour_blocks_after_completed_run()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var dispatcher = provider.GetRequiredService<AutomationTriggerDispatcher>();
            var runs = provider.GetRequiredService<IAutomationRunService>();

            var automation = await CreatePublishedEntityTriggerAsync(authoring, owner, "hourly");
            var loaded = await store.GetDefinitionAsync(automation.Id);
            loaded!.MaxRunsPerHour = 1;
            await store.UpdateDefinitionAsync(loaded);

            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });
            var wakeup = (await store.GetPendingDueAsync(clock.UtcNow, 10))
                .Single(item => item.AutomationId == automation.Id);
            await runs.AdvanceAsync(wakeup.RunId!.Value);
            Assert.Equal(0, await store.CountActiveRunsAsync(automation.Id));

            await dispatcher.EnqueueEntityAsync(
                AutomationTypeCodes.EntityCreated,
                "user",
                new JsonObject { ["id"] = Guid.NewGuid().ToString() });

            Assert.Equal(1, await store.CountRunsSinceAsync(automation.Id, clock.UtcNow.AddHours(-1)));
            Assert.Equal(0, await store.CountActiveRunsAsync(automation.Id));
        }

        [Fact]
        public async Task Find_record_without_connector_returns_null()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "find");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""f1"", ""kind"": ""action"", ""type"": ""find_record"", ""name"": ""lookup"", ""config"": { ""entity"": ""user"", ""filters"": { ""email"": ""a@b.c"" } } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""f1"" } ]
            }"));
            var queued = await authoring.EnqueueRunAsync(automation.Id, "find", dryRun: true);
            await provider.GetRequiredService<IAutomationRunService>().AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            var steps = await store.GetStepsAsync(run.Id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Equal(AutomationStatuses.StepSucceeded, steps.Single().Status);
            Assert.Contains("null", steps.Single().OutputJson ?? string.Empty);
        }

        private static async Task<Partnerly.Automation.Persistence.Entities.AutomationDefinition> CreatePublishedEntityTriggerAsync(
            IAutomationAuthoringService authoring,
            Guid owner,
            string name)
        {
            var automation = await authoring.CreateDraftAsync(owner, name);
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""entity.created"", ""name"": ""userCreated"", ""config"": { ""entity"": ""user"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            return automation;
        }
    }
}
