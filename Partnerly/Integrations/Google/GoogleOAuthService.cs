using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google
{
    // Same Google Cloud client as sign-in (plan D4): Authentication:Google:*
    // from user-secrets / secrets.json. Only the redirect URI and scopes differ.
    public class GoogleOAuthService
    {
        public const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public const string TokenUrl = "https://oauth2.googleapis.com/token";
        public const string UserInfoUrl = "https://openidconnect.googleapis.com/v1/userinfo";

        public const string ScopeSheets = "https://www.googleapis.com/auth/spreadsheets";
        public const string ScopeGmailSend = "https://www.googleapis.com/auth/gmail.send";
        public const string ScopeGmailRead = "https://www.googleapis.com/auth/gmail.readonly";
        public const string ScopeDriveFile = "https://www.googleapis.com/auth/drive.file";
        public const string ScopeDriveFull = "https://www.googleapis.com/auth/drive";

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _http;

        public GoogleOAuthService(IConfiguration configuration, IHttpClientFactory http)
        {
            _configuration = configuration;
            _http = http;
        }

        private string ClientId => _configuration["Authentication:Google:ClientId"] ?? string.Empty;
        private string ClientSecret => _configuration["Authentication:Google:ClientSecret"] ?? string.Empty;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        public static IReadOnlyList<string> ScopesFor(bool sheets, bool gmail, bool drive, bool driveFull)
        {
            var scopes = new List<string> { "openid", "email" };
            if (sheets)
            {
                scopes.Add(ScopeSheets);
            }

            if (gmail)
            {
                scopes.Add(ScopeGmailSend);
                scopes.Add(ScopeGmailRead);
            }

            if (drive)
            {
                scopes.Add(driveFull ? ScopeDriveFull : ScopeDriveFile);
            }

            return scopes;
        }

        public string BuildConsentUrl(string redirectUri, string state, IEnumerable<string> scopes)
        {
            var query = new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = string.Join(" ", scopes),
                ["access_type"] = "offline",
                ["prompt"] = "consent",
                ["include_granted_scopes"] = "true",
                ["state"] = state
            };
            return AuthorizeUrl + "?" + string.Join("&", query.Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)));
        }

        public Task<GoogleTokens> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
        {
            return TokenRequestAsync(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }, cancellationToken);
        }

        public Task<GoogleTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            return TokenRequestAsync(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["grant_type"] = "refresh_token"
            }, cancellationToken);
        }

        public async Task<string?> GetEmailAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoUrl);
            IntegrationHttp.Bearer(request, accessToken);
            using var response = await IntegrationHttp.Create(_http).SendAsync(request, cancellationToken);
            var json = await IntegrationHttp.ReadJsonAsync(response, "Google", cancellationToken);
            return json["email"]?.ToString();
        }

        private async Task<GoogleTokens> TokenRequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                throw new IntegrationException("Google client is not configured (Authentication:Google).", retryable: false);
            }

            using var response = await IntegrationHttp.Create(_http)
                .PostAsync(TokenUrl, new FormUrlEncodedContent(form), cancellationToken);
            JsonObject json;
            try
            {
                json = await IntegrationHttp.ReadJsonAsync(response, "Google", cancellationToken);
            }
            catch (IntegrationException ex) when (ex.Message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
            {
                throw new IntegrationException("Google access was revoked or expired. Reconnect Google.", retryable: false);
            }

            return new GoogleTokens
            {
                AccessToken = json["access_token"]?.ToString() ?? string.Empty,
                RefreshToken = json["refresh_token"]?.ToString(),
                ExpiresIn = int.TryParse(json["expires_in"]?.ToString(), out var seconds) ? seconds : 3600,
                Scope = json["scope"]?.ToString()
            };
        }
    }

    public class GoogleTokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Scope { get; set; }
    }
}
