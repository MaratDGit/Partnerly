using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByEmailAsync(string? email);
        Task<User?> GetUserByPhoneAsync(string? phone);
        Task<User?> GetUserByRefCodeAsync(string? refCode);
        Task<User?> GetUserByIDAsync(Guid? id);
        Task<User?> GetUserByIDAsync(string? id);
        Task<IEnumerable<User?>> GetAllUsersAsync();
        Task<IEnumerable<User?>> GetReferralsAsync(Guid? userID);
        Task<ReferralTreeQueryResult> GetReferralTreeAsync(
            Guid rootUserId,
            Guid viewerId,
            bool viewerIsSuper,
            bool canViewRoot);

        Task<ServiceResult<User?>> CreateUserAsync(User? user);
        Task<ServiceResult<User?>> UpdateUserAsync(User? user, Guid? userId = null);
        Task<ServiceResult<User?>> DeleteUserAsync(Guid? id);

        IQueryable<User> QueryEntity(IEnumerable<string> fields);

        Task<UserPermission?> GetUserPermissionByIdAsync(Guid id);
        Task<ServiceResult<UserPermission?>> AddPermissionAsync(Guid userId, Guid permissionId, bool isGranted = true);
        Task<ServiceResult<UserPermission?>> DeletePermissionAsync(Guid userId, Guid permissionId);
        Task<ServiceResult<UserPermission?>> UpdatePermissionGrantAsync(Guid userPermissionId, bool isGranted);
    }
}
