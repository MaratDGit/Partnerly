using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Attributes;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.Net.Sockets;

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

        public IQueryable<SupportTicketExt> QueryEntityExt(IEnumerable<string> fields)
        {
            return _context.SupportTickets
            .AsNoTracking()
            .Select(x => new SupportTicketExt
            {
                Id = x.Id,
                UserId = x.UserId,
                AssignedTo = x.AssignedTo,
                CreatorName = x.Creator.FirstName + " " + x.Creator.LastName,
                AssignedToName = x.Assigned.FirstName + " " + x.Assigned.LastName,
                Status = x.Status,
                TicketID = x.TicketID,
                Subject = x.Subject,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
            });
        }
    }
}
