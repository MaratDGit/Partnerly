using System.Net;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Automation.Tests.Integrations
{
    public class TelegramIntegrationTests
    {
        private const string Token = "123:ABC";

        private static JsonObject BotPayload(string? defaultChat = null)
        {
            var payload = new JsonObject { ["botToken"] = Token, ["botUsername"] = "partnerly_bot" };
            if (defaultChat != null)
            {
                payload["defaultChatId"] = defaultChat;
            }

            return payload;
        }

        [Fact]
        public async Task Send_message_calls_bot_api_with_interpolated_text()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/sendMessage", "{\"ok\":true,\"result\":{\"message_id\":77}}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendMessage, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["chatId"] = "555",
                ["text"] = "Run {{execution.dryRun}} ok"
            });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var request = Assert.Single(http.To("/sendMessage"));
            Assert.Equal($"https://api.telegram.org/bot{Token}/sendMessage", request.Url);
            var body = JsonNode.Parse(request.Body)!;
            Assert.Equal("555", body["chat_id"]!.ToString());
            Assert.Equal("Run false ok", body["text"]!.ToString());
            Assert.Contains("77", step.OutputJson);
            Assert.DoesNotContain(Token, step.OutputJson ?? string.Empty);
        }

        [Fact]
        public async Task Empty_chat_uses_connection_default_chat()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/sendMessage", "{\"ok\":true,\"result\":{\"message_id\":1}}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload("999"));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendMessage, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["text"] = "hi"
            });

            await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal("999", JsonNode.Parse(http.To("/sendMessage").Single().Body)!["chat_id"]!.ToString());
        }

        [Fact]
        public async Task Dry_run_does_not_send()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload("1"));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendMessage, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["text"] = "hi"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id, dryRun: true);

            Assert.Equal(AutomationStatuses.StepSkipped, step!.Status);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Connection_of_another_user_is_rejected()
        {
            var owner = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http);
            var foreign = await IntegrationTestHost.ConnectAsync(provider, stranger, IntegrationKinds.Telegram, BotPayload("1"));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendMessage, new JsonObject
            {
                ["connectionId"] = foreign.Id.ToString(),
                ["text"] = "steal"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Connection was not found", step.Error);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Api_error_fails_step_and_marks_connection()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/sendMessage", "{\"ok\":false,\"description\":\"Unauthorized\"}", HttpStatusCode.Unauthorized);
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload("1"));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendMessage, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["text"] = "hi"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Unauthorized", step.Error);
            Assert.DoesNotContain(Token, step.Error);
            var reloaded = await IntegrationTestHost.ReloadAsync(provider, connection.Id);
            Assert.False(reloaded.Status.Ok);
        }

        [Fact]
        public async Task Chat_not_found_explains_what_to_do()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/sendMessage",
                "{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: chat not found\"}", HttpStatusCode.BadRequest);
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload("@someone"));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendMessage, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["text"] = "hi"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("chat not found", step.Error);
            Assert.Contains("Recent chats", step.Error);
        }

        [Fact]
        public async Task Send_document_uploads_attachment()
        {
            var owner = Guid.NewGuid();
            var files = new FakeAttachmentService();
            var fileId = files.Add("report.txt", "text/plain", "hello file");
            var http = new FakeHttpHandler().On("/sendDocument", "{\"ok\":true,\"result\":{\"message_id\":5}}");
            await using var provider = IntegrationTestHost.Create(owner, http, attachments: files);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload("42"));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.TelegramSendDocument, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["attachmentId"] = fileId.ToString(),
                ["caption"] = "Monthly"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var request = http.To("/sendDocument").Single();
            Assert.StartsWith("multipart/form-data", request.ContentType);
            Assert.Contains("hello file", request.Body);
            Assert.Contains("report.txt", request.Body);
        }

        [Fact]
        public async Task Poller_skips_history_then_starts_matching_agents()
        {
            var owner = Guid.NewGuid();
            var call = 0;
            var http = new FakeHttpHandler().On("/getUpdates", _ =>
            {
                call++;
                return call == 1
                    ? (HttpStatusCode.OK, "{\"ok\":true,\"result\":[{\"update_id\":10,\"message\":{\"message_id\":1,\"text\":\"old\",\"chat\":{\"id\":5}}}]}")
                    : (HttpStatusCode.OK, "{\"ok\":true,\"result\":[" +
                       "{\"update_id\":11,\"message\":{\"message_id\":2,\"text\":\"Order please\",\"chat\":{\"id\":5,\"type\":\"private\"},\"from\":{\"id\":5,\"first_name\":\"Ann\"}}}," +
                       "{\"update_id\":12,\"message\":{\"message_id\":3,\"text\":\"hello\",\"chat\":{\"id\":5}}}]}");
            });
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, BotPayload());
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.TelegramMessageReceived, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["textContains"] = "order"
            });

            await IntegrationTestHost.PollAsync(provider);
            Assert.Empty(IntegrationTestHost.RunsOf(provider, id));
            Assert.Equal("11", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("updateOffset"));

            await IntegrationTestHost.PollAsync(provider);
            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            var trigger = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal("Order please", trigger["text"]!.ToString());
            Assert.Equal("Ann", trigger["fromName"]!.ToString());
            Assert.Equal("13", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("updateOffset"));
            Assert.Contains("\"offset\":11", http.To("/getUpdates").Last().Body);
        }

        [Fact]
        public async Task Poller_ignores_agents_of_other_users_bound_to_my_bot()
        {
            var owner = Guid.NewGuid();
            var attacker = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/getUpdates",
                "{\"ok\":true,\"result\":[{\"update_id\":1,\"message\":{\"message_id\":1,\"text\":\"x\",\"chat\":{\"id\":5}}}]}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var payload = BotPayload();
            payload["updateOffset"] = "1";
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Telegram, payload);
            var foreignAgent = await IntegrationTestHost.PublishTriggerAsync(provider, attacker, IntegrationTypeCodes.TelegramMessageReceived, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString()
            });

            var polled = await IntegrationTestHost.PollAsync(provider);

            Assert.Equal(0, polled);
            Assert.Empty(http.Requests);
            Assert.Empty(IntegrationTestHost.RunsOf(provider, foreignAgent));
        }
    }
}
