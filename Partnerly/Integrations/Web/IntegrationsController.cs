using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Automation.Abstractions;
using Partnerly.Controllers;
using Partnerly.Descriptors.Attributes;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.Google;
using Partnerly.Integrations.Telegram;
using Partnerly.Integrations.WhatsApp;
using Partnerly.Models.DataTable;

namespace Partnerly.Integrations.Web
{
    // Same right as Automation connections (plan D6): automation.connections.
    public class IntegrationsController : _BaseController
    {
        private const string GoogleStateCookie = "Partnerly.GoogleState";

        private readonly IIntegrationConnectionService _connections;
        private readonly IAutomationAccessService _access;
        private readonly IAutomationClock _clock;
        private readonly TelegramClient _telegram;
        private readonly GoogleOAuthService _googleOAuth;
        private readonly GoogleApiClient _google;
        private readonly WhatsAppClient _whatsApp;
        private readonly IDataProtector _stateProtector;

        public IntegrationsController(
            IEventBus eventBus,
            IUserService userService,
            ICurrentUserService currentUser,
            ILogService logService,
            IStringLocalizer<SharedResource> localizer,
            ISystemSettingsService systemSettingsService,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry,
            IIntegrationConnectionService connections,
            IAutomationAccessService access,
            IAutomationClock clock,
            TelegramClient telegram,
            GoogleOAuthService googleOAuth,
            GoogleApiClient google,
            WhatsAppClient whatsApp,
            IDataProtectionProvider dataProtection)
            : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _connections = connections;
            _access = access;
            _clock = clock;
            _telegram = telegram;
            _googleOAuth = googleOAuth;
            _google = google;
            _whatsApp = whatsApp;
            _stateProtector = dataProtection.CreateProtector("Integrations.GoogleState");
        }

        [HttpGet]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> Index(string? connected, string? error)
        {
            ViewData["Title"] = _localizer["Integrations"].Value;
            var list = await _connections.ListAsync(_access.CurrentUserId);
            var model = new IntegrationsViewModel
            {
                GoogleConfigured = _googleOAuth.IsConfigured,
                GoogleRedirectUri = GoogleRedirectUri(),
                Telegram = list.Where(c => c.Kind == IntegrationKinds.Telegram).Select(ToCard).ToList(),
                Google = list.Where(c => c.Kind == IntegrationKinds.Google).Select(ToCard).ToList(),
                WhatsApp = list.Where(c => c.Kind == IntegrationKinds.WhatsApp).Select(ToCard).ToList()
            };
            if (!string.IsNullOrEmpty(connected))
            {
                model.Flash = _localizer["IntegrationConnected"].Value;
            }
            else if (!string.IsNullOrEmpty(error))
            {
                model.Flash = _localizer["IntegrationGoogleFailed"].Value + " " + error;
                model.FlashIsError = true;
            }

            return View("~/Views/Integrations/Index.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ConnectTelegram(string? name, string botToken, string? defaultChatId)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                return Fail(_localizer["IntegrationTokenRequired"].Value);
            }

            JsonObject me;
            try
            {
                me = await _telegram.GetMeAsync(botToken.Trim());
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                return Fail(_localizer["IntegrationCheckFailed"].Value + " " + ex.Message);
            }

            var username = me["username"]?.ToString();
            var payload = new JsonObject
            {
                ["botToken"] = botToken.Trim(),
                ["botUsername"] = username,
                ["botName"] = me["first_name"]?.ToString()
            };
            if (!string.IsNullOrWhiteSpace(defaultChatId))
            {
                payload["defaultChatId"] = defaultChatId.Trim();
            }

            var connection = await _connections.CreateAsync(_access.CurrentUserId, IntegrationKinds.Telegram,
                string.IsNullOrWhiteSpace(name) ? "@" + username : name, payload);
            connection.MarkOk(_clock.UtcNow);
            await _connections.SaveAsync(connection);
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Telegram connection created");
            return Ok(new { success = true, message = _localizer["IntegrationConnected"].Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> UpdateTelegramChat(Guid id, string? defaultChatId)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Telegram);
            if (connection == null)
            {
                return NotFound();
            }

            connection.Set("defaultChatId", string.IsNullOrWhiteSpace(defaultChatId) ? null : defaultChatId.Trim());
            await _connections.SaveAsync(connection);
            return Ok(new { success = true, message = _localizer["AutomationSaved"].Value });
        }

        // Chats that wrote to the bot recently, so the user can copy a chat id.
        // getUpdates without offset does not confirm updates, so the poller still sees them.
        [HttpGet]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> TelegramChats(Guid id)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Telegram);
            if (connection == null)
            {
                return NotFound();
            }

