using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Drive
{
    public partial class GoogleDriveApi
    {
        public const string BaseUrl = "https://www.googleapis.com/drive/v3";
        public const string UploadUrl = "https://www.googleapis.com/upload/drive/v3/files";
        public const string FolderMime = "application/vnd.google-apps.folder";
        private const string FileFields = "id,name,mimeType,webViewLink,parents,createdTime,modifiedTime,size";

        private readonly GoogleApiClient _google;

        public GoogleDriveApi(GoogleApiClient google)
        {
            _google = google;
        }

        public async Task<JsonObject> UploadAsync(
            IntegrationConnection connection,
            IntegrationFile file,
            string? name,
            string? folderId,
            CancellationToken cancellationToken)
        {
            Require(connection);
            using var buffer = new MemoryStream();
            await file.Stream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var metadata = new JsonObject { ["name"] = string.IsNullOrWhiteSpace(name) ? file.FileName : name };
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                metadata["parents"] = new JsonArray(folderId);
            }

            // multipart/related: metadata part + media part (files up to a few MB;
            // attachments in Partnerly are already size-limited by SystemSettings).
            HttpRequestMessage Build()
            {
                var content = new MultipartContent("related");
                content.Add(new StringContent(metadata.ToJsonString(), Encoding.UTF8, "application/json"));
                var media = new ByteArrayContent(bytes);
                media.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                content.Add(media);
                return new HttpRequestMessage(HttpMethod.Post, $"{UploadUrl}?uploadType=multipart&fields={FileFields}") { Content = content };
            }

            return await _google.SendAsync(connection, Build, "Google Drive", cancellationToken);
        }

        public async Task<JsonObject> CreateFolderAsync(
            IntegrationConnection connection,
            string name,
            string? parentId,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var body = new JsonObject { ["name"] = name, ["mimeType"] = FolderMime };
            if (!string.IsNullOrWhiteSpace(parentId))
            {
                body["parents"] = new JsonArray(parentId);
            }

            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/files?fields={FileFields}")
            {
                Content = IntegrationHttp.Json(body)
            }, "Google Drive", cancellationToken);
        }

        public async Task<List<JsonObject>> FindAsync(
            IntegrationConnection connection,
            string? nameContains,
            string? folderId,
            int max,
            CancellationToken cancellationToken)
        {
            Require(connection);
            var q = new List<string> { "trashed = false" };
            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                q.Add($"name contains '{Escape(nameContains)}'");
            }

            if (!string.IsNullOrWhiteSpace(folderId))
            {
                q.Add($"'{Escape(folderId)}' in parents");
            }

            var url = $"{BaseUrl}/files?pageSize={Math.Clamp(max, 1, 50)}&orderBy=modifiedTime desc" +
                      $"&fields=files({FileFields})&q={Uri.EscapeDataString(string.Join(" and ", q))}";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Google Drive", cancellationToken);
            return (json["files"] as JsonArray ?? new JsonArray()).OfType<JsonObject>().Select(Clean).ToList();
        }

        public async Task<string> StartPageTokenAsync(IntegrationConnection connection, CancellationToken cancellationToken)
        {
            Require(connection);
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/changes/startPageToken"), "Google Drive", cancellationToken);
            return json["startPageToken"]?.ToString() ?? string.Empty;
        }

        public async Task<(List<JsonObject> files, string? nextToken)> ChangesAsync(
            IntegrationConnection connection,
            string pageToken,
            CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/changes?pageToken={Uri.EscapeDataString(pageToken)}&pageSize=100" +
                      $"&fields=newStartPageToken,nextPageToken,changes(removed,file({FileFields},trashed))";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Google Drive", cancellationToken);
            var files = (json["changes"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .Where(change => change["removed"]?.GetValue<bool>() != true)
                .Select(change => change["file"] as JsonObject)
                .Where(file => file != null && file["trashed"]?.GetValue<bool>() != true)
                .Select(file => Clean(file!))
                .ToList();
            return (files, json["nextPageToken"]?.ToString() ?? json["newStartPageToken"]?.ToString());
        }

        public static JsonObject Clean(JsonObject file) => new()
        {
            ["id"] = file["id"]?.ToString(),
            ["name"] = file["name"]?.ToString(),
            ["mimeType"] = file["mimeType"]?.ToString(),
            ["webViewLink"] = file["webViewLink"]?.ToString(),
            ["parents"] = file["parents"]?.DeepClone(),
            ["createdTime"] = file["createdTime"]?.ToString(),
            ["modifiedTime"] = file["modifiedTime"]?.ToString(),
            ["size"] = file["size"]?.ToString()
        };

        private void Require(IntegrationConnection connection) =>
            _google.RequireScope(connection, GoogleOAuthService.ScopeDriveFile, GoogleOAuthService.ScopeDriveFull);

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");
    }
}
