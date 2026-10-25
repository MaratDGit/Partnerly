using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
       
        public SystemPreferencesController(
            IEventBus eventBus, 
            ISystemSettingsService systemSettingsService, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            IStringLocalizer<SharedResource> localizer,
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            
        }

        [Authorize(Policy = "Permission:system_settings.view")]
        public async Task<IActionResult> Index()
        {
            SystemSettings? setting = await _systemSettingsService.GetSetupByIDAsync(1);
            if (setting == null) return NotFound();

            var settingAsView = await PropertyActionsHelper.CopyPropertiesAsync(setting, new SystemPreferencesViewModel());
            if (settingAsView == null) return NotFound();

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

                    setting.Note = model.Note;

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
