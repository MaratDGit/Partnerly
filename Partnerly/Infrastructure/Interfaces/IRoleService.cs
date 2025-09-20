using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IRoleService
    {
        Task<Role?> GetRoleByNameAsync(string? name);
        Task<IEnumerable<Role?>> GetAllRolesAsync();

        Task<ServiceResult<Role?>> CreateRoleAsync(Role? role);
        Task<ServiceResult<Role?>> UpdateRoleAsync(Role? role);
        Task<ServiceResult<Role?>> DeleteRoleAsync(Guid? id);
    }
}
