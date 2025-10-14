using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class SystemPreferencesController : _BaseController
    {
        protected readonly ISystemSettingsService _systemSettingsService;
        public SystemPreferencesController(IEventBus eventBus, ISystemSettingsService systemSettingsService, IUserService userService, ICurrentUserService currentUser, ILogService logService)
        : base(eventBus, userService, currentUser, logService)
        {
            _systemSettingsService = systemSettingsService;
        }

        public async Task<IActionResult> Index()
        {
            SystemSettings? setting = await _systemSettingsService.GetSetupByIDAsync(1);
            if (setting == null) return NotFound();

            var settingAsView = await PropertyActionsHelper.CopyPropertiesAsync(setting, new SystemPreferencesViewModel());
            if (settingAsView == null) return NotFound();

            return View(settingAsView);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
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

                    ServiceResult<SystemSettings?> result = await _systemSettingsService.UpdateSystemSettingsAsync(setting);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View(model);
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Index", new { id = model.Id });
                }
            }
            return View(model);
        }
    }
}
