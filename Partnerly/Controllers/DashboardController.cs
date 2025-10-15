using Microsoft.AspNetCore.Mvc;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    public class DashboardController : _BaseController
    {
        private readonly INotificationService _notificationService;
        public DashboardController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, INotificationService notificationService)
        : base(eventBus, userService, currentUser, logService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> MyProfile()
        {
            return View();
        }

        public async Task<IActionResult> Settings()
        {
            return View();
        }

        public async Task<IActionResult> Payments()
        {
            return View();
        }
    }
}
