using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserGroupService
    {
        Task<IEnumerable<UserGroupMember?>> GetAllUsersByGroupIDAsync(Guid groupId);
        Task<IEnumerable<UserGroupMember?>> GetAllGroupsByUserIDAsync(Guid userId);
        
        Task<IEnumerable<UserGroup?>> GetAllUserGroupsAsync();
        Task<UserGroup?> GetByIDAsync(Guid id);
        Task<UserGroup?> GetByGroupNameAsync(string groupName);

        Task<ServiceResult<UserGroup?>> CreateUserGroupAsync(UserGroup? group);
        Task<ServiceResult<UserGroup?>> UpdateUserGroupAsync(UserGroup? group);
        Task<ServiceResult<UserGroup?>> DeleteUserGroupAsync(Guid? id);
    }
}
