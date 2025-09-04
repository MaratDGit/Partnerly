using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Models;
using System.Diagnostics;
using System.Reflection;

namespace Partnerly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var prop = typeof(Role).GetProperty(nameof(Role.Type));
            var attr = prop?.GetCustomAttribute<RoleTypeAttribute>();

            if (attr != null)
            {
                Console.WriteLine("Dropdown values:");
                foreach (var (value, label) in attr.Items)
                {
                    Console.WriteLine($"{value} - {label}");
                }
            }

            return View();
        }

        public IActionResult Privacy()
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