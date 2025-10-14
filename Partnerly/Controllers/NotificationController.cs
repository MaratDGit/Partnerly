using Microsoft.AspNetCore.Mvc;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Controllers
{
    public class NotificationController : _BaseController
    {
        private readonly INotificationService _notificationService;
        public NotificationController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, INotificationService notificationService)
        : base(eventBus, userService, currentUser, logService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = new List<Notification>();

            if (_currentUser?.UserId != null)
             notifications = await _notificationService.GetUserNotificationsAsync(_currentUser.UserId.Value);

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid? id)
        {
            if (id != null)
            {
                await _notificationService.MarkAsReadAsync(id);
            }
            
            return Ok();
        }
    }
}
