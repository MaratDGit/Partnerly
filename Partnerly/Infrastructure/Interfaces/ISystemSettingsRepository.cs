using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ISystemSettingsRepository : IRepository<SystemSettings>
    {
        Task<SystemSettings?> GetBySetupIDAsync(int? email);
    }
}
