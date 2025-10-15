using Microsoft.AspNetCore.SignalR;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;

        public NotificationHub(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                var unread = await _notificationService.GetUserNotificationsAsync(Guid.Parse(userId), onlyUnread: true);
                foreach (var n in unread)
                {
                    await Clients.Caller.SendAsync("ReceiveNotification", n);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
