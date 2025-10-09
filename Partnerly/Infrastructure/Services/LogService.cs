using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.Runtime.CompilerServices;

namespace Partnerly.Infrastructure.Services
{
    public class LogService : ILogService
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly ILogRepository _logRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;

        public LogService(DbContextOptions<AppDbContext> options, ILogRepository logRepo, IPermissionService permissionService, ICurrentUserService currentUser)
        {
            _options = options;
            _logRepo = logRepo;
            _permissionService = permissionService;
            _currentUserService = currentUser;
        }

        public async Task<Log?> GetLogByIDAsync(Guid? id) =>
             await _logRepo.GetByIdAsync(id);

        public async Task<IEnumerable<Log?>> GetAllLogsAsync() =>
            await _logRepo.GetAllAsync();

        public async Task<Log?> CreateLogAsync(string action, string type, string? message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            using var context = new AppDbContext(_options, _currentUserService);

            var newLog = new Log { Id = Guid.NewGuid() };
            newLog.Type = type;
            newLog.Action = action;
            newLog.LogMessage = message ?? ErrorMessages.DefaultLogErrorMessage;
            newLog.FilePath = file;
            newLog.Method = member;
            newLog.LineNumber = line;
            newLog.IsDeleted = false;

            await context.AddAsync(newLog);
            await context.SaveChangesAsync();
            return newLog;
        }

        public async Task UpdateLogAsync(Log? log)
        {
            if (log == null)
                return;

            if (await GetLogByIDAsync(log.Id) != null)
            {
                if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, log))
                    throw new UnauthorizedAccessException(ErrorMessages.NoPermissionForThisAction);

                _logRepo.Update(log);
                await _logRepo.SaveChangesAsync();
            }
        }

        public async Task DeleteLogAsync(Guid? id)
        {
            if (id != null)
            {
                var log = await _logRepo.GetByIdAsync((Guid)id);
                if (log != null)
                {
                    if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, log))
                        throw new UnauthorizedAccessException(ErrorMessages.NoPermissionForThisAction);

                    //_logRepo.Delete(log);
                    await _logRepo.SaveChangesAsync();
                }
            }
        }
    }
}
