using Microsoft.AspNetCore.SignalR;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Hubs;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;

        public NotificationService(INotificationRepository notificationRepository, IHubContext<NotificationHub> hub, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService)
        {
            _notificationRepository = notificationRepository;
            _hub = hub;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
        }

        public async Task<Notification?> GetNotificationByIDAsync(Guid? id) =>
            await _notificationRepository.GetByIdAsync(id);

        public async Task<Notification?> GetNotificationByTicketIDAsync(Guid? ticketID) =>
           await _notificationRepository.GetNotificationByTicketIDAsync(ticketID);

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userID, bool onlyUnread = false) =>
            await _notificationRepository.GetUserNotificationsAsync(userID, onlyUnread);

        public async Task<IEnumerable<Notification?>> GetAllNotificationsAsync() =>
            await _notificationRepository.GetAllAsync();

        public async Task<ServiceResult<Notification?>> MarkAsReadAsync(Guid? notificationId)
        {
            if (notificationId == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Notification id"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            var notif = await GetNotificationByIDAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                return await UpdateNotificationAsync(notif, fromMarkAsRead: true);
            }

            return ServiceResult<Notification?>.Ok(notif);
        }

        public async Task<ServiceResult<Notification?>> MarkAllReadAsync(Guid? userID)
        {
            if (userID == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "user id"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            var userNotifications = await GetUserNotificationsAsync((Guid)userID, onlyUnread: true);
            if (userNotifications != null && userNotifications.Count() > 0)
            {
                foreach (var notification in userNotifications.Where(_ => _.Type == NotificationTypeAttribute.Information))
                {
                    notification.IsRead = true;
                    await UpdateNotificationAsync(notification, fromMarkAsRead: true);
                }
            }

            return ServiceResult<Notification?>.Ok( new Notification());
        }

        public async Task<ServiceResult<Notification?>> CreateNotificationAsync(Notification? notification)
        {
            if (notification == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Notification"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            var newnotification = new Notification { Id = Guid.NewGuid() };

            newnotification.Message = notification.Message;
            newnotification.Type = notification.Type;
            newnotification.Link = notification.Link;
            newnotification.UserId = notification.UserId;
            newnotification.TicketID = notification.TicketID;
            newnotification.IsDeleted = false;

            await _notificationRepository.AddAsync(newnotification);
            await _notificationRepository.SaveChangesAsync();

            await SendNotification(newnotification.UserId.ToString(), newnotification);

            return ServiceResult<Notification?>.Ok(newnotification);
        }

        public async Task<ServiceResult<Notification?>> UpdateNotificationAsync(Notification? notification, bool fromMarkAsRead = false)
        {
            if (notification == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Notification"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            if (await _notificationRepository.GetByIdAsync(notification.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Notification"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            if ((!fromMarkAsRead || _currentUserService.UserId != notification.UserId) && !await _permissionService.CanUpdateAsync(_currentUserService.UserId, notification))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            _notificationRepository.Update(notification);
            await _notificationRepository.SaveChangesAsync();

            if (fromMarkAsRead)
            {
                var unread = await GetUserNotificationsAsync(notification.UserId, onlyUnread: true);
                await UpdateUnreadCount(notification.UserId.ToString(), unread == null ? 0 : unread.Count());
            }

            return ServiceResult<Notification?>.Ok(notification);
        }

        public async Task<ServiceResult<Notification?>> DeleteNotificationAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Notification ID"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            var Notification = await _notificationRepository.GetByIdAsync(id);
            if (Notification == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Notification"));
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, Notification))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.NotificationDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<Notification?>.Fail(new List<string> { });
            }

            _notificationRepository.Delete(Notification);
            await _notificationRepository.SaveChangesAsync();

            return ServiceResult<Notification?>.Ok(Notification);
        }

        #region Hub
        public async Task SendNotification(string userId, Notification notification)
        {
            await _hub.Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }

        public async Task UpdateUnreadCount(string userId, int count)
        {
            await _hub.Clients.User(userId).SendAsync("UpdateUnreadCount", count);
        }
        #endregion
    }
}
