using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Partnerly.Controllers
{
    [Authorize] // только для авторизованных
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
