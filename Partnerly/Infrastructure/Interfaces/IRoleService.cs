using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IRoleService
    {
        Task<Role?> GetRoleByNameAsync(string? name);
        Task<IEnumerable<Role?>> GetAllRolesAsync();
        Task<Role?> GetRoleByIDAsync(Guid? id);

        Task<ServiceResult<RolePermission?>> AddPermissionAsync(Guid roleId, Guid permissionId);
        Task<ServiceResult<RolePermission?>> DeletePermissionAsync(Guid groupId, Guid userId);

        Task<ServiceResult<Role?>> CreateRoleAsync(Role? role);
        Task<ServiceResult<Role?>> UpdateRoleAsync(Role? role);
        Task<ServiceResult<Role?>> DeleteRoleAsync(Guid? id);

        IQueryable<Role> QueryEntity(IEnumerable<string> fields);
    }
}
