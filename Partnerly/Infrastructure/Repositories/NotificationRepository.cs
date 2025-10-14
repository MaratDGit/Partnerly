using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context) { }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userID)
        {
            return await _dbSet
            .Where(n => n.UserId == userID)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();
        }
    }
}
