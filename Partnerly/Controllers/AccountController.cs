using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    public class AccountController : Controller
    {

        #region Services
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly ILogService _logService;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailSender _emailSender;
        private readonly IEmailConfirmationTokenService _tokenService;
        private readonly IEventBus _eventBus;
        private readonly IWebHostEnvironment _environment;
        private readonly ISystemSettingsService _systemSettingsService;
        #endregion
        #region Constructor
        public AccountController(
            IConfiguration config, 
            ILogger<AccountController> logger, 
            IUserService userService, 
            ILogService logService, 
            ICurrentUserService currentUser, 
            IEmailSender emailSender, 
            IEmailConfirmationTokenService tokenService, 
            IEventBus eventBus, 
            IWebHostEnvironment environment,
            ISystemSettingsService systemSettingsService)
        {
            _config = config;
            _logger = logger;
            _userService = userService;
            _logService = logService;
            _currentUser = currentUser;
            _emailSender = emailSender;
            _tokenService = tokenService;
            _eventBus = eventBus;
            _environment = environment;
            _systemSettingsService = systemSettingsService;
        }
        #endregion

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            await PopulateAuthSettingsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Register(string? reff = null, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            await PopulateAuthSettingsAsync();

            var model = new RegisterViewModel();
            if (reff != null)
            {
                model.ReferrerCode = reff;
                model.FromReferrerLink = true;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ForgotPassword(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            await PopulateAuthSettingsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAuthSettingsAsync();
                return View(model);
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);

            if (user == null || user.IsDeleted)
            {
                ModelState.AddModelError("Password", ErrorMessages.InvalidEmailOrPassword);
                await PopulateAuthSettingsAsync();
                return View(model);
            }

            if (await IsUserLockedOutAsync(user))
            {
                ModelState.AddModelError("Password", ErrorMessages.AccountLocked);
                await PopulateAuthSettingsAsync();
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                await RegisterFailedLoginAsync(user);
                if (await IsUserLockedOutAsync(user))
                    ModelState.AddModelError("Password", ErrorMessages.AccountLocked);
                else
                    ModelState.AddModelError("Password", ErrorMessages.InvalidEmailOrPassword);

                await PopulateAuthSettingsAsync();
                return View(model);
            }

            await ResetLockoutAsync(user);

            if (user.Role == null || user.IsBlocked == true)
            {
                ModelState.AddModelError("Password", ErrorMessages.UserAccessDenied);
                await PopulateAuthSettingsAsync();
                return View(model);
            }

            if (user.EmailConfirmed != true)
            {
                ViewBag.EmailConfirmationMessage = ErrorMessages.LoginEmailConfirmationMessage;
                await PopulateAuthSettingsAsync();
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid || _userService == null)
                return View(model);

            if (model?.TermsConditions != true)
            {
                ModelState.AddModelError("TermsConditions", ErrorMessages.ReadPolicyAndTerms);
                return View(model);
            }

            User? referrer = null;
            if (model.ReferrerCode != null)
            {
                referrer = await _userService.GetUserByRefCodeAsync(model?.ReferrerCode);
                if (referrer == null)
                {
                    ModelState.AddModelError("ReferrerCode", ErrorMessages.InvalidReferralCode);
                    return View(model);
                }
            }

            User? existingUser = null;
            existingUser = await _userService.GetUserByEmailAsync(model?.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", ErrorMessages.UserWithEmailAlreadyExists);
                return View(model);
            }

            existingUser = await _userService.GetUserByPhoneAsync(model?.Phone);
            if (existingUser != null)
            {
                ModelState.AddModelError("Phone", ErrorMessages.UserWithPhoneAlreadyExists);
                return View(model);
            }

            string? fullName = model?.FullName?.Trim();
            string? firstName = null;
            string? lastName = null;

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    firstName = char.ToUpper(parts[0][0]) + parts[0][1..].ToLower();
                if (parts.Length > 1)
                    lastName = char.ToUpper(parts[1][0]) + parts[1][1..].ToLower();
            }
            if (string.IsNullOrEmpty(firstName))
            {
                ModelState.AddModelError("FullName", $"{FieldsDisplayNames.FirstName} {ErrorMessages.FieldRequired}");
                return View(model);
            }
            if (string.IsNullOrEmpty(lastName))
            {
                ModelState.AddModelError("FullName", $"{FieldsDisplayNames.LastName} {ErrorMessages.FieldRequired}");
                return View(model);
            }

            Partnerly.Models.User newUser = new Partnerly.Models.User();
            newUser.Email = model?.Email;
            newUser.Phone = model?.Phone;
            newUser.FirstName = firstName;
            newUser.LastName = lastName;
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model?.Password);
            newUser.ReferrerId = referrer?.Id;

            var result = await _userService.CreateUserAsync(newUser);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("TermsConditions", error);
                }
                return View(model);
            }

            return View("RegistrationSuccessful");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || user.IsDeleted)
            {
                ModelState.AddModelError("Email", ErrorMessages.InvalidEmail);
                return View(model);
            }

            if (user.IsBlocked == true)
            {
                ModelState.AddModelError("Email", ErrorMessages.UserIsBlocked);
                return View(model);
            }

            var emailToken = new EmailConfirmationToken { UserId = user.Id, TokenType = EmailTokenTypeAttribute.ForgotPassword };

            var tokenResult = await _tokenService.CreateConfirmationTokenAsync(emailToken);
            if (!tokenResult.Success)
            {
                foreach (var error in tokenResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(model);
            }

            if (tokenResult?.Data?.UserId != null && tokenResult.Data.Token != null)
            {
                var confirmationLink = Url.Action(
                nameof(ConfirmForgotPassword),
                "Account",
                new { userId = tokenResult.Data.UserId, tokenResult.Data.Token },
                Request.Scheme);

                var emailModel = new
                {
                    UserName = $"{user.FirstName} {user.LastName}",
                    ConfirmationLink = confirmationLink,
                };

                await _emailSender.SendEmailWithTemplateAsync(EmailTemplateNameAttribute.ForgotPassword, user.Email, emailModel);

                return View("ForgotPasswordSuccessful");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmForgotPassword(string userId, string token)
        {
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                var emailToken = await _tokenService.GetByUserIDAndTokenAsync(userId, token);

                if (emailToken != null && emailToken.ExpiresAt >= DateTime.UtcNow)
                {
                    var user = await _userService.GetUserByIDAsync(userId);

                    if (user == null || user.IsBlocked == true)
                    {
                        return View("Error", new ErrorViewModel
                        {
                            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                            ErrorCode = ErrorMessages.UserIsBlocked,
                        });
                    }

                    emailToken.Used = true;
                    var tokenResult = await _tokenService.UpdateConfirmationTokenAsync(emailToken, user.Id);

                    if (tokenResult.Success)
                    {
                        return View("ConfirmForgotPassword", new ChangePasswordViewModel { Email = user.Email });
                    }
                }
            }

            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorCode = ErrorMessages.LinkIsExpired,
                ErrorMessage = ErrorMessages.LinkExpiredDetail
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View("ConfirmForgotPassword");
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || user.IsDeleted)
            {
                ModelState.AddModelError("Email", ErrorMessages.InvalidEmail);
                return View("ConfirmForgotPassword");
            }

            if (user.IsBlocked == true)
            {
                ModelState.AddModelError("Email", ErrorMessages.UserIsBlocked);
                return View("ConfirmForgotPassword");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model?.Password);

            var result = await _userService.UpdateUserAsync(user, user.Id);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                var emailToken = await _tokenService.GetByUserIDAndTokenAsync(userId, token);

                if (emailToken != null && emailToken.ExpiresAt >= DateTime.UtcNow)
                {
                    var user = await _userService.GetUserByIDAsync(userId);

                    if (user == null || user.IsBlocked == true)
                        return View("Error", new ErrorViewModel
                        {
                            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                            ErrorCode = ErrorMessages.UserIsBlocked,
                        });

                    user.EmailConfirmed = true;
                    var result = await _userService.UpdateUserAsync(user, user.Id);

                    if (result.Success)
                    {
                        emailToken.Used = true;
                        var tokenResult = await _tokenService.UpdateConfirmationTokenAsync(emailToken, user.Id);

                        if (tokenResult.Success)
                        {
                            return View("ConfirmEmailSuccess");
                        }
                    }
                }
            }

            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorCode = ErrorMessages.LinkIsExpired,
                ErrorMessage = ErrorMessages.LinkExpiredDetail
            });
        }

        [HttpGet]
        [ActionName(nameof(GoogleLogin))]
        public Task<IActionResult> GoogleLoginGet(string? returnUrl = null)
        {
            return StartGoogleLoginAsync(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> GoogleLogin(string? returnUrl = null)
        {
            return StartGoogleLoginAsync(returnUrl);
        }

        [HttpGet]
        [ActionName(nameof(AppleLogin))]
        public IActionResult AppleLoginGet(string? returnUrl = null)
        {
            return AppleLogin(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AppleLogin(string? returnUrl = null)
        {
            // Apple Sign In is not wired yet; send the guest to the login page.
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        [HttpGet]
        public async Task<IActionResult> GoogleRegister(
            string googleId,
            string email,
            string? firstName,
            string? lastName,
            string? returnUrl)
        {
            if (!await IsGoogleLoginEnabledAsync())
                return RedirectToAction(nameof(Login));

            await PopulateAuthSettingsAsync();

            var model = new GoogleRegisterViewModel
            {
                GoogleId = googleId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            if (!await IsGoogleLoginEnabledAsync())
                return RedirectToAction(nameof(Login));

            var result = await HttpContext.AuthenticateAsync("Google");

            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var principal = result.Principal;

            var googleId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var email = principal.FindFirstValue(
                ClaimTypes.Email);

            var firstName = principal.FindFirstValue(
                ClaimTypes.GivenName);

            var lastName = principal.FindFirstValue(
                ClaimTypes.Surname);

            if (string.IsNullOrWhiteSpace(googleId) ||
                string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
            {
                return RedirectToAction(
                        nameof(GoogleRegister),
                        new
                        {
                            googleId,
                            email,
                            firstName,
                            lastName,
                            returnUrl
                        });
            }

            if (user.IsDeleted == true || user.IsBlocked == true)
            {
                return RedirectToAction(nameof(Login));
            }

            if (await IsUserLockedOutAsync(user))
            {
                return RedirectToAction(nameof(Login));
            }

            if (user.Role == null)
            {
                return RedirectToAction(nameof(Login));
            }

            await SignInUserAsync(user, true);

            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleId;

                await _userService.UpdateUserAsync(user, user.Id);
            }

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleRegister(GoogleRegisterViewModel model)
        {
            if (!await IsGoogleLoginEnabledAsync())
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                await PopulateAuthSettingsAsync();
                return View(model);
            }

            var existingUser = await _userService.GetUserByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", ErrorMessages.UserWithEmailAlreadyExists);
                return View(model);
            }

            var user = new User
            {
                GoogleId = model.GoogleId,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true,

                PasswordHash = null,
            };

            await _userService.CreateUserAsync(user);

            var createdUser = await _userService.GetUserByEmailAsync(model.Email);

            if (createdUser == null)
            {
                return RedirectToAction(nameof(Login));
            }

            await SignInUserAsync(createdUser, true);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUserAsync(User user, bool rememberMe)
        {
            if (user?.Role == null || user.Email == null)
                throw new InvalidOperationException("User role or email is missing.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(Constants.ClaimTypeRoleId, user.RoleId.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name ?? Constants.User),

                new Claim(
                    Constants.ClaimTypeLoginTime,
                    DateTime.UtcNow.ToString("o")),

                new Claim(
                    Constants.ClaimTypeFirstName,
                    user.FirstName ?? ""),

                new Claim(
                    Constants.ClaimTypeLastName,
                    user.LastName ?? ""),

                new Claim(
                    Constants.ClaimTypeReffCode,
                    user.MyReferralCode ?? ""),

                new Claim(
                    Constants.ClaimTypeUserPhotoUrl,
                    user.PhotoUrl ?? Constants.DefaultUserProfilePhotoPath)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var settings = await _systemSettingsService.GetCurrentAsync();
            var persistentDays = settings?.PersistentCookieDays > 0
                ? settings.PersistentCookieDays.Value
                : SystemSettingsDefaults.PersistentCookieDays;
            var sessionMinutes = settings?.SessionExpireMinutes > 0
                ? settings.SessionExpireMinutes.Value
                : SystemSettingsDefaults.SessionExpireMinutes;

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(persistentDays)
                    : DateTimeOffset.UtcNow.AddMinutes(sessionMinutes)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    

    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ErrorMessages.PhotoWasNotSelected
                });
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(photo.ContentType))
            {
                return BadRequest(new
                {
                    success = false,
                    message = ErrorMessages.InvalidImageFormat
                });
            }

            var settings = await _systemSettingsService.GetCurrentAsync();
            var maxSizeMB = settings?.MaxProfilePhotoSizeMb > 0
                ? settings.MaxProfilePhotoSizeMb.Value
                : SystemSettingsDefaults.MaxProfilePhotoSizeMb;
            var maxFileSize = maxSizeMB * 1024L * 1024L;
            
            if (photo.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.Format(ErrorMessages.Maximumfilesizeis, maxSizeMB)
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var extension = Path.GetExtension(photo.FileName);

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "profiles");

            Directory.CreateDirectory(
                uploadsFolder);

            var filePath = Path.Combine(
                uploadsFolder,
                fileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            var photoUrl =
                $"/uploads/profiles/{fileName}";

            var user = await _userService.GetUserByIDAsync(userId);

            if (user != null && user.IsBlocked != true && user.IsDeleted != true)
            {
                user.PhotoUrl = photoUrl;
                var result = await _userService.UpdateUserAsync(user);
                if (result?.Success == true)
                {
                    await RefreshUserPhotoClaimAsync(photoUrl);
                }
            }

            return Json(new
            {
                success = true,
                photoUrl,
                message = ErrorMessages.Photosavedsuccessfully
            });
        }

        private async Task RefreshUserPhotoClaimAsync(string photoUrl)
        {
            var auth = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            var currentPrincipal = auth.Principal ?? User;
            if (currentPrincipal?.Identity is not ClaimsIdentity)
                return;

            var claims = currentPrincipal.Claims
                .Where(c => c.Type != Constants.ClaimTypeUserPhotoUrl)
                .ToList();

            claims.Add(new Claim(Constants.ClaimTypeUserPhotoUrl, photoUrl));

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                auth.Properties ?? new AuthenticationProperties());
        }

        private async Task PopulateAuthSettingsAsync()
        {
            var settings = await _systemSettingsService.GetCurrentAsync();
            ViewBag.GoogleLoginEnabled = settings?.EnableGoogleLogin != false;

            var appName = settings?.ApplicationName;
            if (string.IsNullOrWhiteSpace(appName))
                appName = _config["AppSettings:AppName"];
            if (string.IsNullOrWhiteSpace(appName))
                appName = SystemSettingsDefaults.ApplicationName;

            TempData["appName"] = appName;
        }

        private async Task<IActionResult> StartGoogleLoginAsync(string? returnUrl)
        {
            if (!await IsGoogleLoginEnabledAsync())
                return RedirectToAction(nameof(Login));

            var redirectUrl = Url.Action(
                nameof(GoogleCallback),
                "Account",
                new { returnUrl });

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, "Google");
        }

        private async Task<bool> IsGoogleLoginEnabledAsync()
        {
            var settings = await _systemSettingsService.GetCurrentAsync();
            return settings?.EnableGoogleLogin != false;
        }

        private async Task<bool> IsUserLockedOutAsync(User user)
        {
            if (user.LockoutEnd != null && user.LockoutEnd <= DateTime.UtcNow)
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                await _userService.UpdateUserAsync(user, user.Id);
            }

            return user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow;
        }

        private async Task RegisterFailedLoginAsync(User user)
        {
            var settings = await _systemSettingsService.GetCurrentAsync();
            var maxAttempts = settings?.MaxFailedLoginAttempts ?? SystemSettingsDefaults.MaxFailedLoginAttempts;
            if (maxAttempts <= 0)
                return;

            user.AccessFailedCount += 1;
            if (user.AccessFailedCount >= maxAttempts)
            {
                var minutes = settings?.LockoutDurationMinutes > 0
                    ? settings.LockoutDurationMinutes.Value
                    : SystemSettingsDefaults.LockoutDurationMinutes;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(minutes);
            }

            await _userService.UpdateUserAsync(user, user.Id);
        }

        private async Task ResetLockoutAsync(User user)
        {
            if (user.AccessFailedCount == 0 && user.LockoutEnd == null)
                return;

            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _userService.UpdateUserAsync(user, user.Id);
        }
    }
}
