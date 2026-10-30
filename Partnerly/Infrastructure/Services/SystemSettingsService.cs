using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ISystemSettingsRepository _setupRepo;
        private readonly ILogService _logService;
        private SystemSettings? _current;
        private bool _currentLoaded;

        public SystemSettingsService(ISystemSettingsRepository setupRepo, ILogService logService)
        {
            _setupRepo = setupRepo;
            _logService = logService;
        }

        public async Task<SystemSettings?> GetSetupByIDAsync(int? id) =>
            await _setupRepo.GetBySetupIDAsync(id);

        public async Task<SystemSettings?> GetCurrentAsync()
        {
            if (_currentLoaded)
                return _current;

            _current = await _setupRepo.GetBySetupIDAsync(1);
            _currentLoaded = true;
            return _current;
        }

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

            CopySettings(rec, newSetup);

            await _setupRepo.AddAsync(newSetup);
            await _setupRepo.SaveChangesAsync();

            _current = newSetup;
            _currentLoaded = true;

            return ServiceResult<SystemSettings?>.Ok(newSetup);
        }

        public async Task<ServiceResult<SystemSettings?>> UpdateSystemSettingsAsync(SystemSettings? rec)
        {
            if (rec?.Id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "SystemSettings"));
                return ServiceResult<SystemSettings?>.Fail(new List<string> { });
            }

            _setupRepo.Update(rec);
            await _setupRepo.SaveChangesAsync();

            _current = rec;
            _currentLoaded = true;

            return ServiceResult<SystemSettings?>.Ok(rec);
        }

        private static void CopySettings(SystemSettings source, SystemSettings target)
        {
            target.EmailConfirmationTokenExpiredAtHours = source.EmailConfirmationTokenExpiredAtHours;
            target.ForgotPasswordTokenExpiredAtHours = source.ForgotPasswordTokenExpiredAtHours;
            target.IsMaintenanceMode = source.IsMaintenanceMode;
            target.OnlineStatusAutoRefreshMinute = source.OnlineStatusAutoRefreshMinute;
            target.ScrollingText = source.ScrollingText;
            target.ApplicationName = source.ApplicationName;
            target.MaintenanceMessage = source.MaintenanceMessage;
            target.DefaultLanguage = source.DefaultLanguage;
            target.TimeZoneId = source.TimeZoneId;
            target.IsAiAssistantEnabled = source.IsAiAssistantEnabled;
            target.DefaultNewUserRoleId = source.DefaultNewUserRoleId;
            target.EnableGoogleLogin = source.EnableGoogleLogin;
            target.PersistentCookieDays = source.PersistentCookieDays;
            target.SessionExpireMinutes = source.SessionExpireMinutes;
            target.MaxProfilePhotoSizeMb = source.MaxProfilePhotoSizeMb;
            target.MaxFailedLoginAttempts = source.MaxFailedLoginAttempts;
            target.LockoutDurationMinutes = source.LockoutDurationMinutes;
            target.EnableEmailSending = source.EnableEmailSending;
            target.EnableNotifications = source.EnableNotifications;
            target.EmailAdminOnNewRegistration = source.EmailAdminOnNewRegistration;
            target.EmailAdminOnNewTicket = source.EmailAdminOnNewTicket;
            target.AdminNotificationEmail = source.AdminNotificationEmail;
            target.DefaultAllowSendEmails = source.DefaultAllowSendEmails;
            target.DefaultAllowSendNotifications = source.DefaultAllowSendNotifications;
            target.MaxAttachmentSizeMb = source.MaxAttachmentSizeMb;
            target.AllowedAttachmentExtensions = source.AllowedAttachmentExtensions;
            target.TicketAutoAssignUserId = source.TicketAutoAssignUserId;
            target.TicketAutoAssignRoleId = source.TicketAutoAssignRoleId;
            target.Note = source.Note;
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

            await _logService.CreateLogAsync(LogActionsAttribute.SystemSettingsDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
            
            //_setupRepo.Delete(setup);
            //await _setupRepo.SaveChangesAsync();

            return ServiceResult<SystemSettings?>.Ok(setup);
        }
    }
}
