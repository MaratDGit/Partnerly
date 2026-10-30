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

        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View("Index");
        }

        public IActionResult Privacy()
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
