using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Infrastructure.Interfaces;
using System.Text.Json.Nodes;

namespace Partnerly.Automation.Tests
{
    public class WorkflowExecutorTests
    {
        [Fact]
        public async Task Set_variable_then_branch_true()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "flag");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""setReady"", ""config"": { ""name"": ""ready"", ""value"": ""yes"" } },
                { ""id"": ""b1"", ""kind"": ""action"", ""type"": ""branch"", ""name"": ""ifReady"", ""config"": { ""condition"": { ""op"": ""equals"", ""left"": ""{{vars.ready}}"", ""right"": ""yes"" } } },
                { ""id"": ""ok"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""okPath"", ""config"": { ""name"": ""path"", ""value"": ""true"" } },
                { ""id"": ""no"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""noPath"", ""config"": { ""name"": ""path"", ""value"": ""false"" } }
              ],
              ""edges"": [
                { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" },
                { ""id"": ""e2"", ""from"": ""a1"", ""to"": ""b1"" },
                { ""id"": ""e3"", ""from"": ""b1"", ""to"": ""ok"", ""port"": ""true"" },
                { ""id"": ""e4"", ""from"": ""b1"", ""to"": ""no"", ""port"": ""false"" }
              ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var run = await RunNow(provider, automation.Id, "test", dryRun: false);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var ctx = JsonNode.Parse(run.ContextJson)!.AsObject();
            Assert.Equal("true", ctx["vars"]!["path"]!.GetValue<string>());
            var steps = await store.GetStepsAsync(run.Id);
            Assert.DoesNotContain(steps, step => step.NodeId == "no");
        }

        [Fact]
        public async Task Dry_run_skips_email()
        {
            var owner = Guid.NewGuid();
            var email = new RecordingEmailSender();
            await using var provider = AutomationTestHost.Create(
                owner,
                configure: services => services.AddSingleton<IEmailSender>(email));
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "mail");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""m1"", ""kind"": ""action"", ""type"": ""send_email"", ""name"": ""mail"", ""config"": { ""to"": ""a@b.c"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""m1"" } ]
            }"));
            var run = await RunNow(provider, automation.Id, "preview", dryRun: true);
            var steps = await store.GetStepsAsync(run.Id);
            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Equal(AutomationStatuses.StepSkipped, steps.Single().Status);
            Assert.Contains("dryRun", steps.Single().OutputJson);
            Assert.Equal(0, email.Calls);
        }

        [Fact]
        public async Task Live_email_uses_sender()
        {
            var owner = Guid.NewGuid();
            var email = new RecordingEmailSender();
            await using var provider = AutomationTestHost.Create(
                owner,
                configure: services => services.AddSingleton<IEmailSender>(email));
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();

            var automation = await authoring.CreateDraftAsync(owner, "mail-live");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""m1"", ""kind"": ""action"", ""type"": ""send_email"", ""name"": ""mail"", ""config"": { ""to"": ""a@b.c"", ""subject"": ""hi"", ""body"": ""x"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""m1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var run = await RunNow(provider, automation.Id, "send", dryRun: false);
            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Equal(1, email.Calls);
        }

        [Fact]
        public async Task Live_http_request_to_private_url_fails()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "ssrf");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""h1"", ""kind"": ""action"", ""type"": ""http_request"", ""name"": ""call"", ""config"": { ""method"": ""GET"", ""url"": ""http://127.0.0.1/secret"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""h1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var run = await RunNow(provider, automation.Id, "ssrf", dryRun: false);
            var steps = await store.GetStepsAsync(run.Id);
            Assert.Equal(AutomationStatuses.Failed, run.Status);
            Assert.Contains("not allowed", run.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(AutomationStatuses.StepFailed, steps.Single().Status);
        }

        [Fact]
        public async Task Dry_run_skips_http_request()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "http");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""h1"", ""kind"": ""action"", ""type"": ""http_request"", ""name"": ""call"", ""config"": { ""method"": ""GET"", ""url"": ""https://example.com/hook"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""h1"" } ]
            }"));
            var queued = await authoring.EnqueueRunAsync(automation.Id, "preview", dryRun: true);
            await provider.GetRequiredService<IAutomationRunService>().AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            var steps = await store.GetStepsAsync(run.Id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Equal(AutomationStatuses.StepSkipped, steps.Single().Status);
            Assert.Contains("dryRun", steps.Single().OutputJson);
        }

        [Fact]
        public async Task Delay_waits_then_resumes()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "wait");
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
            var queued = await authoring.EnqueueRunAsync(automation.Id, "delay", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, run.Status);

            clock.UtcNow = clock.UtcNow.AddSeconds(5);
            await runs.AdvanceAsync(run.Id);
            run = (await store.GetRunAsync(run.Id))!;
            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var ctx = JsonNode.Parse(run.ContextJson)!.AsObject();
            Assert.Equal("1", ctx["vars"]!["done"]!.GetValue<string>());
        }

        [Fact]
        public async Task Delay_amount_minutes_waits_that_long()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            await using var provider = AutomationTestHost.Create(owner, clock: clock);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "wait-minutes");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""d1"", ""kind"": ""action"", ""type"": ""delay"", ""name"": ""pause"", ""config"": { ""amount"": 5, ""unit"": ""minutes"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""after"", ""config"": { ""name"": ""done"", ""value"": ""1"" } }
              ],
              ""edges"": [
                { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""d1"" },
                { ""id"": ""e2"", ""from"": ""d1"", ""to"": ""a1"" }
              ]
            }"));
            await authoring.PublishAsync(automation.Id);
            var queued = await authoring.EnqueueRunAsync(automation.Id, "delay", dryRun: false);
            await runs.AdvanceAsync(queued.Id);
            var run = (await store.GetRunAsync(queued.Id))!;
            Assert.Equal(AutomationStatuses.Waiting, run.Status);
            Assert.DoesNotContain(
                await store.GetPendingDueAsync(clock.UtcNow.AddMinutes(4), 20),
                item => item.Kind == AutomationStatuses.WakeupDelay && item.RunId == run.Id);

            clock.UtcNow = clock.UtcNow.AddMinutes(5);
            await runs.AdvanceAsync(run.Id);
            run = (await store.GetRunAsync(run.Id))!;
            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var ctx = JsonNode.Parse(run.ContextJson)!.AsObject();
            Assert.Equal("1", ctx["vars"]!["done"]!.GetValue<string>());
        }

        [Fact]
        public async Task Other_user_cannot_open_automation()
        {
            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            var store = new InMemoryAutomationStore();
            await using var ownerHost = AutomationTestHost.Create(owner, store: store);
            var created = await ownerHost.GetRequiredService<IAutomationAuthoringService>()
                .CreateDraftAsync(owner, "secret");

            await using var otherHost = AutomationTestHost.Create(other, store: store);
            var otherAuthoring = otherHost.GetRequiredService<IAutomationAuthoringService>();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => otherAuthoring.ValidateAsync(created.Id));
        }

        private static async Task<AutomationRun> RunNow(
            IServiceProvider provider,
            Guid automationId,
            string reason,
            bool dryRun)
        {
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var runs = provider.GetRequiredService<IAutomationRunService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var queued = await authoring.EnqueueRunAsync(automationId, reason, dryRun);
            await runs.AdvanceAsync(queued.Id);
            return (await store.GetRunAsync(queued.Id))!;
        }
    }
}
