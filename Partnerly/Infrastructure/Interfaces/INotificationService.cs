using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResult<Notification?>> CreateNotificationAsync(Notification? notification);
        Task<ServiceResult<Notification?>> UpdateNotificationAsync(Notification? notification, bool fromMarkAsRead = false);
        Task<ServiceResult<Notification?>> DeleteNotificationAsync(Guid? id);
        Task<Notification?> GetNotificationByIDAsync(Guid? id);
        Task<List<Notification>> GetUserNotificationsAsync(Guid userId, bool onlyUnread = false);
        Task<ServiceResult<Notification?>> MarkAsReadAsync(Guid? notificationId);
        Task<ServiceResult<Notification?>> MarkAllReadAsync(Guid? userID);
        Task SendNotification(string userId, Notification notification);
        Task UpdateUnreadCount(string userId, int count);
    }
}
