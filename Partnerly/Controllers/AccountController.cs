using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    public class AccountController : Controller
    {

        #region Services
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly ILogService _logService;
        private readonly ICurrentUserService _currentUser;
        #endregion
        #region Constructor
        public AccountController(ILogger<AccountController> logger, IUserService userService, ILogService logService, ICurrentUserService currentUser)
        {
            _logger = logger;
            _userService = userService;
            _logService = logService;
            _currentUser = currentUser;
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

            Partnerly.Models.User user1 = new Partnerly.Models.User();
            var result = await _userService.CreateUserAsync(user1);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(model); // вернём ту же страницу с ошибками
            }

            //// Создание нового пользователя
            //var user = new User
            //{
            //    Email = model.Email,
            //    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            //    Role = "User" // по умолчанию
            //};

            //_db.Users.Add(user);
            //await _db.SaveChangesAsync();

            //// Авто-логин после регистрации
            //var claims = new List<Claim>
            //{
            //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            //    new Claim(ClaimTypes.Name, user.Email),
            //    new Claim(ClaimTypes.Role, user.Role)
            //};

            //var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
