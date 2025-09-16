using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Infrastructure.Interfaces;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                //if (model?.Email == null)
                //{
                //    ModelState.AddModelError("EmailEmpty", "Обязательное поле");
                //}
                //if (model?.Password == null)
                //{
                //    ModelState.AddModelError("PasswordEmpty", "Обязательное поле");
                //}

                return View(model);
            }
               

            //string testMail = "marat.iigservices@gmail.com";
            //string testPass = "MarDan123!";

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash) || user.IsDeleted)
            {
                ModelState.AddModelError("Password", "Неверный логин или пароль");
                return View(model);
            }

            if (user.Role == null || user.IsBlocked == true)
            {
                ModelState.AddModelError("Password", "у пользователя нету доступа");
                return View(model);
            }

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
