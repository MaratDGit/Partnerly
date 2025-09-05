using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> CanUpdateAsync<T>(Guid? currentUser, T entity) where T : class, IAuditableEntity;
        Task<bool> CanDeleteAsync<T>(Guid? currentUser, T entity) where T : class, IAuditableEntity;
    }
}
