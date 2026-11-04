using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Google;
using Partnerly.Integrations.WhatsApp;

namespace Partnerly.Automation.Tests.Integrations
{
    public class DriveAndWhatsAppTests
    {
        private static JsonObject GooglePayload(FakeClock clock) => new()
        {
            ["accessToken"] = "access-1",
            ["refreshToken"] = "refresh-1",
            ["expiresAt"] = clock.UtcNow.AddMinutes(30).ToString("O"),
            ["scope"] = GoogleOAuthService.ScopeDriveFile
        };

        private static JsonObject WhatsAppPayload() => new()
        {
            ["accessToken"] = "wa-token",
            ["phoneNumberId"] = "1098",
            ["appSecret"] = "app-secret",
            ["verifyToken"] = "verify-me"
        };

        [Fact]
        public async Task Drive_upload_sends_metadata_and_file_content()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var files = new FakeAttachmentService();
            var fileId = files.Add("contract.pdf", "application/pdf", "PDF-BYTES");
            var http = new FakeHttpHandler().On("upload/drive/v3/files", "{\"id\":\"f1\",\"name\":\"Contract.pdf\",\"webViewLink\":\"https://drive/f1\"}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock, files);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.DriveUploadFile, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["attachmentId"] = fileId.ToString(),
                ["fileName"] = "Contract.pdf",
                ["folderId"] = "folder9"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var request = http.To("upload/drive").Single();
            Assert.Contains("uploadType=multipart", request.Url);
            Assert.StartsWith("multipart/related", request.ContentType);
            Assert.Contains("\"parents\":[\"folder9\"]", request.Body);
            Assert.Contains("PDF-BYTES", request.Body);
            Assert.Contains("https://drive/f1", step.OutputJson);
        }

        [Fact]
        public async Task Drive_find_escapes_quotes_in_query()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/drive/v3/files?", "{\"files\":[{\"id\":\"1\",\"name\":\"O'Hara.docx\"}]}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.DriveFindFiles, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["nameContains"] = "O'Hara"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var url = Uri.UnescapeDataString(http.Requests.Single().Url);
            Assert.Contains("name contains 'O\\'Hara'", url);
            Assert.Contains("trashed = false", url);
        }

        [Fact]
        public async Task Drive_poller_reports_new_files_in_watched_folder_only()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var created = clock.UtcNow.AddMinutes(1).ToString("O");
            var old = clock.UtcNow.AddDays(-3).ToString("O");
            var http = new FakeHttpHandler()
                .On("/changes/startPageToken", "{\"startPageToken\":\"p1\"}")
                .On("/changes?", "{\"newStartPageToken\":\"p2\",\"changes\":[" +
                    "{\"file\":{\"id\":\"new\",\"name\":\"scan.png\",\"parents\":[\"inbox\"],\"createdTime\":\"" + created + "\"}}," +
                    "{\"file\":{\"id\":\"edited\",\"name\":\"old.png\",\"parents\":[\"inbox\"],\"createdTime\":\"" + old + "\"}}," +
                    "{\"file\":{\"id\":\"elsewhere\",\"name\":\"x.png\",\"parents\":[\"other\"],\"createdTime\":\"" + created + "\"}}]}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.DriveFileAdded, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["folderId"] = "inbox"
            });

