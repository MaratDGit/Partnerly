using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Drive
{
    // Metadata, copy, move, rename, trash, sharing and text files.
    public partial class GoogleDriveApi
    {
        public const string DocMime = "application/vnd.google-apps.document";

        private static readonly Regex IdFromUrl = new(@"/(?:d|folders)/([A-Za-z0-9_-]{10,})|[?&]id=([A-Za-z0-9_-]{10,})", RegexOptions.Compiled);

        // Users paste the browser link; accept it as well as a bare id.
        public static string ItemId(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            var match = IdFromUrl.Match(text);
            return match.Success ? (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value) : text;
        }

        private static string FileUrl(string id, string extra = "") =>
            $"{BaseUrl}/files/{Uri.EscapeDataString(id)}?fields={FileFields}&supportsAllDrives=true{extra}";

        public async Task<JsonObject> GetFileAsync(IntegrationConnection connection, string id, CancellationToken cancellationToken)
        {
            Require(connection);
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, FileUrl(id)), "Google Drive", cancellationToken);
            return Clean(json);
        }

        public async Task<JsonObject> CopyFileAsync(
            IntegrationConnection connection,
            string id,
            string? name,
            string? folderId,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var body = new JsonObject();
            if (!string.IsNullOrWhiteSpace(name))
            {
                body["name"] = name;
            }

            if (!string.IsNullOrWhiteSpace(folderId))
            {
                body["parents"] = new JsonArray(folderId);
            }

            var url = $"{BaseUrl}/files/{Uri.EscapeDataString(id)}/copy?fields={FileFields}&supportsAllDrives=true";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Drive", cancellationToken);
            return Clean(json);
        }

        public async Task<JsonObject> MoveFileAsync(
            IntegrationConnection connection,
            string id,
            string folderId,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var current = await _google.SendAsync(
                connection,
                () => new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/files/{Uri.EscapeDataString(id)}?fields=parents&supportsAllDrives=true"),
                "Google Drive",
                cancellationToken);
            var parents = string.Join(",", (current["parents"] as JsonArray ?? new JsonArray()).Select(p => p?.ToString()).Where(p => !string.IsNullOrEmpty(p)));
            var extra = $"&addParents={Uri.EscapeDataString(folderId)}" +
                        (parents.Length > 0 ? $"&removeParents={Uri.EscapeDataString(parents)}" : string.Empty);
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Patch, FileUrl(id, extra))
            {
                Content = IntegrationHttp.Json(new JsonObject())
            }, "Google Drive", cancellationToken);
            return Clean(json);
        }

        public async Task<JsonObject> UpdateFileAsync(
            IntegrationConnection connection,
            string id,
            JsonObject changes,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Patch, FileUrl(id))
            {
                Content = IntegrationHttp.Json(changes)
            }, "Google Drive", cancellationToken);
            return Clean(json);
        }

        public async Task<JsonObject> ShareFileAsync(
            IntegrationConnection connection,
            string id,
            string email,
            string role,
            bool notify,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var body = new JsonObject { ["type"] = "user", ["role"] = role, ["emailAddress"] = email };
            var url = $"{BaseUrl}/files/{Uri.EscapeDataString(id)}/permissions?supportsAllDrives=true&sendNotificationEmail={(notify ? "true" : "false")}";
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Drive", cancellationToken);
        }

        // Text file; asDocument turns it into a Google Doc.
        public async Task<JsonObject> CreateTextFileAsync(
            IntegrationConnection connection,
            string name,
            string content,
            string? folderId,
            bool asDocument,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var metadata = new JsonObject { ["name"] = name };
            if (asDocument)
            {
                metadata["mimeType"] = DocMime;
            }

            if (!string.IsNullOrWhiteSpace(folderId))
            {
                metadata["parents"] = new JsonArray(folderId);
            }

            var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
            HttpRequestMessage Build()
            {
                var part = new MultipartContent("related");
                part.Add(new StringContent(metadata.ToJsonString(), Encoding.UTF8, "application/json"));
                var media = new ByteArrayContent(bytes);
                media.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" };
                part.Add(media);
                return new HttpRequestMessage(HttpMethod.Post, $"{UploadUrl}?uploadType=multipart&fields={FileFields}") { Content = part };
            }

            var json = await _google.SendAsync(connection, Build, "Google Drive", cancellationToken);
            return Clean(json);
        }
    }
}
