using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ISystemSettingsRepository _setupRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;

        public SystemSettingsService(ISystemSettingsRepository setupRepo, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService)
        {
            _setupRepo = setupRepo;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
        }

        public async Task<SystemSettings?> GetSetupByIDAsync(int? id) =>
            await _setupRepo.GetBySetupIDAsync(id);

        public async Task<IEnumerable<SystemSettings?>> GetAllSetupAsync() =>
                    await _setupRepo.GetAllAsync();

        public async Task<ServiceResult<SystemSettings?>> CreateSystemSettingsAsync(SystemSettings? rec)
        {
            if (rec == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "SystemSettings"));
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            var newSetup = new SystemSettings();

            newSetup.EmailConfirmationTokenExpiredAtHours = rec.EmailConfirmationTokenExpiredAtHours;

            await _setupRepo.AddAsync(newSetup);
            await _setupRepo.SaveChangesAsync();

            return ServiceResult<SystemSettings?>.Ok(newSetup);
        }

        public async Task<ServiceResult<SystemSettings?>> UpdateSystemSettingsAsync(SystemSettings? rec)
        {
            if (rec?.Id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "SystemSettings"));
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, rec))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            _setupRepo.Update(rec);
            await _setupRepo.SaveChangesAsync();

            return ServiceResult<SystemSettings?>.Ok(rec);
        }

        public async Task<ServiceResult<SystemSettings?>> DeleteSystemSettingsAsync(int? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Settings ID"));
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            var setup = await _setupRepo.GetBySetupIDAsync(id);
            if (setup == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Settings"));
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, setup))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            _setupRepo.Delete(setup);
            await _setupRepo.SaveChangesAsync();

            return ServiceResult<SystemSettings?>.Ok(setup);
        }
    }
}
