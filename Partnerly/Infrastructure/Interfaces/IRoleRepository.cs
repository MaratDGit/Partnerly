using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetByNameAsync(string? name);
        Task<RolePermission?> GetPermissionAsync(Guid roleId, Guid permissionId);
        Task AddRolePermissionAsync(RolePermission permision);
        void DeleteRolePermission(RolePermission permision);
    }
}
