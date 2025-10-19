using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class SupportTicketRepository : Repository<SupportTicket>, ISupportTicketRepository
    {
        public SupportTicketRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<SupportTicket?>> GetUserSupportTicketsAsync(Guid? userID)
        {
            return await _dbSet
                .Where(u => u.UserId == userID && userID != null)
                .ToListAsync();
        }
    }
}
