using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Tests
{
    public class WebhookLifecycleTests
    {
        private const string OneHook = @"{
          ""schemaVersion"": 1,
          ""nodes"": [
            { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hookIn"", ""config"": {} },
            { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
          ],
          ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
        }";

        private const string TwoHooks = @"{
          ""schemaVersion"": 1,
          ""nodes"": [
            { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hookA"", ""config"": {} },
            { ""id"": ""t2"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hookB"", ""config"": {} },
            { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
          ],
          ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" }, { ""id"": ""e2"", ""from"": ""t2"", ""to"": ""a1"" } ]
        }";

        [Fact]
        public async Task Republish_keeps_url_and_releases_id_of_previous_version()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "hook");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(OneHook));
            await authoring.PublishAsync(automation.Id);
            var first = await store.GetDefinitionAsync(automation.Id);
            var firstVersion = first!.PublishedVersionId!.Value;
            var firstTrigger = (await store.GetTriggersAsync(firstVersion)).Single();

            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(OneHook));
            await authoring.PublishAsync(automation.Id);
            var second = await store.GetDefinitionAsync(automation.Id);
            var secondTrigger = (await store.GetTriggersAsync(second!.PublishedVersionId!.Value)).Single();

            Assert.NotEqual(firstVersion, second.PublishedVersionId!.Value);
            Assert.Equal(firstTrigger.WebhookPublicId, secondTrigger.WebhookPublicId);
            Assert.Equal(firstTrigger.WebhookSecretHash, secondTrigger.WebhookSecretHash);
            // The unique index allows an id on one version only.
            Assert.All(await store.GetTriggersAsync(firstVersion), item => Assert.Null(item.WebhookPublicId));
        }

        [Fact]
        public async Task Two_webhook_nodes_get_different_urls_that_survive_republish()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();

            var automation = await authoring.CreateDraftAsync(owner, "hooks");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(TwoHooks));
            await authoring.PublishAsync(automation.Id);
            var first = await store.GetDefinitionAsync(automation.Id);
            var before = (await store.GetTriggersAsync(first!.PublishedVersionId!.Value)).Select(item => item.WebhookPublicId).ToList();

            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(TwoHooks));
            await authoring.PublishAsync(automation.Id);
            var second = await store.GetDefinitionAsync(automation.Id);
            var after = (await store.GetTriggersAsync(second!.PublishedVersionId!.Value)).Select(item => item.WebhookPublicId).ToList();

            Assert.Equal(2, before.Distinct().Count());
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task Busy_agent_reports_busy_and_run_shows_open_wakeup()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var ingest = provider.GetRequiredService<AutomationWebhookIngestService>();
            var secrets = provider.GetRequiredService<ISecretProtector>();

            var automation = await authoring.CreateDraftAsync(owner, "busy");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(OneHook));
            await authoring.PublishAsync(automation.Id);
            var definition = await store.GetDefinitionAsync(automation.Id);
            definition!.MaxConcurrentRuns = 1;
            await store.UpdateDefinitionAsync(definition);
            var trigger = (await store.GetTriggersAsync(definition.PublishedVersionId!.Value)).Single();
            var secret = secrets.Unprotect(trigger.WebhookSecretHash!);
            var signature = WebhookSignature.Compute(secret, "{}");

            Assert.Equal(WebhookIngestResult.Accepted, await ingest.IngestAsync(trigger.WebhookPublicId!, "{}", signature));
            Assert.Equal(WebhookIngestResult.Busy, await ingest.IngestAsync(trigger.WebhookPublicId!, "{}", signature));
            Assert.Equal(1, await store.CountActiveRunsAsync(automation.Id));

            var open = await store.GetOpenWakeupsAsync(automation.Id);
            Assert.Contains(open, item => item.Kind == AutomationStatuses.WakeupStart && item.RunId != null);
        }
    }
}
