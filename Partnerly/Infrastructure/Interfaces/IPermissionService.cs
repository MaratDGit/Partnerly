using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<Permission?>> GetAllPermissionAsync();

        Task<bool> HasPermissionAsync(Guid? userId, string permissionCode);

        IQueryable<RolePermissionExt> QueryRolePermissionsExt(Guid? roleId = null);
    }
}
