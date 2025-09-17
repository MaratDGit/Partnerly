using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRefCodeAsync(string refCode);
    }
}
