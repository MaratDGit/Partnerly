using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<Permission?>> GetAllPermissionAsync();
        //Task<bool> CanUpdateAsync<T>(Guid? currentUser, T entity) where T : class, IAuditableEntity;
        //Task<bool> CanDeleteAsync<T>(Guid? currentUser, T entity) where T : class, IAuditableEntity;

        Task<bool> HasPermissionAsync(Guid? userId, string permissionCode);

        IQueryable<RolePermissionExt> QueryRolePermissionsExt(Guid? roleId = null);
    }
}
