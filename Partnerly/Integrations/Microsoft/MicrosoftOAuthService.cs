using System.Text.Json.Nodes;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Microsoft
{
    // Microsoft identity platform (v2.0) for Outlook mail / calendar through
    // Microsoft Graph. Client from Authentication:Microsoft:* (user-secrets /
    // secrets.json); TenantId defaults to "common" (work and personal accounts).
    public class MicrosoftOAuthService
    {
        public const string GraphBase = "https://graph.microsoft.com/v1.0";

        public const string ScopeMailSend = "Mail.Send";
        public const string ScopeMailRead = "Mail.Read";
        public const string ScopeCalendar = "Calendars.ReadWrite";

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _http;

        public MicrosoftOAuthService(IConfiguration configuration, IHttpClientFactory http)
        {
            _configuration = configuration;
            _http = http;
        }

        private string ClientId => _configuration["Authentication:Microsoft:ClientId"] ?? string.Empty;
        private string ClientSecret => _configuration["Authentication:Microsoft:ClientSecret"] ?? string.Empty;

        private string Tenant
        {
            get
            {
                var tenant = (_configuration["Authentication:Microsoft:TenantId"] ?? string.Empty).Trim();
                // The tenant goes into the URL path: letters, digits, '-' and '.' only.
                return tenant.Length > 0 && tenant.All(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '.') ? tenant : "common";
            }
        }

        private string AuthorizeUrl => $"https://login.microsoftonline.com/{Tenant}/oauth2/v2.0/authorize";
        private string TokenUrl => $"https://login.microsoftonline.com/{Tenant}/oauth2/v2.0/token";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        public static IReadOnlyList<string> ScopesFor(bool mail, bool calendar)
        {
            var scopes = new List<string> { "openid", "email", "offline_access", "User.Read" };
            if (mail)
            {
                scopes.Add(ScopeMailSend);
                scopes.Add(ScopeMailRead);
            }

            if (calendar)
            {
                scopes.Add(ScopeCalendar);
            }

            return scopes;
        }

        public string BuildConsentUrl(string redirectUri, string state, IEnumerable<string> scopes)
        {
            var query = new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUri,
                ["response_mode"] = "query",
                ["scope"] = string.Join(" ", scopes),
                ["prompt"] = "select_account",
                ["state"] = state
            };
            return AuthorizeUrl + "?" + string.Join("&", query.Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)));
        }

        public Task<MicrosoftTokens> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
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

        public Task<MicrosoftTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
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
            using var request = new HttpRequestMessage(HttpMethod.Get, GraphBase + "/me?$select=mail,userPrincipalName");
            IntegrationHttp.Bearer(request, accessToken);
            using var response = await IntegrationHttp.Create(_http).SendAsync(request, cancellationToken);
            var json = await IntegrationHttp.ReadJsonAsync(response, "Microsoft", cancellationToken);
            return json["mail"]?.ToString() ?? json["userPrincipalName"]?.ToString();
        }

        private async Task<MicrosoftTokens> TokenRequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                throw new IntegrationException("Microsoft client is not configured (Authentication:Microsoft).", retryable: false);
            }

            using var response = await IntegrationHttp.Create(_http)
                .PostAsync(TokenUrl, new FormUrlEncodedContent(form), cancellationToken);
            JsonObject json;
            try
            {
                json = await IntegrationHttp.ReadJsonAsync(response, "Microsoft", cancellationToken);
            }
            catch (IntegrationException ex) when (ex.Message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase)
                                                  || ex.Message.Contains("interaction_required", StringComparison.OrdinalIgnoreCase))
            {
                throw new IntegrationException("Microsoft access was revoked or expired. Reconnect Outlook.", retryable: false);
            }

            return new MicrosoftTokens
            {
                AccessToken = json["access_token"]?.ToString() ?? string.Empty,
                RefreshToken = json["refresh_token"]?.ToString(),
                ExpiresIn = int.TryParse(json["expires_in"]?.ToString(), out var seconds) ? seconds : 3600,
                Scope = json["scope"]?.ToString()
            };
        }
    }

    public class MicrosoftTokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Scope { get; set; }
    }
}
