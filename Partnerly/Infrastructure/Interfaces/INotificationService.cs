using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResult<Notification?>> CreateNotificationAsync(Notification? notification);
        Task<List<Notification>> GetUserNotificationsAsync(Guid userId);
        Task<ServiceResult<Notification?>> MarkAsReadAsync(Guid? notificationId);
    }
}
