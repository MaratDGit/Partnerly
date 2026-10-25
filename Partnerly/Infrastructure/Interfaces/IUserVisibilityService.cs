using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserVisibilityService
    {
        IQueryable<Guid> VisibleUserIds();
        Task<bool> CanViewUserAsync(Guid? userId);
        Task<bool> CanViewUserAsync(User? user);
        Task<IEnumerable<User>> FilterUsersAsync(IEnumerable<User?> users);
    }
}
