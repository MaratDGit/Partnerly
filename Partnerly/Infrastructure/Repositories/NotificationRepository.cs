using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context) { }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userID, bool onlyUnread = false)
        {
            if (onlyUnread)
            {
                return await _dbSet
                .Where(n => n.UserId == userID && n.IsRead == false)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            }

            return await _dbSet
            .Where(n => n.UserId == userID)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();
        }

        public async Task<Notification?> GetNotificationByTicketIDAsync(Guid? ticketID)
        {
            return await _dbSet
            .Where(n => n.TicketID == ticketID && ticketID != null).FirstOrDefaultAsync();
        }
    }
}
