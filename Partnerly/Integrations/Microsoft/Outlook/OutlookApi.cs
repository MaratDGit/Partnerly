using System.Globalization;
using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Microsoft.Outlook
{
    // Outlook mail and calendar through Microsoft Graph (/me = connected account).
    public class OutlookApi
    {
        public const string BaseUrl = MicrosoftOAuthService.GraphBase + "/me";
        private const string EventFields = "id,subject,start,end,location,organizer,webLink,isAllDay,createdDateTime";
        private const string MessageFields = "id,conversationId,subject,from,toRecipients,receivedDateTime,bodyPreview,isRead,webLink";

        private readonly MicrosoftApiClient _graph;

        public OutlookApi(MicrosoftApiClient graph)
        {
            _graph = graph;
        }

        public async Task SendAsync(
            IntegrationConnection connection,
            IReadOnlyList<string> to,
            string subject,
            string body,
            bool html,
            CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeMailSend);
            var message = new JsonObject
            {
                ["message"] = new JsonObject
                {
                    ["subject"] = OneLine(subject),
                    ["body"] = new JsonObject { ["contentType"] = html ? "HTML" : "Text", ["content"] = body ?? string.Empty },
                    ["toRecipients"] = Recipients(to)
                },
                ["saveToSentItems"] = true
            };
            // Graph answers 202 with an empty body.
            await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/sendMail")
            {
                Content = IntegrationHttp.Json(message)
            }, cancellationToken);
        }

        public async Task<List<JsonObject>> FindAsync(
            IntegrationConnection connection,
            string query,
            int max,
            CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeMailRead);
            var top = Math.Clamp(max, 1, 25);
            // $search cannot be combined with $orderby / $filter.
            var url = string.IsNullOrWhiteSpace(query)
                ? $"{BaseUrl}/messages?$top={top}&$orderby=receivedDateTime desc&$select={MessageFields}"
                : $"{BaseUrl}/messages?$top={top}&$select={MessageFields}&$search=" + Uri.EscapeDataString("\"" + query.Replace("\"", " ") + "\"");
            var json = await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            return Values(json).Select(ToSummary).ToList();
        }

        // Inbox messages received after the cursor (ISO time), oldest first.
        public async Task<List<JsonObject>> NewMessagesAsync(
            IntegrationConnection connection,
            string afterIso,
            CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeMailRead);
            var url = $"{BaseUrl}/mailFolders/inbox/messages?$top=25&$orderby=receivedDateTime asc&$select={MessageFields}"
                      + "&$filter=" + Uri.EscapeDataString("receivedDateTime gt " + afterIso);
            var json = await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            return Values(json).Select(ToSummary).ToList();
        }

        // Received time of the newest inbox message, or null for an empty inbox.
        public async Task<string?> LatestReceivedAsync(IntegrationConnection connection, CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeMailRead);
            var url = $"{BaseUrl}/mailFolders/inbox/messages?$top=1&$orderby=receivedDateTime desc&$select=receivedDateTime";
            var json = await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            return Values(json).FirstOrDefault()?["receivedDateTime"]?.ToString();
        }

        public async Task<JsonObject> CreateEventAsync(
            IntegrationConnection connection,
            string subject,
            DateTime start,
            DateTime end,
            string timeZone,
            string body,
            string location,
            IReadOnlyList<string> attendees,
            CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeCalendar);
            var payload = new JsonObject
            {
                ["subject"] = OneLine(subject),
                ["start"] = new JsonObject { ["dateTime"] = start.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), ["timeZone"] = timeZone },
                ["end"] = new JsonObject { ["dateTime"] = end.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), ["timeZone"] = timeZone }
            };
            if (!string.IsNullOrWhiteSpace(body))
            {
                payload["body"] = new JsonObject { ["contentType"] = "Text", ["content"] = body };
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                payload["location"] = new JsonObject { ["displayName"] = OneLine(location) };
            }

            if (attendees.Count > 0)
            {
                var list = new JsonArray();
                foreach (var address in attendees)
                {
                    list.Add(new JsonObject { ["emailAddress"] = new JsonObject { ["address"] = address }, ["type"] = "required" });
                }

                payload["attendees"] = list;
            }

            return await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/events")
            {
                Content = IntegrationHttp.Json(payload)
            }, cancellationToken);
        }

        // Events created after the cursor (ISO time), oldest first.
        public async Task<List<JsonObject>> NewEventsAsync(
            IntegrationConnection connection,
            string afterIso,
            CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeCalendar);
            var url = $"{BaseUrl}/events?$top=25&$orderby=createdDateTime asc&$select={EventFields}"
                      + "&$filter=" + Uri.EscapeDataString("createdDateTime gt " + afterIso);
            var json = await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            return Values(json).Select(ToEvent).ToList();
        }

        // Creation time of the newest event, or null for an empty calendar.
        public async Task<string?> LatestEventCreatedAsync(IntegrationConnection connection, CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeCalendar);
            var url = $"{BaseUrl}/events?$top=1&$orderby=createdDateTime desc&$select=createdDateTime";
            var json = await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            return Values(json).FirstOrDefault()?["createdDateTime"]?.ToString();
        }

        public async Task<List<JsonObject>> FindEventsAsync(
            IntegrationConnection connection,
            DateTime fromUtc,
            DateTime toUtc,
            int max,
            CancellationToken cancellationToken)
        {
            _graph.RequireScope(connection, MicrosoftOAuthService.ScopeCalendar);
            var url = $"{BaseUrl}/calendarView?startDateTime={Uri.EscapeDataString(Iso(fromUtc))}&endDateTime={Uri.EscapeDataString(Iso(toUtc))}"
                      + $"&$top={Math.Clamp(max, 1, 50)}&$orderby=start/dateTime&$select={EventFields}";
            var json = await _graph.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            return Values(json).Select(ToEvent).ToList();
        }

        public static JsonObject ToSummary(JsonObject message)
        {
            var from = (message["from"] as JsonObject)?["emailAddress"] as JsonObject;
            var to = (message["toRecipients"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .Select(item => (item["emailAddress"] as JsonObject)?["address"]?.ToString())
                .Where(address => !string.IsNullOrEmpty(address));
            return new JsonObject
            {
                ["id"] = message["id"]?.ToString(),
                ["conversationId"] = message["conversationId"]?.ToString(),
                ["from"] = from?["address"]?.ToString() ?? string.Empty,
                ["fromName"] = from?["name"]?.ToString() ?? string.Empty,
                ["to"] = string.Join(", ", to),
                ["subject"] = message["subject"]?.ToString() ?? string.Empty,
                ["date"] = message["receivedDateTime"]?.ToString(),
                ["snippet"] = message["bodyPreview"]?.ToString() ?? string.Empty,
                ["isRead"] = message["isRead"]?.DeepClone(),
                ["webLink"] = message["webLink"]?.ToString()
            };
        }

        public static JsonObject ToEvent(JsonObject item) => new()
        {
            ["id"] = item["id"]?.ToString(),
            ["subject"] = item["subject"]?.ToString() ?? string.Empty,
            ["start"] = (item["start"] as JsonObject)?["dateTime"]?.ToString(),
            ["end"] = (item["end"] as JsonObject)?["dateTime"]?.ToString(),
            ["location"] = (item["location"] as JsonObject)?["displayName"]?.ToString() ?? string.Empty,
            ["organizer"] = ((item["organizer"] as JsonObject)?["emailAddress"] as JsonObject)?["address"]?.ToString(),
            ["webLink"] = item["webLink"]?.ToString(),
            ["created"] = item["createdDateTime"]?.ToString()
        };

        private static string Iso(DateTime utc) =>
            utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + "Z";

        private static IEnumerable<JsonObject> Values(JsonObject json) =>
            (json["value"] as JsonArray ?? new JsonArray()).OfType<JsonObject>();

        private static JsonArray Recipients(IReadOnlyList<string> addresses)
        {
            var array = new JsonArray();
            foreach (var address in addresses)
            {
                array.Add(new JsonObject { ["emailAddress"] = new JsonObject { ["address"] = address } });
            }

            return array;
        }

        // Header-like fields: no CR/LF from variables.
        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
