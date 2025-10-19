using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid? id)
        {
            if (id != null)
            {
                var note = await _notificationService.GetNotificationByIDAsync(id);

                if (note?.Type == NotificationTypeAttribute.Information)
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
    }
}
