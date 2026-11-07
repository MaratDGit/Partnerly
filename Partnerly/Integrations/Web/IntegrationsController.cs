using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Automation.Abstractions;
using Partnerly.Computer;
using Partnerly.Controllers;
using Partnerly.Descriptors.Attributes;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Integrations.Common;
using Partnerly.Integrations.GitHub;
using Partnerly.Integrations.Google;
using Partnerly.Integrations.Microsoft;
using Partnerly.Integrations.Telegram;
using Partnerly.Integrations.WhatsApp;
using Partnerly.Models.DataTable;

namespace Partnerly.Integrations.Web
{
    // Same right as Automation connections (plan D6): automation.connections.
    public partial class IntegrationsController : _BaseController
    {
        private const string GoogleStateCookie = "Partnerly.GoogleState";
        private const string MicrosoftStateCookie = "Partnerly.MicrosoftState";

        private readonly IIntegrationConnectionService _connections;
        private readonly IAutomationAccessService _access;
        private readonly IAutomationClock _clock;
        private readonly TelegramClient _telegram;
        private readonly GoogleOAuthService _googleOAuth;
        private readonly GoogleApiClient _google;
        private readonly WhatsAppClient _whatsApp;
        private readonly MicrosoftOAuthService _microsoftOAuth;
        private readonly MicrosoftApiClient _microsoft;
        private readonly IComputerGateway _computers;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IDataProtector _stateProtector;
        private readonly IDataProtector _microsoftStateProtector;

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
            MicrosoftOAuthService microsoftOAuth,
            MicrosoftApiClient microsoft,
            IComputerGateway computers,
            IConfiguration configuration,
            IWebHostEnvironment environment,
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
            _microsoftOAuth = microsoftOAuth;
            _microsoft = microsoft;
            _computers = computers;
            _configuration = configuration;
            _environment = environment;
            _stateProtector = dataProtection.CreateProtector("Integrations.GoogleState");
            _microsoftStateProtector = dataProtection.CreateProtector("Integrations.MicrosoftState");
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
                GitHubConfigured = GitHubOAuth.IsConfigured,
                GitHubRedirectUri = GitHubRedirectUri(),
                GitHub = list.Where(c => c.Kind == IntegrationKinds.GitHub).Select(ToCard).ToList(),
                MicrosoftConfigured = _microsoftOAuth.IsConfigured,
                MicrosoftRedirectUri = MicrosoftRedirectUri(),
                Microsoft = list.Where(c => c.Kind == IntegrationKinds.Microsoft).Select(ToCard).ToList(),
                Telegram = list.Where(c => c.Kind == IntegrationKinds.Telegram).Select(ToCard).ToList(),
                Google = list.Where(c => c.Kind == IntegrationKinds.Google).Select(ToCard).ToList(),
                WhatsApp = list.Where(c => c.Kind == IntegrationKinds.WhatsApp).Select(ToCard).ToList(),
                Computers = list.Where(c => c.Kind == IntegrationKinds.Computer).Select(ToCard).ToList(),
                AgentAvailable = ComputerAgentPackage.FindExe(AgentFolder()) != null
            };
            if (!string.IsNullOrEmpty(connected))
            {
                model.Flash = _localizer["IntegrationConnected"].Value;
            }
            else if (!string.IsNullOrEmpty(error))
            {
                model.Flash = _localizer["IntegrationConnectFailed"].Value + " " + error;
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

        // Polled by the "add computer" window: turns green as soon as the program on the PC connects.
        [HttpGet]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ComputerStatus(Guid id)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Computer);
            if (connection == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                success = true,
                online = _computers.IsOnline(connection.Id),
                machine = connection.GetString("machine"),
                version = connection.GetString("agentVersion")
            });
        }

        // What the program on the PC says it allows (folders, programs, scripts): the card editor
        // offers them in a dropdown. A hint only - the PC itself decides at run time.
        [HttpGet]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ComputerSettings(Guid id)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Computer);
            if (connection == null)
            {
                return NotFound();
            }

            JsonNode? settings = null;
            try { settings = JsonNode.Parse(connection.GetString("settings") ?? "null"); }
            catch (System.Text.Json.JsonException) { /* an old or damaged hint is just ignored */ }

            return Ok(new
            {
                success = true,
                online = _computers.IsOnline(connection.Id),
                machine = connection.GetString("machine"),
                known = settings != null,
                folders = settings?["folders"],
                programs = settings?["programs"],
                scripts = settings?["scripts"],
                repos = settings?["repos"],
                confirm = settings?["confirm"]
            });
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
                return Ok(new { success = true, chats, defaultChatId = connection.GetString("defaultChatId") });
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
        public IActionResult ConnectMicrosoft(string? name, bool mail, bool calendar)
        {
            if (!_microsoftOAuth.IsConfigured)
            {
                return Fail(_localizer["IntegrationMicrosoftNotConfigured"].Value);
            }

            if (!mail && !calendar)
            {
                return Fail(_localizer["IntegrationMicrosoftChooseService"].Value);
            }

            // Same state scheme as Google: protected {user, nonce, options}
            // plus the nonce in a short-lived cookie of this browser.
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var state = _microsoftStateProtector.Protect(new JsonObject
            {
                ["user"] = _access.CurrentUserId.ToString(),
                ["nonce"] = nonce,
                ["name"] = name?.Trim(),
                ["exp"] = _clock.UtcNow.AddMinutes(10).ToString("O")
            }.ToJsonString());
            Response.Cookies.Append(MicrosoftStateCookie, nonce, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10)
            });

            var scopes = MicrosoftOAuthService.ScopesFor(mail, calendar);
            return Ok(new { success = true, redirect = _microsoftOAuth.BuildConsentUrl(MicrosoftRedirectUri(), state, scopes) });
        }

        [HttpGet("/Integrations/Microsoft/Callback")]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> MicrosoftCallback(string? code, string? state, string? error)
        {
            var nonce = Request.Cookies[MicrosoftStateCookie];
            Response.Cookies.Delete(MicrosoftStateCookie);
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return RedirectToAction(nameof(Index), new { error = error ?? "cancelled" });
            }

            JsonObject? data;
            try
            {
                data = JsonNode.Parse(_microsoftStateProtector.Unprotect(state)) as JsonObject;
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
                var tokens = await _microsoftOAuth.ExchangeCodeAsync(code, MicrosoftRedirectUri());
                if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
                {
                    return RedirectToAction(nameof(Index), new { error = "no_refresh_token" });
                }

                var email = await _microsoftOAuth.GetEmailAsync(tokens.AccessToken);
                var name = data["name"]?.ToString();
                var connection = await _connections.CreateAsync(
                    _access.CurrentUserId,
                    IntegrationKinds.Microsoft,
                    string.IsNullOrWhiteSpace(name) ? email ?? "Outlook" : name,
                    new JsonObject { ["email"] = email });
                MicrosoftApiClient.StoreTokens(connection, tokens, _clock.UtcNow);
                connection.MarkOk(_clock.UtcNow);
                await _connections.SaveAsync(connection);
                await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Outlook connection created");
                return RedirectToAction(nameof(Index), new { connected = "microsoft" });
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

        // A computer is paired with a one-time token that the Partnerly Agent program on the PC
        // uses to connect. The token is shown once; only its hash is kept.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ConnectComputer(string? name)
        {
            var connection = await _connections.CreateAsync(_access.CurrentUserId, IntegrationKinds.Computer,
                string.IsNullOrWhiteSpace(name) ? _localizer["ComputerDefaultName"].Value : name.Trim(), new JsonObject());
            var (token, hash) = ComputerToken.Create(connection.Id);
            connection.Set("tokenHash", hash);
            connection.MarkOk(_clock.UtcNow);
            await _connections.SaveAsync(connection);
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Computer connection created");
            return Ok(new { success = true, message = _localizer["IntegrationConnected"].Value, token, serverUrl = $"{Request.Scheme}://{Request.Host}", connectionId = connection.Id });
        }

        // The program itself, to be asked for the first time (the site address and token are typed in).
        [HttpGet]
        [Authorize(Policy = "Permission:automation.connections")]
        public IActionResult DownloadAgent()
        {
            var exe = ComputerAgentPackage.FindExe(AgentFolder());
            return exe == null
                ? NotFound(new { success = false, message = _localizer["ComputerAgentMissing"].Value })
                : File(ComputerAgentPackage.Build(exe, null, null), "application/zip", "Partnerly-Agent.zip");
        }

        // The program plus config.json with the address and token of a computer that was just added.
        // The site never stores the token, so it is checked against the hash before it is packed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> DownloadAgentWithSettings(Guid id, string token)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Computer);
            if (connection == null || !ComputerToken.TryParse(token, out var tokenId, out var secret)
                || tokenId != connection.Id || !ComputerToken.Matches(secret, connection.GetString("tokenHash")))
            {
                return NotFound();
            }

            var exe = ComputerAgentPackage.FindExe(AgentFolder());
            return exe == null
                ? NotFound(new { success = false, message = _localizer["ComputerAgentMissing"].Value })
                : File(ComputerAgentPackage.Build(exe, $"{Request.Scheme}://{Request.Host}", token.Trim()), "application/zip", "Partnerly-Agent.zip");
        }

        private string AgentFolder() =>
            _configuration["Agent:DownloadFolder"] is { Length: > 0 } folder ? folder : ComputerAgentPackage.DefaultFolder(_environment.ContentRootPath);

        private static readonly string[] PickKinds = { "file", "save", "folder", "repo", "program", "programname", "script", "zip", "zipsave" };

        // The editor's "Choose" button: the program on the PC opens a Windows file / folder dialog, the person picks
        // there, and only the chosen path comes back. Waits while the person decides.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ComputerPick(Guid id, string kind, string? title)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Computer);
            if (connection == null || !PickKinds.Contains(kind, StringComparer.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            try
            {
                var response = await _computers.SendAsync(
                    connection.Id,
                    "pick_path",
                    new JsonObject { ["kind"] = kind.ToLowerInvariant(), ["title"] = title ?? string.Empty },
                    TimeSpan.FromMinutes(3),
                    HttpContext.RequestAborted);
                if (!response.Ok)
                {
                    var failure = response.ErrorKey != null ? _localizer[response.ErrorKey] : null;
                    return Ok(new { success = false, message = failure is { ResourceNotFound: false } ? failure.Value : response.Error });
                }

                return Ok(new
                {
                    success = true,
                    cancelled = response.Data["cancelled"]?.GetValue<bool>() == true,
                    path = response.Data["path"]?.ToString(),
                    allowed = response.Data["allowed"]?.GetValue<bool>() == true
                });
            }
            catch (ComputerException ex)
            {
                var text = _localizer[ex.Key];
                return Ok(new { success = false, message = text.ResourceNotFound ? ex.Message : text.Value });
            }
        }

        // The editor's "Browse" window: what lies inside the folders the PC allows (folders, files, Git repositories).
        // The PC lists it, and refuses anything outside its allowed folders.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> ComputerBrowse(Guid id, string? path)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Computer);
            if (connection == null)
            {
                return NotFound();
            }

            try
            {
                var response = await _computers.SendAsync(
                    connection.Id,
                    "browse",
                    new JsonObject { ["path"] = path ?? string.Empty },
                    TimeSpan.FromSeconds(25),
                    HttpContext.RequestAborted);
                if (!response.Ok)
                {
                    var failure = response.ErrorKey != null ? _localizer[response.ErrorKey] : null;
                    return Ok(new { success = false, message = failure is { ResourceNotFound: false } ? failure.Value : response.Error });
                }

                return Content("{\"success\":true,\"data\":" + response.Data.ToJsonString() + "}", "application/json");
            }
            catch (ComputerException ex)
            {
                var text = _localizer[ex.Key];
                return Ok(new { success = false, message = text.ResourceNotFound ? ex.Message : text.Value });
            }
        }

        // A new token cuts off the program that used the old one.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:automation.connections")]
        public async Task<IActionResult> RenewComputerToken(Guid id)
        {
            var connection = await _connections.GetForOwnerAsync(id, _access.CurrentUserId, IntegrationKinds.Computer);
            if (connection == null)
            {
                return NotFound();
            }

            var (token, hash) = ComputerToken.Create(connection.Id);
            connection.Set("tokenHash", hash);
            await _connections.SaveAsync(connection);
            await _logService.CreateLogAsync("AC", LogTypeAttribute.Information, "Computer token renewed");
            return Ok(new { success = true, message = _localizer["IntegrationConnected"].Value, token, serverUrl = $"{Request.Scheme}://{Request.Host}", connectionId = connection.Id });
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
                    case IntegrationKinds.Microsoft:
                        var profile = await _microsoft.SendAsync(connection,
                            () => new HttpRequestMessage(HttpMethod.Get, MicrosoftOAuthService.GraphBase + "/me?$select=mail,userPrincipalName"),
                            HttpContext.RequestAborted);
                        detail = profile["mail"]?.ToString() ?? profile["userPrincipalName"]?.ToString() ?? "Outlook";
                        break;
                    case IntegrationKinds.GitHub:
                        var account = await GitHubApi.SendObjectAsync(connection, HttpMethod.Get, "/user", null, HttpContext.RequestAborted);
                        detail = account["login"]?.ToString() ?? "GitHub";
                        break;
                    case IntegrationKinds.Computer:
                        var pong = await _computers.SendAsync(connection.Id, "ping", new JsonObject(), TimeSpan.FromSeconds(15), HttpContext.RequestAborted);
                        detail = pong.Data["machine"]?.ToString() ?? connection.Name;
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
                case IntegrationKinds.Microsoft:
                    card.Detail = connection.GetString("email");
                    if (MicrosoftApiClient.HasScope(connection, MicrosoftOAuthService.ScopeMailSend)) card.Badges.Add("Mail");
                    if (MicrosoftApiClient.HasScope(connection, MicrosoftOAuthService.ScopeCalendar)) card.Badges.Add("Calendar");
                    break;
                case IntegrationKinds.GitHub:
                    card.Detail = connection.GetString("login");
                    card.Badges.Add(GitHubApiClient.HasScope(connection, GitHubOAuthService.ScopeRepo) ? "Private + public"
                        : GitHubApiClient.HasScope(connection, GitHubOAuthService.ScopePublicRepo) ? "Public"
                        : connection.GetString("scope") == null ? "Token" : "Limited");
                    if (GitHubApiClient.HasScope(connection, GitHubOAuthService.ScopeWorkflow)) card.Badges.Add("Workflows");
                    break;
                case IntegrationKinds.Computer:
                    card.Detail = string.Join(" · ", new[] { connection.GetString("machine"), connection.GetString("os") }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    card.IsOnline = _computers.IsOnline(connection.Id);
                    card.OfflineReason = connection.GetString("lastState") is "paused" or "uninstalled" ? connection.GetString("lastState") : "offline";
                    card.Badges.Add(card.IsOnline
                        ? _localizer["ComputerOnline"].Value
                        : card.OfflineReason switch
                        {
                            "paused" => _localizer["ComputerPaused"].Value,
                            "uninstalled" => _localizer["ComputerUninstalled"].Value,
                            _ => _localizer["ComputerOffline"].Value
                        });
                    break;
                case IntegrationKinds.WhatsApp:
                    card.Detail = connection.GetString("displayPhone");
                    card.WebhookUrl = $"{Request.Scheme}://{Request.Host}/Integrations/WhatsApp/Webhook/{connection.Id}";
                    card.VerifyToken = connection.GetString("verifyToken");
                    break;
            }

            return card;
        }

        private string MicrosoftRedirectUri() => $"{Request.Scheme}://{Request.Host}/Integrations/Microsoft/Callback";

        private string GoogleRedirectUri() => $"{Request.Scheme}://{Request.Host}/Integrations/Google/Callback";

        private IActionResult Fail(string message) => BadRequest(new { success = false, message });
    }
}
