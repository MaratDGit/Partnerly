using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ISupportTicketService
    {
        Task<SupportTicket?> GetSupportTicketByIDAsync(Guid? id);
        Task<IEnumerable<SupportTicket?>> GetAllSupportTicketAsync();
        Task<IEnumerable<SupportTicket?>> GetUserSupportTicketsAsync(Guid? userID);
        Task<ServiceResult<SupportTicket?>> CreateSupportTicketAsync(SupportTicket? ticket);
        Task<ServiceResult<SupportTicket?>> UpdateSupportTicketAsync(SupportTicket? ticket);
        Task<ServiceResult<SupportTicket?>> DeleteSupportTicketAsync(Guid? id);
    }
}
