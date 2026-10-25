using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<Permission?>> GetAllPermissionAsync();

        Task<Permission?> GetActivePermissionAsync(Guid permissionId);

        Task<bool> HasPermissionAsync(Guid? userId, string permissionCode);

        Task<HashSet<string>> GetEffectivePermissionCodesAsync(Guid? userId);

        IQueryable<RolePermissionExt> QueryRolePermissionsExt(Guid? roleId = null);
        IQueryable<UserPermissionExt> QueryUserPermissionsExt(Guid? userId = null);

        void AddAccessAudit(
            string action,
            Guid? targetRoleId = null,
            Guid? targetUserId = null,
            string? permissionCode = null,
            string? details = null);
    }
}
