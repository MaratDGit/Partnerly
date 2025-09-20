using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class EmailConfirmationTokenRepository : Repository<EmailConfirmationToken>, IEmailConfirmationTokenRepository
    {
        public EmailConfirmationTokenRepository(AppDbContext context) : base(context) { }

        public async Task<EmailConfirmationToken?> GetByUserIDAndTokenAsync(Guid? userID, string? token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => userID != null && token != null && u.UserId == userID && u.Token == token && u.Used != true);
        }
    }
}
