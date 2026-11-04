using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Google;
using Partnerly.Integrations.Google.Gmail;
using Partnerly.Integrations.Google.Sheets;

namespace Partnerly.Automation.Tests.Integrations
{
    public class GoogleIntegrationTests
    {
        private static readonly string AllScopes = string.Join(" ",
            GoogleOAuthService.ScopeSheets, GoogleOAuthService.ScopeGmailSend, GoogleOAuthService.ScopeGmailRead, GoogleOAuthService.ScopeDriveFile);

        private static JsonObject GooglePayload(FakeClock clock, string? scope = null, bool expired = false) => new()
        {
            ["email"] = "me@example.com",
            ["accessToken"] = "access-1",
            ["refreshToken"] = "refresh-1",
            ["expiresAt"] = clock.UtcNow.AddMinutes(expired ? -5 : 30).ToString("O"),
            ["scope"] = scope ?? AllScopes
        };

        [Fact]
        public async Task Sheets_append_refreshes_expired_token_and_keeps_it()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler()
                .On("oauth2.googleapis.com/token", "{\"access_token\":\"access-2\",\"expires_in\":3600}")
                .On(":append", "{\"updates\":{\"updatedRange\":\"Leads!A5:C5\"}}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock, expired: true));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.SheetsAppendRow, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["spreadsheetId"] = "https://docs.google.com/spreadsheets/d/abc_123-XY/edit#gid=0",
                ["sheet"] = "Leads",
                ["values"] = "Ann\n{{execution.dryRun}}\nnew"
            });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Contains("refresh_token=refresh-1", http.To("/token").Single().Body);
            var append = http.To(":append").Single();
            Assert.Equal("Bearer access-2", append.Authorization);
            Assert.Contains("/spreadsheets/abc_123-XY/values/", append.Url);
            Assert.Equal("[[\"Ann\",\"false\",\"new\"]]", JsonNode.Parse(append.Body)!["values"]!.ToJsonString());
            Assert.Contains("Leads!A5:C5", step!.OutputJson);
            Assert.Equal("access-2", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("accessToken"));
        }

        [Fact]
        public async Task Find_rows_maps_header_to_fields_and_runs_in_test_mode()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/values/", "{\"values\":[[\"Name\",\"Email\"],[\"Ann\",\"ann@x.am\"],[\"Bob\",\"bob@x.am\"]]}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.SheetsFindRows, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["spreadsheetId"] = "sheet1",
                ["column"] = "B",
                ["value"] = "BOB@x.am"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id, dryRun: true);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var output = JsonNode.Parse(step.OutputJson!)!["result"] ?? JsonNode.Parse(step.OutputJson!)!;
            Assert.Equal(1, output["count"]!.GetValue<int>());
            Assert.Equal("Bob", output["first"]!["fields"]!["Name"]!.ToString());
            Assert.Equal(3, output["first"]!["rowNumber"]!.GetValue<int>());
        }

        [Fact]
        public async Task Missing_scope_fails_without_calling_google()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google,
                GooglePayload(clock, scope: GoogleOAuthService.ScopeGmailSend));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.SheetsAppendRow, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["spreadsheetId"] = "s",
                ["values"] = "x"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("no access", step.Error);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Revoked_refresh_token_asks_to_reconnect()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/token", "{\"error\":\"invalid_grant\",\"error_description\":\"Token has been expired or revoked.\"}", HttpStatusCode.BadRequest);
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock, expired: true));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GmailSend, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "a@b.am"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Reconnect Google", step.Error);
            Assert.False((await IntegrationTestHost.ReloadAsync(provider, connection.Id)).Status.Ok);
        }

        [Fact]
        public async Task Gmail_send_posts_utf8_mime_message()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/messages/send", "{\"id\":\"m1\",\"threadId\":\"t1\"}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.GmailSend, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "client@example.com",
                ["subject"] = "Привет",
                ["body"] = "<b>Բարև</b>"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var raw = JsonNode.Parse(http.To("/messages/send").Single().Body)!["raw"]!.ToString();
            var mime = Encoding.UTF8.GetString(Convert.FromBase64String(Pad(raw.Replace('-', '+').Replace('_', '/'))));
            Assert.Contains("To: client@example.com", mime);
            Assert.Contains("Subject: =?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Привет")) + "?=", mime);
            Assert.Contains("Content-Type: text/html; charset=UTF-8", mime);
            var body = mime[(mime.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..];
            Assert.Equal("<b>Բարև</b>", Encoding.UTF8.GetString(Convert.FromBase64String(body.Replace("\r\n", ""))));
        }

        [Fact]
        public void Mime_headers_drop_line_breaks_from_variables()
        {
            var mime = GmailApi.BuildMime("a@b.am\r\nBcc: evil@x.am", "Hi", "body", html: false);

            Assert.DoesNotContain("\r\nBcc:", mime);
            Assert.Contains("To: a@b.am  Bcc: evil@x.am", mime);
        }

        [Fact]
        public async Task Gmail_poller_starts_agents_for_new_inbox_mail_matching_filters()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler()
                .On("/profile", "{\"historyId\":\"100\"}")
                .On("/history?", "{\"historyId\":\"105\",\"history\":[{\"messagesAdded\":[{\"message\":{\"id\":\"a\"}},{\"message\":{\"id\":\"b\"}}]}]}")
                .On("/messages/a?", "{\"id\":\"a\",\"snippet\":\"Invoice attached\",\"payload\":{\"headers\":[{\"name\":\"From\",\"value\":\"Billing <bill@shop.am>\"},{\"name\":\"Subject\",\"value\":\"Invoice 12\"}]}}")
                .On("/messages/b?", "{\"id\":\"b\",\"payload\":{\"headers\":[{\"name\":\"From\",\"value\":\"friend@x.am\"},{\"name\":\"Subject\",\"value\":\"Hi\"}]}}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.GmailMessageReceived, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["subjectContains"] = "invoice"
            });

            await IntegrationTestHost.PollAsync(provider);
            Assert.Equal("100", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("gmailHistoryId"));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, id));

            await IntegrationTestHost.PollAsync(provider);
            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            Assert.Equal("Invoice 12", IntegrationTestHost.TriggerPayload(run)["subject"]!.ToString());
            Assert.Equal("105", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("gmailHistoryId"));
        }

        [Fact]
        public async Task Sheets_poller_reports_only_rows_added_after_first_pass()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var rows = "[[\"Name\",\"Email\"],[\"Ann\",\"a@x.am\"]]";
            var http = new FakeHttpHandler().On("/values/", _ => (HttpStatusCode.OK, "{\"values\":" + rows + "}"));
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, GooglePayload(clock));
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.SheetsRowAdded, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["spreadsheetId"] = "book",
                ["sheet"] = "Leads"
            });

            await IntegrationTestHost.PollAsync(provider);
            Assert.Empty(IntegrationTestHost.RunsOf(provider, id));

            rows = "[[\"Name\",\"Email\"],[\"Ann\",\"a@x.am\"],[\"Bob\",\"b@x.am\"]]";
            await IntegrationTestHost.PollAsync(provider);

            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            var trigger = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal(3, trigger["rowNumber"]!.GetValue<int>());
            Assert.Equal("b@x.am", trigger["fields"]!["Email"]!.ToString());
        }

        [Theory]
        [InlineData("https://docs.google.com/spreadsheets/d/1AbC-_9/edit", "1AbC-_9")]
        [InlineData("  plainId ", "plainId")]
        public void Spreadsheet_id_accepts_url_or_id(string input, string expected)
        {
            Assert.Equal(expected, GoogleSheetsApi.SpreadsheetId(input));
        }

        [Theory]
        [InlineData("A", 0)]
        [InlineData("c", 2)]
        [InlineData("AA", 26)]
        [InlineData("3", 2)]
        [InlineData("A1", -1)]
        public void Column_index_from_letter_or_number(string column, int expected)
        {
            Assert.Equal(expected, GoogleSheetsApi.ColumnIndex(column));
        }

        [Fact]
        public async Task Consent_url_requests_offline_access_and_chosen_scopes()
        {
            await using var provider = IntegrationTestHost.Create(Guid.NewGuid(), new FakeHttpHandler());
            var oauth = provider.GetRequiredService<GoogleOAuthService>();

            var url = oauth.BuildConsentUrl("http://localhost:5000/Integrations/Google/Callback", "st", GoogleOAuthService.ScopesFor(true, false, true, false));

            Assert.StartsWith(GoogleOAuthService.AuthorizeUrl, url);
            Assert.Contains("access_type=offline", url);
            Assert.Contains("client_id=test-client", url);
            Assert.Contains(Uri.EscapeDataString(GoogleOAuthService.ScopeSheets), url);
            Assert.Contains(Uri.EscapeDataString(GoogleOAuthService.ScopeDriveFile), url);
            Assert.DoesNotContain(Uri.EscapeDataString(GoogleOAuthService.ScopeGmailRead), url);
            Assert.DoesNotContain("test-secret", url);
        }

        private static string Pad(string base64) => base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }
}
