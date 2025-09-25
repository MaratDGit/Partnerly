using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;
using System.Diagnostics;
using System.Reflection;

namespace Partnerly.Controllers
{
    public class HomeController : Controller
    {
        #region Services
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        private readonly ILogService _logService;
        private readonly ICurrentUserService _currentUser;
        #endregion
        #region Constructor
        public HomeController(ILogger<HomeController> logger, IUserService userService, ILogService logService, ICurrentUserService currentUser)
        {
            _logger = logger;
            _userService = userService;
            _logService = logService;
            _currentUser = currentUser;
        }
        #endregion

        public IActionResult Index()
        {
            //var prop = typeof(Role).GetProperty(nameof(Role.Type));
            //var attr = prop?.GetCustomAttribute<RoleTypeAttribute>();

            //if (attr != null)
            //{
            //    Console.WriteLine("Dropdown values:");
            //    foreach (var (value, label) in attr.Items)
            //    {
            //        Console.WriteLine($"{value} - {label}");
            //    }
            //}

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

        public IActionResult Maintenance()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}