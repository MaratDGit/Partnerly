using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Authoring;
using Partnerly.Automation.Persistence;

namespace Partnerly.Automation.Tests
{
    public class ExecuteWorkflowAndWaitTests
    {
        [Fact]
        public async Task Execute_workflow_enqueues_child_for_same_owner()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var child = await authoring.CreateDraftAsync(owner, "child");
            await authoring.UpdateGraphAsync(child.Id, LinearManualGraph());
            await authoring.PublishAsync(child.Id);

            var parent = await authoring.CreateDraftAsync(owner, "parent");
            await authoring.UpdateGraphAsync(parent.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""w1"", ""kind"": ""action"", ""type"": ""execute_workflow"", ""name"": ""child"", ""config"": { ""automationId"": """ + child.Id + @""" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""w1"" } ]
            }"));
            await authoring.PublishAsync(parent.Id);

            var queued = await authoring.EnqueueRunAsync(parent.Id, "parent", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            var parentRun = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Succeeded, parentRun.Status);
            Assert.Equal(1, await store.CountActiveRunsAsync(child.Id));

            var childWakeup = (await store.GetPendingDueAsync(provider.GetRequiredService<IAutomationClock>().UtcNow, 10))
                .Single(item => item.AutomationId == child.Id);
            await runs.AdvanceAsync(childWakeup.RunId!.Value);
            var childRun = (await store.GetRunAsync(childWakeup.RunId.Value))!;
            Assert.Equal(AutomationStatuses.Succeeded, childRun.Status);
            Assert.Equal(1, childRun.Depth);
        }

        [Fact]
        public async Task Execute_workflow_hides_other_owners_child()
        {
            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            var store = new InMemoryAutomationStore();
            await using var ownerHost = AutomationTestHost.Create(owner, store: store);
            await using var otherHost = AutomationTestHost.Create(other, store: store);

            var foreign = await otherHost.GetRequiredService<IAutomationAuthoringService>()
                .CreateDraftAsync(other, "secret");
            await otherHost.GetRequiredService<IAutomationAuthoringService>()
                .UpdateGraphAsync(foreign.Id, LinearManualGraph());
            await otherHost.GetRequiredService<IAutomationAuthoringService>().PublishAsync(foreign.Id);

            var authoring = ownerHost.GetRequiredService<IAutomationAuthoringService>();
            var parent = await authoring.CreateDraftAsync(owner, "parent");
            await authoring.UpdateGraphAsync(parent.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""w1"", ""kind"": ""action"", ""type"": ""execute_workflow"", ""name"": ""child"", ""config"": { ""automationId"": """ + foreign.Id + @""" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""w1"" } ]
            }"));
            await authoring.PublishAsync(parent.Id);
            var queued = await authoring.EnqueueRunAsync(parent.Id, "steal", dryRun: false);
            await ownerHost.GetRequiredService<IAutomationRunService>().AdvanceAsync(queued.Id);

            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("not found", run.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await store.CountActiveRunsAsync(foreign.Id));
        }

        [Fact]
        public async Task Wait_until_times_out()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var until = clock.UtcNow.AddSeconds(5).ToString("O");

            var automation = await authoring.CreateDraftAsync(owner, "wait");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""w1"", ""kind"": ""action"", ""type"": ""wait_until"", ""name"": ""untilReady"", ""config"": {
                    ""until"": """ + until + @""",
                    ""condition"": { ""op"": ""equals"", ""left"": ""no"", ""right"": ""yes"" }
                } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""w1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "wait", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            Assert.Equal(AutomationStatuses.Waiting, (await store.GetRunAsync(queued.Id))!.Status);

            clock.UtcNow = clock.UtcNow.AddSeconds(5);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("timed out", run.Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Wait_until_resumes_when_condition_becomes_true()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var until = clock.UtcNow.AddHours(1).ToString("O");

            var automation = await authoring.CreateDraftAsync(owner, "wait-ok");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""w1"", ""kind"": ""action"", ""type"": ""wait_until"", ""name"": ""untilReady"", ""config"": {
                    ""until"": """ + until + @""",
                    ""condition"": { ""op"": ""is_not_empty"", ""left"": ""{{vars.flag}}"" }
                } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""after"", ""config"": { ""name"": ""done"", ""value"": ""1"" } }
              ],
              ""edges"": [
                { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""w1"" },
                { ""id"": ""e2"", ""from"": ""w1"", ""to"": ""a1"" }
              ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "wait", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, run.Status);

            var ctx = JsonNode.Parse(run.ContextJson)!.AsObject();
            (ctx["vars"] as JsonObject)!["flag"] = "yes";
            run.ContextJson = ctx.ToJsonString();
            await store.UpdateRunAsync(run);

            await runs.AdvanceAsync(run.Id);
            run = (await store.GetRunAsync(run.Id))!;
            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            ctx = JsonNode.Parse(run.ContextJson)!.AsObject();
            Assert.Equal("1", ctx["vars"]!["done"]!.GetValue<string>());
        }

        [Fact]
        public async Task Template_creates_draft_with_graph()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var created = await provider.GetRequiredService<IAutomationAuthoringService>()
                .CreateFromTemplateAsync(owner, "manual-flag");
            var version = await provider.GetRequiredService<IAutomationAuthoringService>()
                .GetDraftVersionAsync(created.Id);
            Assert.Equal("Manual flag", created.Name);
            Assert.Contains("set_variable", version.GraphJson, StringComparison.Ordinal);
            Assert.NotNull(AutomationTemplateCatalog.Get("daily-marker"));
        }

        [Fact]
        public async Task Settings_update_persists_limits()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var created = await authoring.CreateDraftAsync(owner, "limits");
            await authoring.UpdateSettingsAsync(created.Id, "Europe/Berlin", 4, 12, 5, 30, 120);
            var loaded = await authoring.GetAsync(created.Id);
            Assert.Equal("Europe/Berlin", loaded.TimeZoneId);
            Assert.Equal(4, loaded.MaxConcurrentRuns);
            Assert.Equal(12, loaded.MaxRunsPerHour);
            Assert.Equal(5, loaded.RetryLimit);
            Assert.Equal(30, loaded.StepTimeoutSeconds);
            Assert.Equal(120, loaded.RunTimeoutSeconds);
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
