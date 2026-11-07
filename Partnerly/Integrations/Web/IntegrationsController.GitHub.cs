using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.GitHub;

namespace Partnerly.Integrations.Web
{
    // Connecting a GitHub account: the "Connect GitHub" button (OAuth App) or a personal access token.
    public partial class IntegrationsController
    {
        private const string GitHubStateCookie = "Partnerly.GitHubState";

        private GitHubOAuthService GitHubOAuth => HttpContext.RequestServices.GetRequiredService<GitHubOAuthService>();

        private GitHubApiClient GitHubApi => HttpContext.RequestServices.GetRequiredService<GitHubApiClient>();

        private IDataProtector GitHubStateProtector =>
            HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("Integrations.GitHubState");

        private string GitHubRedirectUri() => $"{Request.Scheme}://{Request.Host}/Integrations/GitHub/Callback";

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public IActionResult ConnectGitHub(string? name, bool privateRepos, bool workflows)
        {
            if (!GitHubOAuth.IsConfigured)
            {
                return Fail(_localizer["IntegrationGitHubNotConfigured"].Value);
            }

            // Same state scheme as Google and Outlook: protected {user, nonce, name} plus the nonce in a short-lived cookie.
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var state = GitHubStateProtector.Protect(new JsonObject
            {
                ["user"] = _access.CurrentUserId.ToString(),
                ["nonce"] = nonce,
                ["name"] = name?.Trim(),
                ["exp"] = _clock.UtcNow.AddMinutes(10).ToString("O")
            }.ToJsonString());
            Response.Cookies.Append(GitHubStateCookie, nonce, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10)
            });

            var scopes = GitHubOAuthService.ScopesFor(privateRepos, workflows);
            return Ok(new { success = true, redirect = GitHubOAuth.BuildConsentUrl(GitHubRedirectUri(), state, scopes) });
        }

        [HttpGet("/Integrations/GitHub/Callback")]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> GitHubCallback(string? code, string? state, string? error)
        {
            var nonce = Request.Cookies[GitHubStateCookie];
            Response.Cookies.Delete(GitHubStateCookie);
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return RedirectToAction(nameof(Index), new { error = error ?? "cancelled" });
            }

            JsonObject? data;
            try
            {
                data = JsonNode.Parse(GitHubStateProtector.Unprotect(state)) as JsonObject;
            }
            catch
            {
                data = null;
            }

            if (data == null
                || data["user"]?.ToString() != _access.CurrentUserId.ToString()
                || string.IsNullOrEmpty(nonce)
                || data["nonce"]?.ToString() != nonce
                || !DateTime.TryParse(data["exp"]?.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var exp)
                || exp < _clock.UtcNow)
            {
                return RedirectToAction(nameof(Index), new { error = "state" });
            }

            try
            {
                var tokens = await GitHubOAuth.ExchangeCodeAsync(code, GitHubRedirectUri());
                await SaveGitHubConnectionAsync(data["name"]?.ToString(), tokens.AccessToken, tokens.Scope);
                return RedirectToAction(nameof(Index), new { connected = "github" });
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                return RedirectToAction(nameof(Index), new { error = ex.Message });
            }
        }

        // The other way in: a token made at github.com/settings/tokens, like the Telegram bot token.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ConnectGitHubToken(string? name, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Fail(_localizer["IntegrationTokenRequired"].Value);
            }

            try
            {
                await SaveGitHubConnectionAsync(name, token.Trim(), null);
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                return Fail(_localizer["IntegrationCheckFailed"].Value + " " + ex.Message);
            }

            return Ok(new { success = true, message = _localizer["IntegrationConnected"].Value });
        }

        private async Task SaveGitHubConnectionAsync(string? name, string token, string? scope)
        {
            var probe = new IntegrationConnection(new Partnerly.Automation.Persistence.Entities.AutomationConnection(), new JsonObject());
            GitHubApiClient.StoreToken(probe, token, scope, null);
            var me = await GitHubApi.SendObjectAsync(probe, HttpMethod.Get, "/user", null, HttpContext.RequestAborted);
            var login = me["login"]?.ToString() ?? string.Empty;

            var connection = await _connections.CreateAsync(
                _access.CurrentUserId,
                IntegrationKinds.GitHub,
                string.IsNullOrWhiteSpace(name) ? (login.Length > 0 ? login : "GitHub") : name.Trim(),
                new JsonObject { ["login"] = login });
            GitHubApiClient.StoreToken(connection, token, scope, login);
            connection.MarkOk(_clock.UtcNow);
            await _connections.SaveAsync(connection);
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "GitHub connection created");
        }
    }
}
