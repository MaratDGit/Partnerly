using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Events
{
    public class UserRegisteredEvent : IEvent
    {
        public Guid UserId { get; }
        public string UserName { get; }

        public UserRegisteredEvent(Guid userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }

    public class UserRegisteredEventHandler : IEventHandler<UserRegisteredEvent>
    {
        private readonly INotificationService _notificationService;

        public UserRegisteredEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task HandleAsync(UserRegisteredEvent @event)
        {
            var message = string.Format(Messages.UserRegistrationNotification, @event.UserName);

            Notification notification = new Notification { UserId = @event.UserId, Type = NotificationTypeAttribute.Information, Message = message, Link = "/profile" };

            await _notificationService.CreateNotificationAsync(notification);
        }
    }

}
