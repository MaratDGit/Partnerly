using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Integration;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Registry;
using Partnerly.Integrations.Common;

namespace Partnerly.Automation.Tests.Integrations
{
    public class IntegrationCoreTests
    {
        [Fact]
        public async Task Validator_accepts_registered_integration_types_and_rejects_unknown()
        {
            await using var provider = IntegrationTestHost.Create(Guid.NewGuid(), new FakeHttpHandler());
            var validator = provider.GetRequiredService<GraphValidator>();
            var graph = WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""telegram.message_received"", ""name"": ""tg"", ""config"": { ""connectionId"": ""9d3b6d4e-5c7a-4b8e-9f10-2a3b4c5d6e7f"" } },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""sheets.append_row"", ""name"": ""row"", ""config"": { ""connectionId"": ""{{vars.sheet}}"" } },
                { ""id"": ""a2"", ""kind"": ""action"", ""type"": ""sheets.nope"", ""name"": ""bad"", ""config"": {} }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" }, { ""id"": ""e2"", ""from"": ""a1"", ""to"": ""a2"" } ]
            }");

            var errors = validator.Validate(graph);

            var error = Assert.Single(errors);
            Assert.Contains("sheets.nope", error);
        }

        [Fact]
        public async Task Catalog_describes_integration_nodes_with_group_and_connection_kind()
        {
            await using var provider = IntegrationTestHost.Create(Guid.NewGuid(), new FakeHttpHandler());
            var actions = provider.GetRequiredService<ActionRegistry>().DescribeAll(provider);
            var triggers = provider.GetRequiredService<TriggerRegistry>().DescribeAll(provider);

            var send = Assert.Single(actions, item => item.Type == IntegrationTypeCodes.WhatsAppSendText);
            Assert.Equal("WhatsApp", send.Group);
            Assert.Equal(IntegrationKinds.WhatsApp, send.ConnectionKind);
            Assert.True(send.HasSideEffects);
            Assert.False(actions.Single(item => item.Type == IntegrationTypeCodes.SheetsFindRows).HasSideEffects);
            Assert.Equal(57, actions.Count(item => item.Group != null));
            Assert.Equal(12, triggers.Count(item => item.Group != null));
            Assert.Contains(actions, item => item.Type == AutomationTypeCodes.SendEmail && item.Group == null);
        }

        [Fact]
        public async Task Connection_payload_is_encrypted_at_rest_and_scoped_to_owner()
        {
            var owner = Guid.NewGuid();
            var other = Guid.NewGuid();
            await using var provider = IntegrationTestHost.Create(owner, new FakeHttpHandler());
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, new JsonObject { ["botToken"] = "secret-token-value" });
            var store = provider.GetRequiredService<IAutomationStore>();
            var raw = await store.GetConnectionAsync(connection.Id);
            Assert.DoesNotContain("secret-token-value", raw!.EncryptedPayload);

            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
            Assert.Null(await service.GetForOwnerAsync(connection.Id, other, IntegrationKinds.Telegram));
            Assert.Null(await service.GetForOwnerAsync(connection.Id, owner, IntegrationKinds.Google));
            Assert.Equal("secret-token-value", (await service.GetForOwnerAsync(connection.Id, owner, IntegrationKinds.Telegram))!.GetString("botToken"));
            Assert.Empty(await service.ListAsync(other));
            Assert.Single(await service.ListAsync(owner, IntegrationKinds.Telegram));

            Assert.False(await service.DeleteAsync(connection.Id, other, canManage: false));
            Assert.True(await service.DeleteAsync(connection.Id, owner, canManage: false));
            Assert.Null(await service.GetAsync(connection.Id));
        }

        [Fact]
        public async Task Http_connections_are_not_listed_as_integrations()
        {
            var owner = Guid.NewGuid();
            await using var provider = IntegrationTestHost.Create(owner, new FakeHttpHandler());
            var store = provider.GetRequiredService<IAutomationStore>();
            await store.AddConnectionAsync(new Partnerly.Automation.Persistence.Entities.AutomationConnection
            {
                Id = Guid.NewGuid(),
                OwnerUserId = owner,
                Name = "api",
                Kind = "http",
                EncryptedPayload = "{}"
            });

            using var scope = provider.CreateScope();
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>().ListAsync(owner));
        }

        [Fact]
        public async Task Dispatcher_starts_only_owner_agents_bound_to_that_connection()
        {
            var owner = Guid.NewGuid();
            await using var provider = IntegrationTestHost.Create(owner, new FakeHttpHandler());
            var mine = Guid.NewGuid();
            var otherConnection = Guid.NewGuid();
            var bound = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.WhatsAppMessageReceived,
                new JsonObject { ["connectionId"] = mine.ToString() });
            var unbound = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.WhatsAppMessageReceived,
                new JsonObject { ["connectionId"] = otherConnection.ToString() });
            var stranger = await IntegrationTestHost.PublishTriggerAsync(provider, Guid.NewGuid(), IntegrationTypeCodes.WhatsAppMessageReceived,
                new JsonObject { ["connectionId"] = mine.ToString() });

            await provider.GetRequiredService<AutomationTriggerDispatcher>().EnqueueIntegrationAsync(
                IntegrationTypeCodes.WhatsAppMessageReceived, owner, mine, new JsonObject { ["text"] = "hi" });

            Assert.Single(IntegrationTestHost.RunsOf(provider, bound));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, unbound));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, stranger));
        }

        [Fact]
        public async Task Publish_refuses_integration_step_without_connection()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GmailSend,
                    new JsonObject { ["to"] = "a@b.c" }));

            Assert.Contains("needs a google connection", error.Message);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Dry_run_of_draft_without_connection_fails_clearly()
        {
            // Test runs use the draft, which is not validated: the step itself explains what is missing.
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);
            var authoring = provider.GetRequiredService<IAutomationAuthoringService>();
            var draft = await authoring.CreateDraftAsync(owner, "draft");
            await authoring.UpdateGraphAsync(draft.Id, WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""sheets.find_rows"", ""name"": ""step"", ""config"": {} }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }"));

            var (_, step) = await IntegrationTestHost.RunAsync(provider, draft.Id, dryRun: true);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Choose a connection", step.Error);
            Assert.Empty(http.Requests);
        }
    }
}
