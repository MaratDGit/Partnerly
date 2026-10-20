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
using System.Security.Claims;

namespace Partnerly.Controllers
{
    public class NotificationController : _BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ISupportTicketService _supportTicketService;

        public NotificationController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            INotificationService notificationService, 
            ISupportTicketService supportTicketService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _notificationService = notificationService;
            _supportTicketService = supportTicketService;
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid? id)
        {
            if (id != null)
            {
                var note = await _notificationService.GetNotificationByIDAsync(id);

                if (note?.Type == NotificationTypeAttribute.Information)
                    await _notificationService.MarkAsReadAsync(id);
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllReadAsync(userId.Value);
            return Ok();
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public IActionResult Index()
        {
            NotificationsViewModel model = new NotificationsViewModel();
            model.notifications = NotificationsViewModel.CreateTableOptions(DataTableNames.notificationsTable, _dataTableRegistry);

            return View("~/Views/Shared/DataTable/_DataTablePartial.cshtml", model.notifications);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public IActionResult Create()
        {
            NotificationsViewModel model = new NotificationsViewModel { Id = Guid.NewGuid() };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Create(NotificationsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.GetUserByIDAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("OwnerName", Messages.Usercannotbefound);
                    return View(model);
                }

                Notification newTemp = new Notification();
                newTemp.UserId = model.UserId;
                newTemp.Message = model.Message;
                newTemp.Link = model.Link;

                var result = await _notificationService.CreateNotificationAsync(newTemp);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("Message", error);
                    }
                    return View(model);
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = result?.Data?.Id });
            }

            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var template = await _notificationService.GetNotificationByIDAsync(id);
            if (template == null) return NotFound();

            var templatesAsView = await PropertyActionsHelper.CopyPropertiesAsync(template, new NotificationsViewModel());
            if (templatesAsView == null) return NotFound();

            var creator = await _userService.GetUserByIDAsync(templatesAsView.CreatedBy);
            var owner = await _userService.GetUserByIDAsync(templatesAsView.UserId);
            templatesAsView.CreatorName = $"{creator?.FirstName} {creator?.LastName}";
            templatesAsView.OwnerName = $"{owner?.FirstName} {owner?.LastName}";

            await PrepareViewModelAsync(templatesAsView);

            return View(templatesAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Edit(Guid id, NotificationsViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                Notification? template = await _notificationService.GetNotificationByIDAsync(model.Id);
                if (template != null)
                {
                    template.Message = model.Message;
                    template.IsRead = model.IsRead;
                    template.Link = model.Link;

                    template.Note = model.Note;
                    ServiceResult<Notification?> result = await _notificationService.UpdateNotificationAsync(template);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View(model);
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _notificationService.GetNotificationByIDAsync(id);
            if (template == null) return NotFound();

            var deleteModel = new DeleteConfirmationViewModel
            {
                Id = template.Id,
                Title = ErrorMessages.Deletingthenotification,
                Message = ErrorMessages.Areyousureyouwanttodeletethisfile,
                DeleteAction = "DeleteConfirmed",
                DeleteController = "Notification",
                CancelAction = "Index",
                CancelController = "Notification"
            };

            return View("~/Views/Shared/_DeleteConfirmation.cshtml", deleteModel);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var template = await _notificationService.GetNotificationByIDAsync(id);
            if (template != null)
            {
                ServiceResult<Notification?> result = await _notificationService.DeleteNotificationAsync(id);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    
                    return View();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetLookupList(string fieldName)
        {
            if (fieldName == "Owner")
            {
                
            }

            return Json(new { });
        }


        [HttpGet]
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID is required."
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);

            if (options == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }

            if (request.TableID == DataTableNames.notificationsTable)
            {
                var res = await _tableService.ExecuteAsync(
                   fields => _notificationService.QueryEntity(fields),
                   request,
                   options);

                return Json(res);
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });

            if (request.TableID == DataTableNames.notificationsTable)
            {
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _notificationService.QueryEntity(fields),
                            "notifications");
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }
        }
    }
}
