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

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllReadAsync(userId.Value);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value, onlyUnread: true);

            return Json(notifications.Select(n => new
            {
                n.Id,
                n.Message,
                CreatedAt = ((DateTime)n.CreatedDate).ToString("dd.MM.yyyy HH:mm")
            }));
        }
    }
}
