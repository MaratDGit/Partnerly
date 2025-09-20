using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IEmailConfirmationTokenRepository : IRepository<EmailConfirmationToken>
    {
        Task<EmailConfirmationToken?> GetByUserIDAndTokenAsync(Guid? userID, string? token);
    }
}
