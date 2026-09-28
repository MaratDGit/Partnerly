using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;
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
        #endregion
        #region Constructor
        public AccountController(IConfiguration config, ILogger<AccountController> logger, IUserService userService, ILogService logService, ICurrentUserService currentUser, IEmailSender emailSender, IEmailConfirmationTokenService tokenService, IEventBus eventBus, IWebHostEnvironment environment)
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
        }
        #endregion

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult Register(string? reff = null, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var model = new RegisterViewModel();
            if (reff != null)
            {
                model.ReferrerCode = reff;
                model.FromReferrerLink = true;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash) || user.IsDeleted)
            {
                ModelState.AddModelError("Password", ErrorMessages.IncorectPasswordOrUsername);
                return View(model);
            }


            if (user.Role == null || user.IsBlocked == true)
            {
                ModelState.AddModelError("Password", ErrorMessages.UserAccessDenied);
                return View(model);
            }

            if (user.EmailConfirmed != true)
            {
                ViewBag.EmailConfirmationMessage = ErrorMessages.LoginEmailConfirmationMessage;
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
                    ModelState.AddModelError("ReferrerCode", ErrorMessages.InvalidRefferalCode);
                    return View(model);
                }
            }

            User? existingUser = null;
            existingUser = await _userService.GetUserByEmailAsync(model?.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", ErrorMessages.UserWithEmailArleadyExist);
                return View(model);
            }

            existingUser = await _userService.GetUserByPhoneAsync(model?.Phone);
            if (existingUser != null)
            {
                ModelState.AddModelError("Phone", ErrorMessages.UserWithPhoneArleadyExist);
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
                ModelState.AddModelError("Email", ErrorMessages.IncorectEmail);
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
                ModelState.AddModelError("Email", ErrorMessages.IncorectEmail);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
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

        [HttpGet]
        public IActionResult GoogleRegister(
            string googleId,
            string email,
            string? firstName,
            string? lastName,
            string? returnUrl)
        {
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
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userService.GetUserByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", ErrorMessages.UserWithEmailArleadyExist);
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
            if (user.Role == null || user.Email == null)
                throw new InvalidOperationException("User role or email is missing.");

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.Name),

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
            Constants.ClaimTypeRoleType,
            user.Role.Type ?? RoleTypeAttribute.View),

        new Claim(
            Constants.ClaimTypeReffCode,
            user.MyReferralCode ?? ""),

        new Claim(
            Constants.ClaimTypeUserPhotoUrl,
            user.PhotoUrl ?? Constants.DefaultUserProfilePhotoPath)
    };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddMinutes(60)
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
                    message = ErrorMessages.Photowasnotselected
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
                    message = ErrorMessages.Invalidimageformat
                });
            }

            const int maxSizeMB = 5;
            const long maxFileSize = maxSizeMB * 1024 * 1024;
            
            if (photo.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ErrorMessages.Maximumfilesizeis + $"{maxFileSize.ToString()} MB"
                });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var extension = Path.GetExtension(photo.FileName);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

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
            if (user?.IsBlocked != true && user.IsDeleted != true)
            {
                user.PhotoUrl = photoUrl;
                var result = await _userService.UpdateUserAsync(user);
                if (result?.Success == true)
                {
                    ViewBag.UserPhotoUrl = user.PhotoUrl;
                }
            }

            return Json(new
            {
                success = true,
                photoUrl,
                message = ErrorMessages.Photosavedsuccessfully
            });
        }
    }
}
