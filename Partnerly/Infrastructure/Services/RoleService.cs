using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepo;
        private readonly ILogService _logService;
        private readonly IPermissionService _permissionService;

        public RoleService(
            IRoleRepository roleRepo,
            ILogService logService,
            IPermissionService permissionService)
        {
            _roleRepo = roleRepo;
            _logService = logService;
            _permissionService = permissionService;
        }

        public async Task<Role?> GetRoleByIDAsync(Guid? id) =>
            await _roleRepo.GetByIdAsync(id);

        public async Task<Role?> GetRoleByNameAsync(string? name) =>
            await _roleRepo.GetByNameAsync(name);

        public async Task<IEnumerable<Role?>> GetAllRolesAsync() =>
            await _roleRepo.GetAllAsync();

        public IQueryable<Role> QueryEntity(IEnumerable<string> fields) =>
            _roleRepo.QueryEntity(fields);

        public async Task<ServiceResult<RolePermission?>> AddPermissionAsync(Guid roleId, Guid permissionId)
        {
            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role == null || role.IsDeleted || role.IsActive != true)
            {
                return ServiceResult<RolePermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "Role") });
            }

            var catalogPermission = await _permissionService.GetActivePermissionAsync(permissionId);
            if (catalogPermission == null)
            {
                return ServiceResult<RolePermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "Permission") });
            }

            var existing = await _roleRepo.GetPermissionAsync(roleId, permissionId);
            if (existing != null)
            {
                return ServiceResult<RolePermission?>.Fail(new List<string> { $"{FieldsDisplayNames.Permission} {ErrorMessages.UniqueValue}" });
            }

            RolePermission per = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            };

            await _roleRepo.AddRolePermissionAsync(per);
            _permissionService.AddAccessAudit(
                action: "RolePermissionAdded",
                targetRoleId: roleId,
                permissionCode: catalogPermission.Code,
                details: catalogPermission.Name);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<RolePermission?>.Ok(per);
        }

        public async Task<ServiceResult<RolePermission?>> DeletePermissionAsync(Guid roleId, Guid permissionId)
        {
            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role == null || role.IsDeleted || role.IsActive != true)
            {
                return ServiceResult<RolePermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "Role") });
            }

            RolePermission? permission = await _roleRepo.GetPermissionAsync(roleId, permissionId);
            if (permission == null)
            {
                return ServiceResult<RolePermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "Role Permission") });
            }

            var catalogPermission = await _permissionService.GetActivePermissionAsync(permissionId);

            _roleRepo.DeleteRolePermission(permission);
            _permissionService.AddAccessAudit(
                action: "RolePermissionRemoved",
                targetRoleId: roleId,
                permissionCode: catalogPermission?.Code,
                details: catalogPermission?.Name);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<RolePermission?>.Ok(permission);
        }

        public async Task<ServiceResult<Role?>> CreateRoleAsync(Role? role)
        {
            if (role == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role"));
                return ServiceResult<Role?>.Fail(new List<string> { });
            }
                
            var newRole = new Role { Id = Guid.NewGuid() };

            newRole.Name = role.Name;
            newRole.IsDeleted = false;
            newRole.IsActive = role.IsActive;
            newRole.Note = role.Note;

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

            //_roleRepo.Delete(role);
            role.IsDeleted = true;
            _roleRepo.Update(role);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<Role?>.Ok(role);
        }
    }
}
