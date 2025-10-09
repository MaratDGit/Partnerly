using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;

        public RoleService(IRoleRepository roleRepo, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService)
        {
            _roleRepo = roleRepo;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
        }

        public async Task<Role?> GetRoleByIDAsync(Guid? id) =>
            await _roleRepo.GetByIdAsync(id);

        public async Task<Role?> GetRoleByNameAsync(string? name) =>
            await _roleRepo.GetByNameAsync(name);

        public async Task<IEnumerable<Role?>> GetAllRolesAsync() =>
            await _roleRepo.GetAllAsync();

        public async Task<ServiceResult<Role?>> CreateRoleAsync(Role? role)
        {
            if (role == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role"));
                return ServiceResult<Role?>.Fail(new List<string> { });
            }
                
            var newRole = new Role { Id = Guid.NewGuid() };

            newRole.Name = role.Name;
            newRole.Type = role.Type;
            newRole.IsDeleted = false;

            await _roleRepo.AddAsync(newRole);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<Role?>.Ok(newRole);
        }

        public async Task<ServiceResult<Role?>> UpdateRoleAsync(Role? role)
        {
            if (role == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role"));
                return ServiceResult<Role?>.Fail(new List<string> { });
            }

            if (await _roleRepo.GetByIdAsync(role.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Role"));
                return ServiceResult<Role?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, role))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<Role?>.Fail(new List<string> { });
            }
                
            _roleRepo.Update(role);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<Role?>.Ok(role);
        }

        public async Task<ServiceResult<Role?>> DeleteRoleAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role ID"));
                return ServiceResult<Role?>.Fail(new List<string> { });
            }

            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Role"));
                return ServiceResult<Role?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, role))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<Role?>.Fail(new List<string> { });
            }
                
            //_roleRepo.Delete(role);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<Role?>.Ok(role);
        }
    }
}
