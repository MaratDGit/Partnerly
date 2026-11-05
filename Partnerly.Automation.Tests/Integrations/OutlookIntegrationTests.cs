using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Microsoft;

namespace Partnerly.Automation.Tests.Integrations
{
    public class OutlookIntegrationTests
    {
        private const string AllScopes = "openid email offline_access User.Read Mail.Send Mail.Read Calendars.ReadWrite";

        private static JsonObject MicrosoftPayload(FakeClock clock, string? scope = null, bool expired = false) => new()
        {
            ["email"] = "me@contoso.com",
            ["accessToken"] = "access-1",
            ["refreshToken"] = "refresh-1",
            ["expiresAt"] = clock.UtcNow.AddMinutes(expired ? -5 : 30).ToString("O"),
            ["scope"] = scope ?? AllScopes
        };

        [Fact]
        public async Task Send_posts_message_and_keeps_rotated_refresh_token()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler()
                .On("/oauth2/v2.0/token", "{\"access_token\":\"access-2\",\"refresh_token\":\"refresh-2\",\"expires_in\":3600}")
                .On("/me/sendMail", "", HttpStatusCode.Accepted);
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock, expired: true));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookSend, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "a@b.am; c@d.am\r\nBcc: evil@x.am",
                ["subject"] = "Hi\r\nBcc: evil@x.am",
                ["body"] = "<p>Hello</p>",
                ["format"] = "html"
            });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            Assert.Contains("refresh_token=refresh-1", http.To("/token").Single().Body);
            var send = http.To("/me/sendMail").Single();
            Assert.Equal("Bearer access-2", send.Authorization);
            var message = JsonNode.Parse(send.Body)!["message"]!;
            Assert.Equal("HTML", message["body"]!["contentType"]!.ToString());
            Assert.Equal("Hi  Bcc: evil@x.am", message["subject"]!.ToString());
            var recipients = message["toRecipients"]!.AsArray().Select(item => item!["emailAddress"]!["address"]!.ToString()).ToList();
            Assert.Equal(new[] { "a@b.am", "c@d.am" }, recipients);
            Assert.Contains("\"sent\":true", step!.OutputJson);
            var saved = await IntegrationTestHost.ReloadAsync(provider, connection.Id);
            Assert.Equal("access-2", saved.GetString("accessToken"));
            Assert.Equal("refresh-2", saved.GetString("refreshToken"));
        }

        [Fact]
        public async Task Send_is_skipped_in_test_run_and_needs_a_recipient()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var dry = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookSend, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "a@b.am"
            });
            var noTo = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookSend, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "not-an-address"
            });

            var (_, dryStep) = await IntegrationTestHost.RunAsync(provider, dry, dryRun: true);
            var (_, failedStep) = await IntegrationTestHost.RunAsync(provider, noTo);

            Assert.Equal(AutomationStatuses.StepSkipped, dryStep!.Status);
            Assert.Equal(AutomationStatuses.StepFailed, failedStep!.Status);
            Assert.Contains("Recipient", failedStep.Error);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Another_users_connection_and_missing_scope_are_refused()
        {
            var owner = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler();
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var foreign = await IntegrationTestHost.ConnectAsync(provider, stranger, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var calendarOnly = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft,
                MicrosoftPayload(clock, scope: MicrosoftOAuthService.ScopeCalendar));
            var stolen = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookSend, new JsonObject
            {
                ["connectionId"] = foreign.Id.ToString(),
                ["to"] = "a@b.am"
            });
            var noScope = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookSend, new JsonObject
            {
                ["connectionId"] = calendarOnly.Id.ToString(),
                ["to"] = "a@b.am"
            });

            var (_, stolenStep) = await IntegrationTestHost.RunAsync(provider, stolen);
            var (_, scopeStep) = await IntegrationTestHost.RunAsync(provider, noScope);

            Assert.Equal(AutomationStatuses.StepFailed, stolenStep!.Status);
            Assert.Contains("not found", stolenStep.Error);
            Assert.Equal(AutomationStatuses.StepFailed, scopeStep!.Status);
            Assert.Contains("no access", scopeStep.Error);
            Assert.Empty(http.Requests);
        }

        [Fact]
        public async Task Revoked_refresh_token_asks_to_reconnect()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/token", "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70008: The refresh token has expired.\"}", HttpStatusCode.BadRequest);
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock, expired: true));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookSend, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["to"] = "a@b.am"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id);

            Assert.Equal(AutomationStatuses.StepFailed, step!.Status);
            Assert.Contains("Reconnect Outlook", step.Error);
            Assert.False((await IntegrationTestHost.ReloadAsync(provider, connection.Id)).Status.Ok);
        }

        [Fact]
        public async Task Find_messages_searches_and_runs_in_test_mode()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/me/messages", "{\"value\":[{\"id\":\"m1\",\"subject\":\"Invoice 12\",\"from\":{\"emailAddress\":{\"address\":\"bill@shop.am\",\"name\":\"Billing\"}},\"toRecipients\":[{\"emailAddress\":{\"address\":\"me@contoso.com\"}}],\"receivedDateTime\":\"2026-11-02T12:05:00Z\",\"bodyPreview\":\"Attached\"}]}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookFindMessages, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["query"] = "invoice \"12\"",
                ["max"] = "3"
            });

            var (run, step) = await IntegrationTestHost.RunAsync(provider, id, dryRun: true);

            Assert.Equal(AutomationStatuses.Succeeded, run.Status);
            var request = http.Requests.Single();
            Assert.Contains("$search=", request.Url);
            Assert.Contains("$top=3", request.Url);
            Assert.DoesNotContain("$orderby", request.Url);
            Assert.Contains("\"count\":1", step!.OutputJson);
            Assert.Contains("bill@shop.am", step.OutputJson);
        }

        [Fact]
        public async Task Create_event_posts_times_attendees_and_validates_dates()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/me/events", "{\"id\":\"ev1\",\"webLink\":\"https://outlook.live.com/ev1\"}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var good = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookCreateEvent, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["subject"] = "Demo call",
                ["start"] = "2026-11-06T10:00",
                ["durationMinutes"] = "30",
                ["timeZone"] = "Europe/Moscow",
                ["attendees"] = "a@b.am, c@d.am",
                ["location"] = "Office"
            });
            var bad = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookCreateEvent, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["subject"] = "Broken",
                ["start"] = "tomorrow-ish"
            });

            var (_, goodStep) = await IntegrationTestHost.RunAsync(provider, good);
            var (_, badStep) = await IntegrationTestHost.RunAsync(provider, bad);

            var body = JsonNode.Parse(http.To("/me/events").Single().Body)!;
            Assert.Equal("2026-11-06T10:00:00", body["start"]!["dateTime"]!.ToString());
            Assert.Equal("2026-11-06T10:30:00", body["end"]!["dateTime"]!.ToString());
            Assert.Equal("Europe/Moscow", body["start"]!["timeZone"]!.ToString());
            Assert.Equal(2, body["attendees"]!.AsArray().Count);
            Assert.Equal("Office", body["location"]!["displayName"]!.ToString());
            Assert.Contains("ev1", goodStep!.OutputJson);
            Assert.Equal(AutomationStatuses.StepFailed, badStep!.Status);
            Assert.Contains("Start must be", badStep.Error);
        }

        [Fact]
        public async Task Find_events_reads_the_calendar_view_for_the_next_days()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/me/calendarView", "{\"value\":[{\"id\":\"ev1\",\"subject\":\"Standup\",\"start\":{\"dateTime\":\"2026-11-03T09:00:00.0000000\"},\"end\":{\"dateTime\":\"2026-11-03T09:15:00.0000000\"},\"location\":{\"displayName\":\"Room 1\"}}]}");
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var id = await IntegrationTestHost.PublishSingleActionAsync(provider, owner, IntegrationTypeCodes.OutlookFindEvents, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["days"] = "3"
            });

            var (_, step) = await IntegrationTestHost.RunAsync(provider, id, dryRun: true);

            var url = Uri.UnescapeDataString(http.Requests.Single().Url);
            Assert.Contains("startDateTime=2026-11-02T12:00:00Z", url);
            Assert.Contains("endDateTime=2026-11-05T12:00:00Z", url);
            Assert.Contains("Standup", step!.OutputJson);
        }

        [Fact]
        public async Task Poller_starts_from_now_then_reports_new_matching_mail()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/mailFolders/inbox/messages", request =>
            {
                if (request.Url.Contains("$top=1&", StringComparison.Ordinal))
                {
                    return (HttpStatusCode.OK, "{\"value\":[{\"receivedDateTime\":\"2026-11-02T12:00:00Z\"}]}");
                }

                return (HttpStatusCode.OK, "{\"value\":["
                    + "{\"id\":\"a\",\"subject\":\"Invoice 12\",\"from\":{\"emailAddress\":{\"address\":\"bill@shop.am\"}},\"receivedDateTime\":\"2026-11-02T12:05:00Z\",\"bodyPreview\":\"Attached\"},"
                    + "{\"id\":\"b\",\"subject\":\"Hi\",\"from\":{\"emailAddress\":{\"address\":\"friend@x.am\"}},\"receivedDateTime\":\"2026-11-02T12:06:00Z\"}]}");
            });
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.OutlookMessageReceived, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["subjectContains"] = "invoice"
            });

            await IntegrationTestHost.PollAsync(provider);
            Assert.Equal("2026-11-02T12:00:00Z", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("outlookCursor"));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, id));

            await IntegrationTestHost.PollAsync(provider);
            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            var trigger = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal("Invoice 12", trigger["subject"]!.ToString());
            Assert.Equal("bill@shop.am", trigger["from"]!.ToString());
            Assert.Equal("2026-11-02T12:06:00Z", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("outlookCursor"));
            Assert.Contains("receivedDateTime", http.Requests.Last().Url);
        }

        [Fact]
        public async Task Event_poller_starts_from_now_then_reports_new_matching_events()
        {
            var owner = Guid.NewGuid();
            var clock = new FakeClock();
            var http = new FakeHttpHandler().On("/me/events", request =>
            {
                if (request.Url.Contains("$top=1&", StringComparison.Ordinal))
                {
                    return (HttpStatusCode.OK, "{\"value\":[{\"createdDateTime\":\"2026-11-02T12:00:00Z\"}]}");
                }

                return (HttpStatusCode.OK, "{\"value\":["
                    + "{\"id\":\"e1\",\"subject\":\"Demo call\",\"start\":{\"dateTime\":\"2026-11-06T10:00:00.0000000\"},\"end\":{\"dateTime\":\"2026-11-06T11:00:00.0000000\"},\"location\":{\"displayName\":\"Office\"},\"createdDateTime\":\"2026-11-02T12:05:00Z\"},"
                    + "{\"id\":\"e2\",\"subject\":\"Lunch\",\"createdDateTime\":\"2026-11-02T12:06:00Z\"}]}");
            });
            await using var provider = IntegrationTestHost.Create(owner, http, clock);
            var connection = await IntegrationTestHost.ConnectAsync(provider, owner, IntegrationKinds.Microsoft, MicrosoftPayload(clock));
            var id = await IntegrationTestHost.PublishTriggerAsync(provider, owner, IntegrationTypeCodes.OutlookEventAdded, new JsonObject
            {
                ["connectionId"] = connection.Id.ToString(),
                ["subjectContains"] = "demo"
            });

            await IntegrationTestHost.PollAsync(provider);
            Assert.Equal("2026-11-02T12:00:00Z", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("outlookEventCursor"));
            Assert.Empty(IntegrationTestHost.RunsOf(provider, id));

            await IntegrationTestHost.PollAsync(provider);
            var run = Assert.Single(IntegrationTestHost.RunsOf(provider, id));
            var trigger = IntegrationTestHost.TriggerPayload(run);
            Assert.Equal("Demo call", trigger["subject"]!.ToString());
            Assert.Equal("Office", trigger["location"]!.ToString());
            Assert.Equal("2026-11-02T12:06:00Z", (await IntegrationTestHost.ReloadAsync(provider, connection.Id)).GetString("outlookEventCursor"));
        }

        [Fact]
        public async Task Consent_url_asks_for_chosen_scopes_without_secret()
        {
            await using var provider = IntegrationTestHost.Create(Guid.NewGuid(), new FakeHttpHandler());
            var oauth = provider.GetRequiredService<MicrosoftOAuthService>();

            var url = oauth.BuildConsentUrl("http://localhost:5000/Integrations/Microsoft/Callback", "st", MicrosoftOAuthService.ScopesFor(mail: true, calendar: false));

            Assert.StartsWith("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", url);
            Assert.Contains("client_id=ms-client", url);
            Assert.Contains("offline_access", url);
            Assert.Contains("Mail.Send", url);
            Assert.DoesNotContain("Calendars", url);
            Assert.DoesNotContain("ms-secret", url);
            Assert.True(oauth.IsConfigured);
        }

        [Fact]
        public void Outlook_nodes_are_registered_with_the_microsoft_kind()
        {
            using var provider = IntegrationTestHost.Create(Guid.NewGuid(), new FakeHttpHandler());
            var actions = provider.GetRequiredService<Partnerly.Automation.Registry.ActionRegistry>();
            var triggers = provider.GetRequiredService<Partnerly.Automation.Registry.TriggerRegistry>();

            foreach (var type in new[] { IntegrationTypeCodes.OutlookSend, IntegrationTypeCodes.OutlookFindMessages, IntegrationTypeCodes.OutlookCreateEvent, IntegrationTypeCodes.OutlookFindEvents })
            {
                Assert.True(actions.IsKnown(type), type);
            }

            Assert.True(triggers.IsKnown(IntegrationTypeCodes.OutlookMessageReceived));
            Assert.True(triggers.IsKnown(IntegrationTypeCodes.OutlookEventAdded));
        }
    }
}
