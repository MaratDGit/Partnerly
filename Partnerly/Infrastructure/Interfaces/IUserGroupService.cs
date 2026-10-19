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
        Task<IEnumerable<UserGroupMember?>> GetAllUsersGroupsAsync();
        Task<ServiceResult<UserGroup?>> CreateUserGroupAsync(UserGroup? group);
        Task<ServiceResult<UserGroup?>> UpdateUserGroupAsync(UserGroup? group);
        Task<ServiceResult<UserGroup?>> DeleteUserGroupAsync(Guid? id);
        
        Task<ServiceResult<UserGroupMember?>> AddMemberAsync(Guid groupId, Guid userId);
        Task<ServiceResult<UserGroupMember?>> DeleteMemberAsync(Guid groupId, Guid userId);

        IQueryable<UserGroup> QueryEntity(IEnumerable<string> fields);
        IQueryable<UserGroupMemberExt> QueryMembersEntity(Guid? groupId = null);
    }
}
