using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByEmailAsync(string? email);
        Task<User?> GetUserByPhoneAsync(string? phone);
        Task<User?> GetUserByRefCodeAsync(string? refCode);
        Task<User?> GetUserByIDAsync(Guid? id);
        Task<IEnumerable<User?>> GetAllUsersAsync();

        Task<ServiceResult<User>> CreateUserAsync(User? user);
        Task<ServiceResult<User>> UpdateUserAsync(User? user);
        Task<ServiceResult<User>> DeleteUserAsync(Guid? id);
    }
}
