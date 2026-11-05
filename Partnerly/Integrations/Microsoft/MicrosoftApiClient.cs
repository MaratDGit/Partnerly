using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Microsoft
{
    // Bearer calls to Microsoft Graph with automatic refresh. Microsoft rotates
    // refresh tokens, so every refresh result is written back into the payload;
    // the caller (action base / polling service) saves it.
    public class MicrosoftApiClient
    {
        private readonly IHttpClientFactory _http;
        private readonly MicrosoftOAuthService _oauth;
        private readonly IAutomationClock _clock;

        public MicrosoftApiClient(IHttpClientFactory http, MicrosoftOAuthService oauth, IAutomationClock clock)
        {
            _http = http;
            _oauth = oauth;
            _clock = clock;
        }

        public static void StoreTokens(IntegrationConnection connection, MicrosoftTokens tokens, DateTime utcNow)
        {
            connection.Set("accessToken", tokens.AccessToken);
            connection.Set("expiresAt", utcNow.AddSeconds(tokens.ExpiresIn).ToString("O"));
            if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                connection.Set("refreshToken", tokens.RefreshToken);
            }

            if (!string.IsNullOrWhiteSpace(tokens.Scope))
            {
                connection.Set("scope", tokens.Scope);
            }
        }

        public static bool HasScope(IntegrationConnection connection, string scope) =>
            (connection.GetString("scope") ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(item => string.Equals(item, scope, StringComparison.OrdinalIgnoreCase));

        public void RequireScope(IntegrationConnection connection, params string[] anyOf)
        {
            if (!anyOf.Any(scope => HasScope(connection, scope)))
            {
                throw new IntegrationException("This Outlook connection has no access to this service. Reconnect Outlook and tick it.", retryable: false);
            }
        }

        public async Task<JsonObject> SendAsync(
            IntegrationConnection connection,
            Func<HttpRequestMessage> build,
            CancellationToken cancellationToken)
        {
            var token = await AccessTokenAsync(connection, force: false, cancellationToken);
            using (var request = build())
            {
                IntegrationHttp.Bearer(request, token);
                using var response = await IntegrationHttp.Create(_http).SendAsync(request, cancellationToken);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    return await IntegrationHttp.ReadJsonAsync(response, "Outlook", cancellationToken);
                }
            }

            // Token revoked early or clock skew: refresh once and retry.
            token = await AccessTokenAsync(connection, force: true, cancellationToken);
            using var retry = build();
            IntegrationHttp.Bearer(retry, token);
            using var second = await IntegrationHttp.Create(_http).SendAsync(retry, cancellationToken);
            return await IntegrationHttp.ReadJsonAsync(second, "Outlook", cancellationToken);
        }

        private async Task<string> AccessTokenAsync(IntegrationConnection connection, bool force, CancellationToken cancellationToken)
        {
            var access = connection.GetString("accessToken");
            var expires = DateTime.TryParse(connection.GetString("expiresAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
                ? at
                : DateTime.MinValue;
            if (!force && !string.IsNullOrWhiteSpace(access) && expires > _clock.UtcNow.AddSeconds(60))
            {
                return access;
            }

            var refresh = connection.GetString("refreshToken");
            if (string.IsNullOrWhiteSpace(refresh))
            {
                throw new IntegrationException("Outlook connection has no refresh token. Reconnect Outlook.", retryable: false);
            }

            var tokens = await _oauth.RefreshAsync(refresh, cancellationToken);
            StoreTokens(connection, tokens, _clock.UtcNow);
            return tokens.AccessToken;
        }
    }
}
