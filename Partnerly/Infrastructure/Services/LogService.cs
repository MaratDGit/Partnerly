using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Infrastructure.Services
{
    public class LogService : ILogService
    {
        private readonly ILogRepository _logRepo;
        private readonly IPermissionService _permissionService;

        public LogService(ILogRepository logRepo, IPermissionService permissionService)
        {
            _logRepo = logRepo;
            _permissionService = permissionService;
        }

        public async Task<Log?> CreateLogAsync(Log? log)
        {
            if (log == null)
                return null;

            var newLog = new Log { Id = Guid.NewGuid() };
            newLog.Type = log.Type;
            newLog.Action = log.Action;
            newLog.CreatorUserId = log.CreatorUserId;
            newLog.LogMessage = log.LogMessage;

            newLog.IsDeleted = false;
            newLog.CreatedDate = DateTime.UtcNow;
            newLog.UpdatedDate = DateTime.UtcNow;
            newLog.CreatedBy = newLog.CreatorUserId;
            newLog.UpdatedBy = newLog.CreatorUserId;

            string? result = ValidationHelper.ValidateEntityRequiredFields(newLog, out bool isValid);
            if (!isValid)
            {
                throw new ValidationException(String.Format(ErrorMessages.RequiredFieldsValidationFailed, result));
            }

            await _logRepo.AddAsync(newLog);
            await _logRepo.SaveChangesAsync();
            return newLog;
        }

        public async Task<Log?> GetLogByIDAsync(Guid? id)
        {
            Log? log = null;
            if (id != null)
            {
                log = await _logRepo.GetByIdAsync((Guid)id);
            }
            return log;
        }

        public async Task<IEnumerable<Log?>> GetAllLogsAsync() =>
            await _logRepo.GetAllAsync();

        public async Task UpdateLogAsync(Log? log)
        {
            if (log == null)
                return;

            string? result = ValidationHelper.ValidateEntityRequiredFields(log, out bool isValid);
            if (!isValid)
            {
                throw new ValidationException(String.Format(ErrorMessages.RequiredFieldsValidationFailed, result));
            }

            if (await GetLogByIDAsync(log.Id) != null)
            {
                if (!await _permissionService.CanUpdateAsync(log.UpdatedBy, log))
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
                    _logRepo.Delete(log);
                    await _logRepo.SaveChangesAsync();
                }
            }
        }
    }
}
