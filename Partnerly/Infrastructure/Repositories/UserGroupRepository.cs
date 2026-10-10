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

        public async Task<IEnumerable<UserGroupMember?>> GetAllUsersGroupsAsync()
        {
            return await _context.UserGroupMembers.ToListAsync();
        }

        public IQueryable<UserGroupMemberExt> QueryMembersEntity(Guid? groupId = null)
        {
            if (groupId != null)
            {
                return _context.UserGroupMembers
                .AsNoTracking()
                .Where(x => x.GroupId == groupId)
                .Select(x => new UserGroupMemberExt
                {
                    UserId = x.UserId,
                    GroupId = x.GroupId,
                    UserFirstName = x.User.FirstName,
                    UserLastName = x.User.LastName,
                    UserEmail = x.User.Email,
                    GroupName = x.Group.Name,
                    CreatedDate = x.CreatedDate,
                    UpdatedDate = x.UpdatedDate,
                    Note = x.Note
                });
            }

            return _context.UserGroupMembers
            .AsNoTracking()
            .Select(x => new UserGroupMemberExt
            {
                UserId = x.UserId,
                GroupId = x.GroupId,
                UserFirstName = x.User.FirstName,
                UserLastName = x.User.LastName,
                UserEmail = x.User.Email,
                GroupName = x.Group.Name,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
                Note = x.Note
            });
        }
    }
}
