using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserGroupRepository : IRepository<UserGroup>
    {
        Task<IEnumerable<UserGroupMember?>> GetAllUsersByGroupIDAsync(Guid groupId);
        Task<IEnumerable<UserGroupMember?>> GetAllGroupsByUserIDAsync(Guid userId);
        Task<UserGroup?> GetByGroupNameAsync(string groupName);
        Task<IEnumerable<UserGroupMember?>> GetAllUsersGroupsAsync();
    }
}
