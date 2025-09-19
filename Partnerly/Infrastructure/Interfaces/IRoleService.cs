using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IRoleService
    {
        Task<Role?> CreateRoleAsync(Role? role);
        Task<Role?> GetRoleByNameAsync(string? name);
        Task<IEnumerable<Role?>> GetAllRolesAsync();
        Task UpdateRoleAsync(Role? role);
        Task DeleteRoleAsync(Guid? id);
    }
}
