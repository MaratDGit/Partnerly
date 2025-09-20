using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Partnerly.Controllers
{
    public class AccountController : Controller
    {

        #region Services
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly ILogService _logService;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailSender _emailSender;
        private readonly IEmailConfirmationTokenService _tokenService;
        #endregion
        #region Constructor
        public AccountController(ILogger<AccountController> logger, IUserService userService, ILogService logService, ICurrentUserService currentUser, IEmailSender emailSender, IEmailConfirmationTokenService tokenService)
        {
            _logger = logger;
            _userService = userService;
            _logService = logService;
            _currentUser = currentUser;
            _emailSender = emailSender;
            _tokenService = tokenService;
        }
        #endregion

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
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

            //string testMail = "marat.iigservices@gmail.com";
            //string testPass = "MarDan123!";

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
                //ModelState.AddModelError("", ErrorMessages.LoginEmailConfirmationMessage);
                return View(model);
            }

            if (user.Role.Name != null && user.Email != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.Name)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
            }

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

            Partnerly.Models.User newUser = new Partnerly.Models.User();
            newUser.Email = model?.Email;
            newUser.Phone = model?.Phone;
            newUser.FirstName = model?.FirstName;
            newUser.LastName = model?.LastName;
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model?.Password);
            newUser.ReferrerId = referrer?.Id;

            var result = await _userService.CreateUserAsync(newUser);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(model);
            }

            if (result.Data?.Id != null && result.Data.Email != null)
            {
                var emailToken = new EmailConfirmationToken { UserId = result.Data.Id };
            
                var tokenResult = await _tokenService.CreateConfirmationTokenAsync(emailToken);
                if (!tokenResult.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(model);
                }

                if (tokenResult?.Data?.UserId != null && tokenResult.Data.Token != null)
                {
                    var confirmationLink = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = tokenResult.Data.UserId, tokenResult.Data.Token },
                    Request.Scheme);

                    await _emailSender.SendEmailAsync(result.Data.Email, "Подтверждение Email",
                        $"Пожалуйста, подтвердите email: <a href='{confirmationLink}'>Подтвердить</a>");

                    return RedirectToAction("RegistrationSuccessful");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return View("Error");

            var emailToken = await _tokenService.GetByUserIDAndTokenAsync(userId, token);

            if (emailToken == null || emailToken.ExpiresAt < DateTime.UtcNow)
                return View("Error");

            var user = await _userService.GetUserByIDAsync(userId);
            if (user == null || user.IsBlocked == true)
                return View("Error");

            user.EmailConfirmed = true;
            var result = await _userService.UpdateUserAsync(user);
            
            if (result.Success)
            {
                emailToken.Used = true;
                var tokenResult = await _tokenService.UpdateConfirmationTokenAsync(emailToken);

                if (tokenResult.Success)
                {
                    return View("ConfirmEmailSuccess");
                }
            }

            return View("Error");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
