using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class UserGroupRepository : Repository<UserGroup>, IUserGroupRepository
    {
        public UserGroupRepository(AppDbContext context) : base(context) {}

        public async Task<IEnumerable<UserGroupMember?>> GetAllUsersByGroupIDAsync(Guid groupId)
        {
            return await _context.UserGroupMembers
                .Where(u => u.GroupId == groupId)
                .ToListAsync();
        }
        public async Task<IEnumerable<UserGroupMember?>> GetAllGroupsByUserIDAsync(Guid userId)
        {
            return await _context.UserGroupMembers
                .Where(u => u.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserGroup?> GetByGroupNameAsync(string groupName)
        {
            return await _dbSet
                .Where(u => u.Name.ToUpper() == groupName.ToUpper())
                .FirstOrDefaultAsync();
        }
    }
}