            await IntegrationTestHost.PollAsync(provider);
            await IntegrationTestHost.PollAsync(provider);

            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            Assert.Equal("scan.png", IntegrationTestHost.TriggerPayload(run)["name"]!.ToString());
            Assert.Equal("p2", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("drivePageToken"));
        }

        [Fact]
        public async Task WhatsApp_text_posts_graph_message_with_normalized_phone()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/1098/messages", "{\"contacts\":[{\"wa_id\":\"37491234567\"}],\"messages\":[{\"id\":\"wamid.1\"}]}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.WhatsApp, WhatsAppPayload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.WhatsAppSendText, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "+374 91 23-45-67",
                ["text"] = "Your order is ready"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var request = http.Requests.Single();
            Assert.Equal("Bearer wa-token", request.Authorization);
            Assert.StartsWith(WhatsAppClient.BaseUrl, request.Url);
            var body = JsonNode.Parse(request.Body)!;
            Assert.Equal("37491234567", body["to"]!.ToString());
            Assert.Equal("Your order is ready", body["text"]!["body"]!.ToString());
            Assert.Contains("wamid.1", step.OutputJson);
        }

        [Fact]
        public async Task WhatsApp_template_sends_body_parameters()
        {
            var owner = Guid.NewGuid();
            var http = new FakeHttpHandler().On("/messages", "{\"messages\":[{\"id\":\"wamid.2\"}]}");
            await using var provider = IntegrationTestHost.Create(owner, http);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.WhatsApp, WhatsAppPayload());
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.WhatsAppSendTemplate, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "37400000000",
                ["template"] = "order_ready",
                ["language"] = "ru",
                ["parameters"] = "Ann\n\n42"
            });

            await IntegrationTestHost.RunAsync(provider, id);

            var template = JsonNode.Parse(http.Requests.Single().Body)!["template"]!;
            Assert.Equal("order_ready", template["name"]!.ToString());
            Assert.Equal("ru", template["language"]!["code"]!.ToString());
            var parameters = template["components"]![0]!["parameters"]!.AsArray();
            Assert.Equal(new[] { "Ann", "42" }, parameters.Select(p => p!["text"]!.ToString()));
        }

        [Fact]
        public void Signature_check_accepts_only_matching_hmac()
        {
            const string body = "{\"entry\":[]}";
            var signature = WhatsAppClient.Sign("app-secret", body);

            Assert.True(WhatsAppClient.VerifySignature("app-secret", body, signature));
            Assert.False(WhatsAppClient.VerifySignature("other-secret", body, signature));
            Assert.False(WhatsAppClient.VerifySignature("app-secret", body + " ", signature));
            Assert.False(WhatsAppClient.VerifySignature("app-secret", body, null));
            Assert.False(WhatsAppClient.VerifySignature("app-secret", body, "md5=abc"));
        }

        [Fact]
        public async Task Webhook_verify_returns_challenge_only_for_right_token()
        {
            var owner = Guid.NewGuid();
            await using var provider = IntegrationTestHost.Create(owner, new FakeHttpHandler());
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.WhatsApp, WhatsAppPayload());
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<WhatsAppWebhookService>();

            Assert.Equal("c123", await service.VerifyAsync(connection.Id, "subscribe", "verify-me", "c123"));
            Assert.Null(await service.VerifyAsync(connection.Id, "subscribe", "wrong", "c123"));
            Assert.Null(await service.VerifyAsync(Guid.NewGuid(), "subscribe", "verify-me", "c123"));
        }

        [Fact]
        public async Task Webhook_starts_agents_only_with_valid_signature()
        {
            var owner = Guid.NewGuid();
            await using var provider = IntegrationTestHost.Create(owner, new FakeHttpHandler());
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.WhatsApp, WhatsAppPayload());
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.WhatsAppMessageReceived, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString()
            });
            const string body = "{\"entry\":[{\"changes\":[{\"value\":{\"contacts\":[{\"profile\":{\"name\":\"Ann\"}}]," +
                                "\"messages\":[{\"id\":\"wamid.9\",\"from\":\"37491000000\",\"type\":\"text\",\"text\":{\"body\":\"Where is my order?\"}}]}}]}]}";
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<WhatsAppWebhookService>();

            Assert.Equal(WhatsAppWebhookResult.Unauthorized, await service.IngestAsync(connection.Id, body, WhatsAppClient.Sign("wrong", body)));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, id));

            Assert.Equal(WhatsAppWebhookResult.Accepted, await service.IngestAsync(connection.Id, body, WhatsAppClient.Sign("app-secret", body)));
            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            var trigger = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal("Where is my order?", trigger["text"]!.ToString());
            Assert.Equal("Ann", trigger["name"]!.ToString());
            Assert.Equal("37491000000", trigger["from"]!.ToString());
        }

        [Fact]
        public async Task Webhook_rejects_oversized_body()
        {
            var owner = Guid.NewGuid();
            await using var provider = IntegrationTestHost.Create(owner, new FakeHttpHandler());
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.WhatsApp, WhatsAppPayload());
            using var scope = provider.CreateScope();
            var body = new string('x', WhatsAppWebhookService.MaxBodyBytes + 1);

            var result = await scope.ServiceProvider.GetRequiredService<WhatsAppWebhookService>()
                .IngestAsync(connection.Id, body, WhatsAppClient.Sign("app-secret", body));

            Assert.Equal(WhatsAppWebhookResult.TooLarge, result);
        }
    }
}
