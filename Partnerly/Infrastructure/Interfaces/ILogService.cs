using Partnerly.Models;
using System.Runtime.CompilerServices;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface ILogService
    {
        Task<Log?> CreateLogAsync(string action, string type, string? message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        Task<Log?> GetLogByIDAsync(Guid? id);
        Task<IEnumerable<Log?>> GetAllLogsAsync();
        Task UpdateLogAsync(Log? user);
        Task DeleteLogAsync(Guid? id);
    }
}
