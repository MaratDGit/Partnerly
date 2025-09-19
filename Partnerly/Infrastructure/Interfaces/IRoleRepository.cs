using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetByNameAsync(string? name);
    }
}
