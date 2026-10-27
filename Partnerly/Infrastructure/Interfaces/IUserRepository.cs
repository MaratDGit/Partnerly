using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string? email);
        Task<User?> GetByRefCodeAsync(string? refCode);
        Task<User?> GetByPhoneAsync(string? phone);
        Task<IEnumerable<User?>> GetReferralsAsync(Guid? userID);
        Task<IReadOnlyList<User>> GetReferralGraphUsersAsync();

        Task<UserPermission?> GetUserPermissionAsync(Guid userId, Guid permissionId);
        Task<UserPermission?> GetUserPermissionByIdAsync(Guid id);
        Task AddUserPermissionAsync(UserPermission permission);
        void DeleteUserPermission(UserPermission permission);
    }
}
