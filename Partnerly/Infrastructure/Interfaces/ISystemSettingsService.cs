using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ISystemSettingsService
    {
        Task<SystemSettings?> GetSetupByIDAsync(int? id);
        Task<IEnumerable<SystemSettings?>> GetAllSetupAsync();
        Task<ServiceResult<SystemSettings?>> CreateSystemSettingsAsync(SystemSettings? rec);
        Task<ServiceResult<SystemSettings?>> UpdateSystemSettingsAsync(SystemSettings? rec);
        Task<ServiceResult<SystemSettings?>> DeleteSystemSettingsAsync(int? id);
    }
}
