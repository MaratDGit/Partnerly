using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Gmail
{
    public partial class GmailApi
    {
        public const string BaseUrl = "https://gmail.googleapis.com/gmail/v1/users/me";

        private readonly GoogleApiClient _google;

        public GmailApi(GoogleApiClient google)
        {
            _google = google;
        }

        public async Task<JsonObject> SendAsync(
            IntegrationConnection connection,
            string to,
            string subject,
            string body,
            bool html,
            CancellationToken cancellationToken)
        {
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailSend);
            var raw = new JsonObject { ["raw"] = Base64Url(BuildMime(to, subject, body, html)) };
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/messages/send")
            {
                Content = IntegrationHttp.Json(raw)
            }, "Gmail", cancellationToken);
        }

        public async Task<List<JsonObject>> FindAsync(
            IntegrationConnection connection,
            string query,
            int max,
            CancellationToken cancellationToken)
        {
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailRead);
            var url = $"{BaseUrl}/messages?maxResults={Math.Clamp(max, 1, 25)}&q={Uri.EscapeDataString(query ?? string.Empty)}";
            var list = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Gmail", cancellationToken);
            var result = new List<JsonObject>();
            foreach (var item in (list["messages"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
            {
                var id = item["id"]?.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    result.Add(await GetSummaryAsync(connection, id, cancellationToken));
                }
            }

            return result;
        }

        public async Task<JsonObject> GetSummaryAsync(IntegrationConnection connection, string id, CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/messages/{Uri.EscapeDataString(id)}?format=metadata&metadataHeaders=From&metadataHeaders=To&metadataHeaders=Subject&metadataHeaders=Date";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Gmail", cancellationToken);
            var headers = ((json["payload"] as JsonObject)?["headers"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .GroupBy(h => h["name"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First()["value"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            string Header(string name) => headers.TryGetValue(name, out var value) ? value : string.Empty;
            return new JsonObject
            {
                ["id"] = id,
                ["threadId"] = json["threadId"]?.ToString(),
                ["from"] = Header("From"),
                ["to"] = Header("To"),
                ["subject"] = Header("Subject"),
                ["date"] = Header("Date"),
                ["snippet"] = WebUtility.HtmlDecode(json["snippet"]?.ToString() ?? string.Empty),
                ["labels"] = json["labelIds"]?.DeepClone()
            };
        }

        public async Task<string> CurrentHistoryIdAsync(IntegrationConnection connection, CancellationToken cancellationToken)
        {
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailRead);
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/profile"), "Gmail", cancellationToken);
            return json["historyId"]?.ToString() ?? string.Empty;
        }

        // Returns new INBOX message ids since historyId and the next cursor.
        public async Task<(List<string> ids, string? nextHistoryId)> NewMessagesAsync(
            IntegrationConnection connection,
            string historyId,
            CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/history?startHistoryId={Uri.EscapeDataString(historyId)}&historyTypes=messageAdded&labelId=INBOX&maxResults=100";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Gmail", cancellationToken);
            var ids = (json["history"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .SelectMany(h => (h["messagesAdded"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                .Select(m => (m["message"] as JsonObject)?["id"]?.ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();
            return (ids, json["historyId"]?.ToString());
        }

        public static string BuildMime(string to, string subject, string body, bool html, IEnumerable<(string name, string value)>? extraHeaders = null)
        {
            var builder = new StringBuilder();
            builder.Append("To: ").Append(SafeHeader(to)).Append("\r\n");
            foreach (var (name, value) in extraHeaders ?? Array.Empty<(string name, string value)>())
            {
                builder.Append(SafeHeader(name)).Append(": ").Append(SafeHeader(value)).Append("\r\n");
            }
            builder.Append("Subject: =?UTF-8?B?").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(SafeHeader(subject)))).Append("?=\r\n");
            builder.Append("MIME-Version: 1.0\r\n");
            builder.Append("Content-Type: ").Append(html ? "text/html" : "text/plain").Append("; charset=UTF-8\r\n");
            builder.Append("Content-Transfer-Encoding: base64\r\n\r\n");
            builder.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(body ?? string.Empty), Base64FormattingOptions.InsertLineBreaks));
            return builder.ToString();
        }

        public static string Base64Url(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Header injection guard: no CR/LF from variables.
        private static string SafeHeader(string value) =>
            (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
