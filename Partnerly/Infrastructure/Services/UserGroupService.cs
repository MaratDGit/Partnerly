using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class UserGroupService : IUserGroupService
    {
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;

        public UserGroupService(IUserGroupRepository userGroupRepository, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService)
        {
            _userGroupRepository = userGroupRepository;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
        }

        public async Task<UserGroup?> GetByIDAsync(Guid id) =>
            await _userGroupRepository.GetByIdAsync(id);

        public async Task<UserGroup?> GetByGroupNameAsync(string groupName) =>
           await _userGroupRepository.GetByGroupNameAsync(groupName);

        public async Task<IEnumerable<UserGroupMember?>> GetAllUsersByGroupIDAsync(Guid groupId) =>
            await _userGroupRepository.GetAllUsersByGroupIDAsync(groupId);

        public async Task<IEnumerable<UserGroupMember?>> GetAllGroupsByUserIDAsync(Guid userId) =>
            await _userGroupRepository.GetAllGroupsByUserIDAsync(userId);

        public async Task<IEnumerable<UserGroup?>> GetAllUserGroupsAsync() =>
            await _userGroupRepository.GetAllAsync();

        public async Task<ServiceResult<UserGroup?>> CreateUserGroupAsync(UserGroup? userGroup)
        {
            if (userGroup == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "UserGroup"));
                return ServiceResult<UserGroup?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "UserGroup") });
            }

            var newuserGroup = new UserGroup { Id = Guid.NewGuid() };

            newuserGroup.Name = userGroup.Name;
            newuserGroup.Description = userGroup.Description;
            newuserGroup.IsDeleted = false;

            if (userGroup.Members.Count > 0)
            {
                newuserGroup.Members.Clear();

                foreach (var user in userGroup.Members)
                {
                    newuserGroup.Members.Add(new UserGroupMember
                    {
                        GroupId = newuserGroup.Id,
                        UserId = user.UserId
                    });
                }
            }

            await _userGroupRepository.AddAsync(newuserGroup);
            await _userGroupRepository.SaveChangesAsync();

            return ServiceResult<UserGroup?>.Ok(newuserGroup);
        }

        public async Task<ServiceResult<UserGroup?>> UpdateUserGroupAsync(UserGroup? userGroup, List<UserGroupMember> members)
        {
            if (userGroup == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "userGroup"));
                return ServiceResult<UserGroup?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "userGroup") });
            }

            if (await _userGroupRepository.GetByIdAsync(userGroup.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "userGroup"));
                return ServiceResult<UserGroup?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "userGroup") });
            }

            if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, userGroup))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<UserGroup?>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }

            if (members.Any())
            {
                var groupMembers = await GetAllUsersByGroupIDAsync(userGroup.Id);
                foreach (var member in members)
                {
                    if (!groupMembers.Any(_ => _.UserId == member.UserId))
                        userGroup.Members.Add(new UserGroupMember { UserId = member.UserId, GroupId = userGroup.Id });
                }
                foreach (var member in groupMembers)
                {
                    if (!members.Any(_ => _.UserId == member.UserId))
                        userGroup.Members.Remove(member);
                }
            }
            
            _userGroupRepository.Update(userGroup);
            await _userGroupRepository.SaveChangesAsync();

            return ServiceResult<UserGroup?>.Ok(userGroup);
        }

        public async Task<ServiceResult<UserGroup?>> DeleteUserGroupAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "UserGroup ID"));
                return ServiceResult<UserGroup?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "UserGroup ID") });
            }

            var userGroup = await _userGroupRepository.GetByIdAsync(id);
            if (userGroup == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "UserGroup"));
                return ServiceResult<UserGroup?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "UserGroup") });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, userGroup))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserGroupDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<UserGroup?>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }

            _userGroupRepository.Delete(userGroup);
            await _userGroupRepository.SaveChangesAsync();

            return ServiceResult<UserGroup?>.Ok(userGroup);
        }
    }
}
