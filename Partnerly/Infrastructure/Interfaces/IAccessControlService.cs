using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IAccessControlService
    {
        Task<IReadOnlyList<Permission>> GetPermissionsAsync();

        Task<IReadOnlyList<Permission>> GetRolePermissionsAsync(
            Guid roleId);

        Task<bool> SetRolePermissionsAsync(
            Guid roleId,
            IEnumerable<Guid> permissionIds);

        Task<IReadOnlyList<UserPermission>> GetUserPermissionsAsync(
            Guid userId);

        Task<bool> SetUserPermissionsAsync(
            Guid userId,
            IEnumerable<UserPermissionInput> permissions);
    }

    public sealed class UserPermissionInput
    {
        public Guid PermissionId { get; set; }

        public bool IsGranted { get; set; }
    }
}
