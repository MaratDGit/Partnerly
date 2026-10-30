using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;
using System.Diagnostics;

namespace Partnerly.Controllers
{
    public class HomeController : Controller
    {
        #region Services
        private readonly ILogger<HomeController> _logger;
        private readonly ISystemSettingsService _systemSettingsService;
        #endregion
        #region Constructor
        public HomeController(ILogger<HomeController> logger, ISystemSettingsService systemSettingsService)
        {
            _logger = logger;
            _systemSettingsService = systemSettingsService;
        }
        #endregion

        public async Task<IActionResult> Index(string? auth = null, string? reff = null, string? returnUrl = null, string? dialog = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var settings = await _systemSettingsService.GetCurrentAsync();
            ViewBag.GoogleLoginEnabled = settings?.EnableGoogleLogin != false;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.AuthPanel = NormalizeAuthPanel(auth);
            ViewBag.InfoPanel = dialog == "privacy" ? "privacy" : null;
            ViewBag.LoginModel ??= new LoginViewModel();
            ViewBag.ForgotModel ??= new ForgotPasswordViewModel();

            var register = ViewBag.RegisterModel as RegisterViewModel ?? new RegisterViewModel();
            if (!string.IsNullOrWhiteSpace(reff))
            {
                register.ReferrerCode = reff;
                register.FromReferrerLink = true;
                ViewBag.AuthPanel ??= "register";
            }
            ViewBag.RegisterModel = register;

            return View("Index");
        }

        private static string? NormalizeAuthPanel(string? auth)
        {
            return auth switch
            {
                "login" or "register" or "forgot" or "register-ok" or "forgot-ok" or "email-ok" => auth,
                _ => null
            };
        }

        public IActionResult Privacy()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Index), new { dialog = "privacy" });
            }

            return View();
        }

        [Authorize]
        public IActionResult Documentation()
        {
            return View();
        }

        public async Task<IActionResult> Maintenance()
        {
            var settings = await _systemSettingsService.GetCurrentAsync();
            ViewBag.MaintenanceMessage = string.IsNullOrWhiteSpace(settings?.MaintenanceMessage)
                ? null
                : settings.MaintenanceMessage;
            ViewBag.ApplicationName = string.IsNullOrWhiteSpace(settings?.ApplicationName)
                ? SystemSettingsDefaults.ApplicationName
                : settings.ApplicationName;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