            try
            {
                var updates = await _telegram.GetUpdatesAsync(connection.GetString("botToken") ?? string.Empty, null, 100);
                var chats = updates.OfType<JsonObject>()
                    .Select(u => u["message"] as JsonObject)
                    .Where(m => m != null)
                    .Select(m => TelegramPoller.ToPayload(m!))
                    .GroupBy(p => p["chatId"]?.ToString())
                    .Select(g => new
                    {
                        chatId = g.Key,
                        type = g.Last()["chatType"]?.ToString(),
                        name = g.Last()["fromName"]?.ToString(),
                        username = g.Last()["fromUsername"]?.ToString(),
                        text = g.Last()["text"]?.ToString()
                    })
                    .ToList();
                return Ok(new { success = true, chats });
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                return Fail(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public IActionResult ConnectGoogle(string? name, bool sheets, bool gmail, bool drive, bool driveFull)
        {
            if (!_googleOAuth.IsConfigured)
            {
                return Fail(_localizer["IntegrationGoogleNotConfigured"].Value);
            }

            if (!sheets && !gmail && !drive)
            {
                return Fail(_localizer["IntegrationGoogleChooseService"].Value);
            }

            // State = protected {user, nonce, options}; the nonce is also in a
            // short-lived cookie so the callback must come from this browser.
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var state = _stateProtector.Protect(new JsonObject
            {
                ["user"] = _access.CurrentUserId.ToString(),
                ["nonce"] = nonce,
                ["name"] = name?.Trim(),
                ["exp"] = _clock.UtcNow.AddMinutes(10).ToString("O"),
                ["sheets"] = sheets,
                ["gmail"] = gmail,
                ["drive"] = drive,
                ["driveFull"] = driveFull
            }.ToJsonString());
            Response.Cookies.Append(GoogleStateCookie, nonce, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10)
            });

            var scopes = GoogleOAuthService.ScopesFor(sheets, gmail, drive, driveFull);
            return Ok(new { success = true, redirect = _googleOAuth.BuildConsentUrl(GoogleRedirectUri(), state, scopes) });
        }

        [HttpGet("/Integrations/Google/Callback")]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> GoogleCallback(string? code, string? state, string? error)
        {
            var nonce = Request.Cookies[GoogleStateCookie];
            Response.Cookies.Delete(GoogleStateCookie);
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return RedirectToAction(nameof(Index), new { error = error ?? "cancelled" });
            }

