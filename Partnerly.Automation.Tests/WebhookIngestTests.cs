using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Integration;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Security;

namespace Partnerly.Automation.Tests
{
    public class WebhookIngestTests
    {
        [Fact]
        public async Task Valid_signature_enqueues_run()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var ingest = provider.GetRequiredService<AutomationWebhookIngestService>();
            var secrets = provider.GetRequiredService<ISecretProtector>();

            var automation = await authoring.CreateDraftAsync(owner, "hook");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hookIn"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            automation = await store.GetDefinitionAsync(automation.Id);
            var trigger = (await store.GetTriggersAsync(automation!.PublishedVersionId!.Value)).Single();
            var secret = secrets.Unprotect(trigger.WebhookSecretHash!);
            var body = "{\"ping\":true}";
            var signature = WebhookSignature.Compute(secret, body);

            var result = await ingest.IngestAsync(trigger.WebhookPublicId!, body, signature);

            Assert.Equal(WebhookIngestResult.Accepted, result);
            Assert.Equal(1, await store.CountActiveRunsAsync(automation.Id));
        }

        [Fact]
        public async Task Unknown_public_id_is_not_found()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var ingest = provider.GetRequiredService<AutomationWebhookIngestService>();
            var result = await ingest.IngestAsync("missing", "{}", "deadbeef");
            Assert.Equal(WebhookIngestResult.NotFound, result);
        }

        [Fact]
        public async Task Bad_signature_is_unauthorized()
        {
            var owner = Guid.NewGuid();
            await using var provider = AutomationTestHost.Create(owner);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var store = provider.GetRequiredService<IAutomationStore>();
            var ingest = provider.GetRequiredService<AutomationWebhookIngestService>();

            var automation = await authoring.CreateDraftAsync(owner, "hook");
            await authoring.UpdateGraphAsync(automation.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""webhook"", ""name"": ""hookIn"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""mark"", ""config"": { ""name"": ""ok"", ""value"": ""1"" } }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));
            await authoring.PublishAsync(automation.Id);
            automation = await store.GetDefinitionAsync(automation.Id);
            var trigger = (await store.GetTriggersAsync(automation!.PublishedVersionId!.Value)).Single();

            var result = await ingest.IngestAsync(trigger.WebhookPublicId!, "{}", "deadbeef");
            Assert.Equal(WebhookIngestResult.Unauthorized, result);
            Assert.Equal(0, await store.CountActiveRunsAsync(automation.Id));
        }
    }
}
