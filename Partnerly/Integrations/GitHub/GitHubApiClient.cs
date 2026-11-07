using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.GitHub
{
    // A card setting that is missing or wrong: it fails the card, it says nothing about the connection.
    public sealed class GitHubSettingException : IntegrationException
    {
        public GitHubSettingException(string message, bool retryable = false) : base(message, retryable) { }
    }

    // Calls to the GitHub REST API (api.github.com) with the connection's token.
    // The token never leaves this class: it is not logged and not part of any step output.
    public class GitHubApiClient
    {
        public const string ApiBase = "https://api.github.com";

        private static readonly Regex RepoPattern = new(@"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _http;

        public GitHubApiClient(IHttpClientFactory http)
        {
            _http = http;
        }

        public static void StoreToken(IntegrationConnection connection, string token, string? scope, string? login)
        {
            connection.Set("accessToken", token);
            connection.Set("scope", scope);
            connection.Set("login", login);
        }

        public static bool HasScope(IntegrationConnection connection, string scope) =>
            (connection.GetString("scope") ?? string.Empty)
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(item => string.Equals(item, scope, StringComparison.OrdinalIgnoreCase));

        // "owner/name", a github.com link or a clone address, all become "owner/name".
        public static string Repo(string? input)
        {
            var text = (input ?? string.Empty).Trim();
            foreach (var prefix in new[] { "https://github.com/", "http://github.com/", "git@github.com:", "github.com/" })
            {
                if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    text = text[prefix.Length..];
                    break;
                }
            }

            text = text.TrimEnd('/');
            if (text.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                text = text[..^4];
            }

            if (!RepoPattern.IsMatch(text) || text.Contains(".."))
            {
                throw new GitHubSettingException("Repository must look like owner/name, for example octocat/hello-world.", retryable: false);
            }

            return text;
        }

        // A path under api.github.com only: no full addresses, no way to leave the API host.
        public static string SafePath(string? input)
        {
            var path = (input ?? string.Empty).Trim();
            if (!path.StartsWith('/') || path.StartsWith("//") || path.Contains("://") || path.Contains("..")
                || path.Any(ch => char.IsWhiteSpace(ch) || ch < 32))
            {
                throw new GitHubSettingException("Path must start with / and stay on api.github.com, for example /user/repos.", retryable: false);
            }

            return path;
        }

        public static string Segment(string value) => Uri.EscapeDataString(value.Trim());

        // Path of a file inside a repository: keep the slashes, escape every part.
        public static string FilePath(string value) =>
            string.Join("/", value.Trim().Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

        public async Task<JsonNode?> SendAsync(
            IntegrationConnection connection,
            HttpMethod method,
            string path,
            JsonNode? body,
            CancellationToken cancellationToken)
        {
            var token = connection.GetString("accessToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new IntegrationException("GitHub connection has no access token. Reconnect GitHub.", retryable: false);
            }

            using var request = new HttpRequestMessage(method, ApiBase + path);
            IntegrationHttp.Bearer(request, token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Partnerly", "1.0"));
            if (body != null)
            {
                request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            }

            using var response = await IntegrationHttp.Create(_http).SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new JsonObject { ["done"] = true };
                }

                try { return JsonNode.Parse(text); }
                catch (System.Text.Json.JsonException) { return new JsonObject { ["raw"] = text.Length > 4000 ? text[..4000] : text }; }
            }

            throw Failure(response, text);
        }

        public async Task<JsonObject> SendObjectAsync(
            IntegrationConnection connection,
            HttpMethod method,
            string path,
            JsonNode? body,
            CancellationToken cancellationToken)
        {
            var node = await SendAsync(connection, method, path, body, cancellationToken);
            return node as JsonObject ?? new JsonObject { ["items"] = node };
        }

        // One page of a list (GitHub caps a page at 100 items).
        public async Task<JsonArray> ListAsync(
            IntegrationConnection connection,
            string path,
            int max,
            CancellationToken cancellationToken)
        {
            var perPage = Math.Clamp(max, 1, 100);
            var separator = path.Contains('?') ? "&" : "?";
            var node = await SendAsync(connection, HttpMethod.Get, $"{path}{separator}per_page={perPage}", null, cancellationToken);
            if (node is JsonArray array)
            {
                return array;
            }

            // Some lists are wrapped: {"total_count": n, "workflow_runs": [...]}.
            if (node is JsonObject wrapper)
            {
                foreach (var item in wrapper)
                {
                    if (item.Value is JsonArray inner)
                    {
                        return inner;
                    }
                }
            }

            return new JsonArray();
        }

        private static IntegrationException Failure(HttpResponseMessage response, string text)
        {
            string detail = response.ReasonPhrase ?? "error";
            try
            {
                if (JsonNode.Parse(text) is JsonObject json && json["message"] != null)
                {
                    detail = json["message"]!.ToString();
                    if (json["errors"] is JsonArray errors && errors.Count > 0)
                    {
                        var first = errors[0]?.ToString() ?? string.Empty;
                        detail += " (" + (first.Length > 160 ? first[..160] : first) + ")";
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // keep the reason phrase
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new IntegrationException("GitHub access was revoked or the token is wrong. Reconnect GitHub.", retryable: false);
            }

            var limited = response.StatusCode == HttpStatusCode.TooManyRequests
                || (response.StatusCode == HttpStatusCode.Forbidden
                    && response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining)
                    && remaining.FirstOrDefault() == "0");
            var retryable = limited || (int)response.StatusCode >= 500;
            return new IntegrationException($"GitHub: {(int)response.StatusCode} {detail}", retryable);
        }
    }
}
