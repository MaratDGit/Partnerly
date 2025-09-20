using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IEmailConfirmationTokenService
    {
        Task<EmailConfirmationToken?> GetByUserIDAndTokenAsync(Guid? userID, string? token);
        Task<EmailConfirmationToken?> GetByUserIDAndTokenAsync(string? userID, string? token);
        Task<IEnumerable<EmailConfirmationToken?>> GetAllTokensAsync();

        Task<ServiceResult<EmailConfirmationToken?>> CreateConfirmationTokenAsync(EmailConfirmationToken? token);
        Task<ServiceResult<EmailConfirmationToken?>> UpdateConfirmationTokenAsync(EmailConfirmationToken? token);
        Task<ServiceResult<EmailConfirmationToken?>> DeleteConfirmationTokenAsync(Guid? userID, string? token);
    }
}
