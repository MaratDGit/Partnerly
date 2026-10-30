using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    public class SystemPreferencesController : _BaseController
    {
        private readonly IRoleService _roleService;

        public SystemPreferencesController(
            IEventBus eventBus,
            ISystemSettingsService systemSettingsService,
            IUserService userService,
            IRoleService roleService,
            ICurrentUserService currentUser,
            ILogService logService,
            IStringLocalizer<SharedResource> localizer,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _roleService = roleService;
        }

        [Authorize(Policy = "Permission:system_settings.view")]
        public async Task<IActionResult> Index()
        {
            SystemSettings? setting = await _systemSettingsService.GetCurrentAsync();
            if (setting == null) return NotFound();

            var settingAsView = await PropertyActionsHelper.CopyPropertiesAsync(setting, new SystemPreferencesViewModel());
            if (settingAsView == null) return NotFound();

            ApplyFormDefaults(settingAsView);
            await PrepareLookupsAsync(settingAsView);
            await PrepareViewModelAsync(settingAsView);

            return View(settingAsView);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:system_settings.edit")]
        public async Task<IActionResult> Edit(int id, SystemPreferencesViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                SystemSettings? setting = await _systemSettingsService.GetSetupByIDAsync(id);
                if (setting != null)
                {
                    setting.OnlineStatusAutoRefreshMinute = model.OnlineStatusAutoRefreshMinute;
                    setting.EmailConfirmationTokenExpiredAtHours = model.EmailConfirmationTokenExpiredAtHours;
                    setting.ForgotPasswordTokenExpiredAtHours = model.ForgotPasswordTokenExpiredAtHours;
                    setting.IsMaintenanceMode = model.IsMaintenanceMode;
                    setting.ScrollingText = model.ScrollingText;
                    setting.ApplicationName = string.IsNullOrWhiteSpace(model.ApplicationName) ? null : model.ApplicationName.Trim();
                    setting.MaintenanceMessage = model.MaintenanceMessage;
                    setting.DefaultLanguage = NormalizeLanguage(model.DefaultLanguage);
                    setting.TimeZoneId = string.IsNullOrWhiteSpace(model.TimeZoneId) ? SystemSettingsDefaults.TimeZoneId : model.TimeZoneId.Trim();
                    setting.IsAiAssistantEnabled = model.IsAiAssistantEnabled;
                    setting.DefaultNewUserRoleId = model.DefaultNewUserRoleId;
                    setting.EnableGoogleLogin = model.EnableGoogleLogin;
                    setting.PersistentCookieDays = model.PersistentCookieDays ?? SystemSettingsDefaults.PersistentCookieDays;
                    setting.SessionExpireMinutes = model.SessionExpireMinutes ?? SystemSettingsDefaults.SessionExpireMinutes;
                    setting.MaxProfilePhotoSizeMb = model.MaxProfilePhotoSizeMb ?? SystemSettingsDefaults.MaxProfilePhotoSizeMb;
                    setting.MaxFailedLoginAttempts = model.MaxFailedLoginAttempts ?? SystemSettingsDefaults.MaxFailedLoginAttempts;
                    setting.LockoutDurationMinutes = model.LockoutDurationMinutes ?? SystemSettingsDefaults.LockoutDurationMinutes;
                    setting.EnableEmailSending = model.EnableEmailSending;
                    setting.EnableNotifications = model.EnableNotifications;
                    setting.EmailAdminOnNewRegistration = model.EmailAdminOnNewRegistration;
                    setting.EmailAdminOnNewTicket = model.EmailAdminOnNewTicket;
                    setting.AdminNotificationEmail = string.IsNullOrWhiteSpace(model.AdminNotificationEmail) ? null : model.AdminNotificationEmail.Trim();
                    setting.DefaultAllowSendEmails = model.DefaultAllowSendEmails;
                    setting.DefaultAllowSendNotifications = model.DefaultAllowSendNotifications;
                    setting.MaxAttachmentSizeMb = model.MaxAttachmentSizeMb ?? SystemSettingsDefaults.MaxAttachmentSizeMb;
                    setting.AllowedAttachmentExtensions = string.IsNullOrWhiteSpace(model.AllowedAttachmentExtensions)
                        ? SystemSettingsDefaults.AllowedAttachmentExtensions
                        : model.AllowedAttachmentExtensions.Trim();
                    setting.TicketAutoAssignUserId = model.TicketAutoAssignUserId;
                    setting.TicketAutoAssignRoleId = model.TicketAutoAssignRoleId;
                    setting.Note = model.Note;

                    ServiceResult<SystemSettings?> result = await _systemSettingsService.UpdateSystemSettingsAsync(setting);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        await PrepareLookupsAsync(model);
                        return View("Index", model);
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Index");
                }
            }

            await PrepareLookupsAsync(model);
            return View("Index", model);
        }

        private static void ApplyFormDefaults(SystemPreferencesViewModel model)
        {
            model.ApplicationName ??= SystemSettingsDefaults.ApplicationName;
            model.DefaultLanguage = NormalizeLanguage(model.DefaultLanguage);
            model.TimeZoneId = string.IsNullOrWhiteSpace(model.TimeZoneId) ? SystemSettingsDefaults.TimeZoneId : model.TimeZoneId;
            model.IsAiAssistantEnabled ??= true;
            model.EnableGoogleLogin ??= true;
            model.PersistentCookieDays ??= SystemSettingsDefaults.PersistentCookieDays;
            model.SessionExpireMinutes ??= SystemSettingsDefaults.SessionExpireMinutes;
            model.MaxProfilePhotoSizeMb ??= SystemSettingsDefaults.MaxProfilePhotoSizeMb;
            model.MaxFailedLoginAttempts ??= SystemSettingsDefaults.MaxFailedLoginAttempts;
            model.LockoutDurationMinutes ??= SystemSettingsDefaults.LockoutDurationMinutes;
            model.EnableEmailSending ??= true;
            model.EnableNotifications ??= true;
            model.EmailAdminOnNewRegistration ??= false;
            model.EmailAdminOnNewTicket ??= false;
            model.DefaultAllowSendEmails ??= true;
            model.DefaultAllowSendNotifications ??= true;
            model.MaxAttachmentSizeMb ??= SystemSettingsDefaults.MaxAttachmentSizeMb;
            if (string.IsNullOrWhiteSpace(model.AllowedAttachmentExtensions))
                model.AllowedAttachmentExtensions = SystemSettingsDefaults.AllowedAttachmentExtensions;
        }

        private static string NormalizeLanguage(string? language)
        {
            var value = (language ?? string.Empty).Trim().ToLowerInvariant();
            return value is "en" or "ru" or "hy" ? value : SystemSettingsDefaults.DefaultLanguage;
        }

        private async Task PrepareLookupsAsync(SystemPreferencesViewModel model)
        {
            if (model.DefaultNewUserRoleId.HasValue)
            {
                var defaultRole = await _roleService.GetRoleByIDAsync(model.DefaultNewUserRoleId);
                if (defaultRole != null && !defaultRole.IsDeleted)
                    model.DefaultNewUserRoleName = defaultRole.Name;
            }

            if (model.TicketAutoAssignUserId.HasValue)
            {
                var user = await _userService.GetUserByIDAsync(model.TicketAutoAssignUserId);
                if (user != null && !user.IsDeleted)
                    model.TicketAutoAssignUserName = $"{user.FirstName} {user.LastName}".Trim();
            }

            if (model.TicketAutoAssignRoleId.HasValue)
            {
                var role = await _roleService.GetRoleByIDAsync(model.TicketAutoAssignRoleId);
                if (role != null && !role.IsDeleted)
                    model.TicketAutoAssignRoleName = role.Name;
            }

            model.TimeZoneOptions = TimeZoneHelper.Choices
                .Select(z => new SelectListItem { Value = z.Id, Text = z.Label })
                .ToList();

            model.LanguageOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "en", Text = "English" },
                new SelectListItem { Value = "ru", Text = "Русский" },
                new SelectListItem { Value = "hy", Text = "Հայերեն" }
            };
        }
    }
}
