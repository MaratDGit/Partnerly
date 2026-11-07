using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.GitHub
{
    // GitHub OAuth App (web flow). Client from Authentication:GitHub:ClientId / ClientSecret
    // (user-secrets / secrets.json). OAuth App tokens do not expire and have no refresh token,
    // so a revoked token simply shows up as 401 and the connection asks to be reconnected.
    public class GitHubOAuthService
    {
        public const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
        public const string TokenUrl = "https://github.com/login/oauth/access_token";

        public const string ScopeRepo = "repo";
        public const string ScopePublicRepo = "public_repo";
        public const string ScopeWorkflow = "workflow";

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _http;

        public GitHubOAuthService(IConfiguration configuration, IHttpClientFactory http)
        {
            _configuration = configuration;
            _http = http;
        }

        private string ClientId => _configuration["Authentication:GitHub:ClientId"] ?? string.Empty;
        private string ClientSecret => _configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        public static IReadOnlyList<string> ScopesFor(bool privateRepos, bool workflows)
        {
            var scopes = new List<string> { "read:user", privateRepos ? ScopeRepo : ScopePublicRepo };
            if (workflows)
            {
                scopes.Add(ScopeWorkflow);
            }

            return scopes;
        }

        public string BuildConsentUrl(string redirectUri, string state, IEnumerable<string> scopes)
        {
            var query = new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["redirect_uri"] = redirectUri,
                ["scope"] = string.Join(" ", scopes),
                ["state"] = state,
                ["allow_signup"] = "false"
            };
            return AuthorizeUrl + "?" + string.Join("&", query.Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)));
        }

        public async Task<GitHubTokens> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                throw new IntegrationException("GitHub client is not configured (Authentication:GitHub).", retryable: false);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["client_secret"] = ClientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri
                })
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await IntegrationHttp.Create(_http).SendAsync(request, cancellationToken);
            var json = await IntegrationHttp.ReadJsonAsync(response, "GitHub", cancellationToken);

            // GitHub answers 200 with {"error": "bad_verification_code", ...} for a wrong or reused code.
            if (json["error"] != null)
            {
                throw new IntegrationException("GitHub: " + (json["error_description"] ?? json["error"]), retryable: false);
            }

            var token = json["access_token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new IntegrationException("GitHub did not return an access token.", retryable: false);
            }

            return new GitHubTokens { AccessToken = token, Scope = json["scope"]?.ToString() };
        }
    }

    public class GitHubTokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? Scope { get; set; }
    }
}
