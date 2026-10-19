using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.Linq;

namespace Partnerly.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string? email)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .FirstOrDefaultAsync(u => u.Email == email && email != null);
        }

        public async Task<User?> GetByRefCodeAsync(string? refCode)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .FirstOrDefaultAsync(u => u.MyReferralCode == refCode && refCode != null);
        }

        public async Task<User?> GetByPhoneAsync(string? phone)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .FirstOrDefaultAsync(u => u.Phone == phone && phone != null);
        }

        public async Task<IEnumerable<User?>> GetReferralsAsync(Guid? userID)
        {
            return await _dbSet
                .Where(u => u.ReferrerId == userID && userID != null)
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .ToListAsync();
        }
    }
}
