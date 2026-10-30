using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;
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

        public async Task<User?> GetFirstActiveByRoleIdAsync(Guid roleId)
        {
            return await _dbSet
                .Where(u =>
                    u.RoleId == roleId &&
                    !u.IsDeleted &&
                    u.IsBlocked != true)
                .OrderBy(u => u.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<User?>> GetReferralsAsync(Guid? userID)
        {
            return await _dbSet
                .Where(u => u.ReferrerId == userID && userID != null)
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<User>> GetReferralGraphUsersAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<UserPermission?> GetUserPermissionAsync(Guid userId, Guid permissionId)
        {
            return await _context.Set<UserPermission>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PermissionId == permissionId);
        }

        public async Task<UserPermission?> GetUserPermissionByIdAsync(Guid id)
        {
            return await _context.Set<UserPermission>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddUserPermissionAsync(UserPermission permission)
        {
            await _context.Set<UserPermission>().AddAsync(permission);
        }

        public void DeleteUserPermission(UserPermission permission)
        {
            _context.Set<UserPermission>().Remove(permission);
        }
    }
}
