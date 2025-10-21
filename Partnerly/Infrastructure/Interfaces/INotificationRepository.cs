using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<List<Notification>> GetUserNotificationsAsync(Guid userID, bool onlyUnread = false);
        Task<Notification?> GetNotificationByTicketIDAsync(Guid? ticketID);
    }
}
