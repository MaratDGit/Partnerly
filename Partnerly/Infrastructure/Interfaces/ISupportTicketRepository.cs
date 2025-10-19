using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ISupportTicketRepository : IRepository<SupportTicket>
    {
        Task<IEnumerable<SupportTicket?>> GetUserSupportTicketsAsync(Guid? userID);
    }
}
