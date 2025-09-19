using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ILogService
    {
        Task<Log?> CreateLogAsync(string action, string type, string? message);
        Task<Log?> GetLogByIDAsync(Guid? id);
        Task<IEnumerable<Log?>> GetAllLogsAsync();
        Task UpdateLogAsync(Log? user);
        Task DeleteLogAsync(Guid? id);
    }
}
