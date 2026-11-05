using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Google;
using Partnerly.Integrations.Google.Drive;
using Partnerly.Integrations.Google.Gmail;
using Partnerly.Integrations.Google.Sheets;

namespace Partnerly.Automation.Tests.Integrations
{
    // Gmail / Sheets / Drive actions added after the first version (reply, labels, cells, sharing...).
    public class GoogleMoreActionsTests
    {
        private static readonly string Scopes = string.Join(" ",
            GoogleOAuthService.ScopeSheets, GoogleOAuthService.ScopeGmailSend, GoogleOAuthService.ScopeGmailRead,
            GoogleOAuthService.ScopeGmailModify, GoogleOAuthService.ScopeDriveFile);

        private static JsonObject Payload(FakeClock clock, string? scope = null) => new()
        {
            ["email"] = "me@example.com",
            ["accessToken"] = "access-1",
            ["refreshToken"] = "refresh-1",
            ["expiresAt"] = clock.UtcNow.AddMinutes(30).ToString("O"),
            ["scope"] = scope ?? Scopes
        };

        private static JsonNode Out(AutomationRunStep step)
        {
            var node = JsonNode.Parse(step.OutputJson!)!;
            return node["result"] ?? node;
        }

        private static string B64Url(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static async Task<(AutomationRunStep? step, FakeHttpHandler http)> RunAsync(
            string type,
            JsonObject config,
            Action<FakeHttpHandler> routes,
            string? scope = null,
            bool dryRun = false)
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler();
            routes(http);
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Google, Payload(clock, scope));
            config["connectionId"] = connection.Id.ToString();
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, type, config);
            var (_, step) = await IntegrationTestHost.RunAsync(provider, id, dryRun: dryRun);
            return (step, http);
        }

        [Fact]
        public async Task Gmail_reply_keeps_the_thread_and_answers_the_sender()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.GmailReply,
                new JsonObject { ["messageId"] = "m1", ["body"] = "Thanks!" },
                h => h
                    .On("format=metadata", "{\"threadId\":\"t9\",\"payload\":{\"headers\":[" +
                        "{\"name\":\"From\",\"value\":\"Ann <ann@x.am>\"},{\"name\":\"Subject\",\"value\":\"Hi\"}," +
                        "{\"name\":\"Message-ID\",\"value\":\"<abc@mail>\"}]}}")
                    .On("/messages/send", "{\"id\":\"m2\",\"threadId\":\"t9\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var sent = JsonNode.Parse(http.To("/messages/send").Single().Body)!;
            Assert.Equal("t9", sent["threadId"]!.ToString());
            var raw = sent["raw"]!.ToString().Replace('-', '+').Replace('_', '/');
            var mime = Encoding.UTF8.GetString(Convert.FromBase64String(raw.PadRight(raw.Length + (4 - raw.Length % 4) % 4, '=')));
            Assert.Contains("To: Ann <ann@x.am>", mime);
            Assert.Contains("In-Reply-To: <abc@mail>", mime);
            Assert.Contains("References: <abc@mail>", mime);
            Assert.Equal("ann@x.am", Out(step)["to"]!.ToString().Replace("Ann <", string.Empty).Replace(">", string.Empty));
        }

        [Fact]
        public async Task Gmail_get_message_returns_text_and_attachment_names()
        {
            var json = "{\"threadId\":\"t1\",\"snippet\":\"Hello\",\"labelIds\":[\"INBOX\"],\"payload\":{\"mimeType\":\"multipart/mixed\"," +
                       "\"headers\":[{\"name\":\"From\",\"value\":\"a@b.am\"},{\"name\":\"Subject\",\"value\":\"Invoice\"}]," +
                       "\"parts\":[{\"mimeType\":\"text/plain\",\"body\":{\"data\":\"" + B64Url("Hello Բարև") + "\"}}," +
                       "{\"mimeType\":\"application/pdf\",\"filename\":\"inv.pdf\",\"body\":{\"size\":123}}]}}";
            var (step, _) = await RunAsync(
                IntegrationTypeCodes.GmailGetMessage,
                new JsonObject { ["messageId"] = "m1" },
                h => h.On("format=full", json),
                dryRun: true);

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var output = Out(step);
            Assert.Equal("Hello Բարև", output["body"]!.ToString());
            Assert.Equal("Invoice", output["subject"]!.ToString());
            Assert.Equal(1, output["attachmentCount"]!.GetValue<int>());
            Assert.Equal("inv.pdf", output["attachments"]![0]!["name"]!.ToString());
        }

        [Fact]
        public async Task Gmail_mark_read_needs_the_modify_permission()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.GmailMarkRead,
                new JsonObject { ["messageId"] = "m1" },
                _ => { },
                scope: GoogleOAuthService.ScopeGmailSend + " " + GoogleOAuthService.ScopeGmailRead);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("no access", step.Error);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Gmail_mark_read_removes_the_unread_label()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.GmailMarkRead,
                new JsonObject { ["messageId"] = "m1" },
                h => h.On("/messages/m1/modify", "{\"id\":\"m1\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var body = JsonNode.Parse(http.To("/modify").Single().Body)!;
            Assert.Equal("[\"UNREAD\"]", body["removeLabelIds"]!.ToJsonString());
            Assert.Equal("[]", body["addLabelIds"]!.ToJsonString());
        }

        [Fact]
        public async Task Gmail_label_creates_a_missing_label_then_adds_it()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.GmailLabel,
                new JsonObject { ["messageId"] = "m1", ["label"] = "Clients" },
                h => h
                    .On("/labels", "{\"labels\":[{\"id\":\"INBOX\",\"name\":\"INBOX\"}]}", method: HttpMethod.Get)
                    .On("/labels", "{\"id\":\"Label_7\",\"name\":\"Clients\"}", method: HttpMethod.Post)
                    .On("/messages/m1/modify", "{\"id\":\"m1\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            Assert.Equal("[\"Label_7\"]", JsonNode.Parse(http.To("/modify").Single().Body)!["addLabelIds"]!.ToJsonString());
        }

        [Fact]
        public async Task Gmail_trash_is_skipped_in_a_test_run()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.GmailTrash,
                new JsonObject { ["messageId"] = "m1" },
                _ => { },
                dryRun: true);

            Assert.Equal(AutomationStatuses.StepSkipped, step!.Status);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Sheets_delete_row_looks_up_the_tab_and_removes_that_row()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.SheetsDeleteRow,
                new JsonObject { ["spreadsheetId"] = "S1", ["sheet"] = "leads", ["rowNumber"] = "5" },
                h => h
                    .On("fields=sheets.properties", "{\"sheets\":[{\"properties\":{\"sheetId\":0,\"title\":\"Main\"}},{\"properties\":{\"sheetId\":42,\"title\":\"Leads\"}}]}")
                    .On(":batchUpdate", "{\"replies\":[{}]}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var range = JsonNode.Parse(http.To(":batchUpdate").Single().Body)!["requests"]![0]!["deleteDimension"]!["range"]!;
            Assert.Equal(42, range["sheetId"]!.GetValue<int>());
            Assert.Equal(4, range["startIndex"]!.GetValue<int>());
            Assert.Equal(5, range["endIndex"]!.GetValue<int>());
        }

        [Fact]
        public async Task Sheets_delete_row_reports_an_unknown_tab()
        {
            var (step, _) = await RunAsync(
                IntegrationTypeCodes.SheetsDeleteRow,
                new JsonObject { ["spreadsheetId"] = "S1", ["sheet"] = "Nope", ["rowNumber"] = "2" },
                h => h.On("fields=sheets.properties", "{\"sheets\":[{\"properties\":{\"sheetId\":0,\"title\":\"Main\"}}]}"));

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Nope", step.Error);
        }

        [Fact]
        public async Task Sheets_update_cell_writes_one_cell_and_refuses_a_range()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.SheetsUpdateCell,
                new JsonObject { ["spreadsheetId"] = "S1", ["sheet"] = "Leads", ["cell"] = "B7", ["value"] = "Done" },
                h => h.On("/values/", "{\"updatedRange\":\"Leads!B7\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var request = http.To("/values/").Single();
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Contains(Uri.EscapeDataString("'Leads'!B7"), request.Url);
            Assert.Equal("[[\"Done\"]]", JsonNode.Parse(request.Body)!["values"]!.ToJsonString());

            var (bad, none) = await RunAsync(
                IntegrationTypeCodes.SheetsUpdateCell,
                new JsonObject { ["spreadsheetId"] = "S1", ["cell"] = "A1:B2", ["value"] = "x" },
                _ => { });
            Assert.Equal(AutomationStatuses.StepFailed, bad!.Status);
            Assert.Empty(none.Requests);
        }

        [Fact]
        public async Task Sheets_read_range_returns_the_cells()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.SheetsReadRange,
                new JsonObject { ["spreadsheetId"] = "S1", ["range"] = "A1:B2" },
                h => h.On("/values/", "{\"values\":[[\"a\",\"b\"],[\"c\",\"d\"]]}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            Assert.Equal(2, Out(step)["count"]!.GetValue<int>());
            Assert.Equal("c", Out(step)["values"]![1]![0]!.ToString());
            Assert.Contains(Uri.EscapeDataString("'Sheet1'!A1:B2"), http.Requests.Single().Url);
        }

        [Fact]
        public async Task Sheets_create_spreadsheet_returns_its_link()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.SheetsCreateSpreadsheet,
                new JsonObject { ["title"] = "Leads 2026", ["sheet"] = "Main" },
                h => h.On("sheets.googleapis.com/v4/spreadsheets", "{\"spreadsheetId\":\"NEW1\",\"spreadsheetUrl\":\"https://docs.google.com/spreadsheets/d/NEW1/edit\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            Assert.Equal("NEW1", Out(step)["spreadsheetId"]!.ToString());
            var body = JsonNode.Parse(http.Requests.Single().Body)!;
            Assert.Equal("Leads 2026", body["properties"]!["title"]!.ToString());
            Assert.Equal("Main", body["sheets"]![0]!["properties"]!["title"]!.ToString());
        }

        [Fact]
        public async Task Drive_move_swaps_the_parent_folder()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.DriveMoveFile,
                new JsonObject
                {
                    ["fileId"] = "https://drive.google.com/file/d/FILE_123456/view",
                    ["folderId"] = "https://drive.google.com/drive/folders/DEST_654321"
                },
                h => h
                    .On("fields=parents", "{\"parents\":[\"OLD_111111\"]}", method: HttpMethod.Get)
                    .On("addParents", "{\"id\":\"FILE_123456\",\"name\":\"a.txt\"}", method: HttpMethod.Patch));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var patch = http.To("addParents").Single();
            Assert.Contains("/files/FILE_123456?", patch.Url);
            Assert.Contains("addParents=DEST_654321", patch.Url);
            Assert.Contains("removeParents=OLD_111111", patch.Url);
        }

        [Fact]
        public async Task Drive_trash_only_marks_the_file_trashed()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.DriveTrashFile,
                new JsonObject { ["fileId"] = "FILE_123456" },
                h => h.On("/files/FILE_123456", "{\"id\":\"FILE_123456\",\"name\":\"a.txt\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var request = http.Requests.Single();
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("{\"trashed\":true}", request.Body);
        }

        [Fact]
        public async Task Drive_share_defaults_to_view_access_with_a_notification()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.DriveShareFile,
                new JsonObject { ["fileId"] = "FILE_123456", ["email"] = "ann@x.am" },
                h => h
                    .On("/permissions", "{\"id\":\"p1\",\"role\":\"reader\"}", method: HttpMethod.Post)
                    .On("/files/FILE_123456", "{\"id\":\"FILE_123456\",\"webViewLink\":\"https://drive.google.com/file/d/FILE_123456/view\"}", method: HttpMethod.Get));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var permission = http.To("/permissions").Single();
            Assert.Contains("sendNotificationEmail=true", permission.Url);
            var body = JsonNode.Parse(permission.Body)!;
            Assert.Equal("reader", body["role"]!.ToString());
            Assert.Equal("ann@x.am", body["emailAddress"]!.ToString());
            Assert.Contains("drive.google.com", Out(step)["webViewLink"]!.ToString());
        }

        [Fact]
        public async Task Drive_create_text_file_can_become_a_google_doc()
        {
            var (step, http) = await RunAsync(
                IntegrationTypeCodes.DriveCreateTextFile,
                new JsonObject { ["name"] = "Notes", ["content"] = "Line 1\nLine 2", ["asDocument"] = "true" },
                h => h.On("uploadType=multipart", "{\"id\":\"D1\",\"name\":\"Notes\"}"));

            Assert.Equal(AutomationStatuses.StepSucceeded, step!.Status);
            var body = http.Requests.Single().Body;
            Assert.Contains("application/vnd.google-apps.document", body);
            Assert.Contains("Line 1\nLine 2", body);
        }

        [Fact]
        public void Helpers_accept_links_and_reject_odd_ranges()
        {
            Assert.Equal("ABC_def-1234", GoogleDriveApi.ItemId("https://drive.google.com/open?id=ABC_def-1234"));
            Assert.Equal("ABC_def-1234", GoogleDriveApi.ItemId("https://docs.google.com/document/d/ABC_def-1234/edit"));
            Assert.Equal("plainId", GoogleDriveApi.ItemId(" plainId "));
            Assert.True(GoogleSheetsApi.IsCellRange("A1:C10"));
            Assert.True(GoogleSheetsApi.IsCellRange("B7"));
            Assert.True(GoogleSheetsApi.IsCellRange("A:C"));
            Assert.False(GoogleSheetsApi.IsCellRange("A1'!B2"));
            Assert.False(GoogleSheetsApi.IsCellRange("../x"));
            Assert.False(GoogleSheetsApi.IsCellRange(""));
        }

        [Fact]
        public void Gmail_text_falls_back_to_html_without_scripts()
        {
            var payload = JsonNode.Parse("{\"mimeType\":\"text/html\",\"body\":{\"data\":\"" +
                                         B64Url("<style>p{}</style><p>Hi&nbsp;<b>there</b></p><script>alert(1)</script>") + "\"}}")!.AsObject();

            var text = GmailApi.ExtractText(payload);

            Assert.Contains("there", text);
            Assert.DoesNotContain("alert", text);
            Assert.DoesNotContain("<", text);
        }
    }
}