            JsonObject? data;
            try
            {
                data = JsonNode.Parse(_stateProtector.Unprotect(state)) as JsonObject;
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
                var tokens = await _googleOAuth.ExchangeCodeAsync(code, GoogleRedirectUri());
                if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
                {
                    return RedirectToAction(nameof(Index), new { error = "no_refresh_token" });
                }

                var email = await _googleOAuth.GetEmailAsync(tokens.AccessToken);
                var name = data["name"]?.ToString();
                var connection = await _connections.CreateAsync(
                    _access.CurrentUserId,
                    IntegrationKinds.Google,
                    string.IsNullOrWhiteSpace(name) ? email ?? "Google" : name,
                    new JsonObject { ["email"] = email });
                GoogleApiClient.StoreTokens(connection, tokens, _clock.UtcNow);
                connection.MarkOk(_clock.UtcNow);
                await _connections.SaveAsync(connection);
                await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Google connection created");
                return RedirectToAction(nameof(Index), new { connected = "google" });
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                return RedirectToAction(nameof(Index), new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ConnectWhatsApp(string? name, string accessToken, string phoneNumberId, string appSecret)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(appSecret))
            {
                return Fail(_localizer["IntegrationWhatsAppFieldsRequired"].Value);
            }

            var payload = new JsonObject
            {
                ["accessToken"] = accessToken.Trim(),
                ["phoneNumberId"] = phoneNumberId.Trim(),
                ["appSecret"] = appSecret.Trim(),
                ["verifyToken"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()
            };
            var probe = new IntegrationConnection(new Automation.Persistence.Entities.AutomationConnection { Kind = IntegrationKinds.WhatsApp }, payload);
            JsonObject phone;
            try
            {
                phone = await _whatsApp.GetPhoneNumberAsync(probe);
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                return Fail(_localizer["IntegrationCheckFailed"].Value + " " + ex.Message);
            }

            payload["displayPhone"] = phone["display_phone_number"]?.ToString();
            payload["verifiedName"] = phone["verified_name"]?.ToString();
            var connection = await _connections.CreateAsync(_access.CurrentUserId, IntegrationKinds.WhatsApp,
                string.IsNullOrWhiteSpace(name) ? payload["displayPhone"]?.ToString() ?? "WhatsApp" : name, payload);
            connection.MarkOk(_clock.UtcNow);
            await _connections.SaveAsync(connection);
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "WhatsApp connection created");
            return Ok(new { success = true, message = _localizer["IntegrationConnected"].Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> Test(Guid id)
        {
            var connection = await _connections.GetAsync(id);
            if (connection == null || connection.OwnerUserId != _access.CurrentUserId)
            {
                return NotFound();
            }

            try
            {
                string detail;
                switch (connection.Kind)
                {
                    case IntegrationKinds.Telegram:
                        var me = await _telegram.GetMeAsync(connection.GetString("botToken") ?? string.Empty);
                        detail = "@" + me["username"];
                        break;
                    case IntegrationKinds.Google:
                        var info = await _google.SendAsync(connection,
                            () => new HttpRequestMessage(HttpMethod.Get, GoogleOAuthService.UserInfoUrl), "Google", HttpContext.RequestAborted);
                        detail = info["email"]?.ToString() ?? "Google";
                        break;
                    default:
                        var phone = await _whatsApp.GetPhoneNumberAsync(connection);
                        detail = phone["display_phone_number"]?.ToString() ?? "WhatsApp";
                        break;
                }

                connection.MarkOk(_clock.UtcNow);
                await _connections.SaveAsync(connection);
                return Ok(new { success = true, message = _localizer["IntegrationCheckOk"].Value + " " + detail });
            }
            catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
            {
                connection.MarkFailed(_clock.UtcNow, ex.Message);
                await _connections.SaveAsync(connection);
                return Fail(_localizer["IntegrationCheckFailed"].Value + " " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _connections.DeleteAsync(id, _access.CurrentUserId, _access.CanManage);
            if (!deleted)
            {
                return NotFound();
            }

            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Integration connection deleted");
            return Ok(new { success = true, message = _localizer["Deleted"].Value });
        }

        private IntegrationCardItem ToCard(IntegrationConnection connection)
        {
            var card = new IntegrationCardItem
            {
                Id = connection.Id,
                Name = connection.Name,
                Kind = connection.Kind,
                Status = connection.Status,
                CreatedDate = connection.Entity.CreatedDate
            };
            switch (connection.Kind)
            {
                case IntegrationKinds.Telegram:
                    card.Detail = "@" + connection.GetString("botUsername");
                    card.DefaultChatId = connection.GetString("defaultChatId");
                    break;
                case IntegrationKinds.Google:
                    card.Detail = connection.GetString("email");
                    if (GoogleApiClient.HasScope(connection, GoogleOAuthService.ScopeSheets)) card.Badges.Add("Sheets");
                    if (GoogleApiClient.HasScope(connection, GoogleOAuthService.ScopeGmailSend)) card.Badges.Add("Gmail");
                    if (GoogleApiClient.HasScope(connection, GoogleOAuthService.ScopeDriveFull)) card.Badges.Add("Drive (full)");
                    else if (GoogleApiClient.HasScope(connection, GoogleOAuthService.ScopeDriveFile)) card.Badges.Add("Drive");
                    break;
                case IntegrationKinds.WhatsApp:
                    card.Detail = connection.GetString("displayPhone");
                    card.WebhookUrl = $"{Request.Scheme}://{Request.Host}/Integrations/WhatsApp/Webhook/{connection.Id}";
                    card.VerifyToken = connection.GetString("verifyToken");
                    break;
            }

            return card;
        }

        private string GoogleRedirectUri() => $"{Request.Scheme}://{Request.Host}/Integrations/Google/Callback";

        private IActionResult Fail(string message) => BadRequest(new { success = false, message });
    }
}
