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
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;

namespace Partnerly.Controllers
{
    public class NotificationController : _BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ISupportTicketService _supportTicketService;

        public NotificationController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, INotificationService notificationService, ISupportTicketService supportTicketService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService)
        {
            _notificationService = notificationService;
            _supportTicketService = supportTicketService;
        }

        private static List<User> _users = new List<User>();
        private static List<SupportTicket> _supportTicket = new List<SupportTicket>();

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

        public async Task<IActionResult> Index()
        {
            _supportTicket = (List<SupportTicket>)await _supportTicketService.GetAllSupportTicketAsync();
            _users = (List<User>)await _userService.GetAllUsersAsync();
            return View();
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

            return View();
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

        #region Grids 
        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.Notification))
            {
                return await GetGridDataAsync<NotificationsGridView, Notification>("Notification");
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(Notification))
                return (IEnumerable<TEntity>)await _notificationService.GetAllNotificationsAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is NotificationsGridView templateRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "message", DisplayName = FieldsDisplayNames.Message, LinkTemplate = "/Notification/Edit/{id}" },
                    new GridField
                    {
                        FieldName = "type", DisplayName = FieldsDisplayNames.Type, IsSortable = true, IsFilterable = true,

                        ValueStyles = new Dictionary<string, GridValueStyle>
                        {
                            [FieldsDisplayNames.Error] = new() { CssClass = "badge bg-label-danger fs-tiny rounded-pill ms-auto text-uppercase"},
                            [FieldsDisplayNames.Success] = new() { CssClass = "badge bg-label-success fs-tiny rounded-pill ms-auto text-uppercase" },
                            [FieldsDisplayNames.Information] = new() { CssClass = "badge bg-label-success fs-tiny rounded-pill ms-auto text-uppercase" },
                            [FieldsDisplayNames.Warning] = new() { CssClass = "badge bg-label-warning fs-tiny rounded-pill ms-auto text-uppercase" },
                        }
                    },
                    new GridField { FieldName = "isRead", DisplayName = FieldsDisplayNames.IsRead, Type = "checkbox", IsSortable = true, IsFilterable = true},
                    new GridField { FieldName = "createdByUserFullName", DisplayName = FieldsDisplayNames.CreatorName, IsVisible = true, IsSortable = true, IsFilterable = true,  LinkTemplate = "/Users/Edit/{createdBy}" },
                    new GridField { FieldName = "ownerUserFullName", DisplayName = FieldsDisplayNames.Owner, IsVisible = true, IsFilterable = true, IsSortable = true, LinkTemplate = "/Users/Edit/{userId}" },
                    new GridField { FieldName = "ticketNumber", DisplayName = FieldsDisplayNames.TaskNumber, IsSortable = true, IsFilterable = true, LinkTemplate = "/SupportTickets/Edit/{ticketID}" },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:lastActivity" }
                };
            }
            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is NotificationsGridView templateRow)
            {
                //var creator = await _userService.GetUserByIDAsync(templateRow.CreatedBy); // ahagin dandaxecnuma 
                //var owner = await _userService.GetUserByIDAsync(templateRow.UserId);
                var creator = _users.FirstOrDefault(_ => _.Id == templateRow.CreatedBy);
                var owner = _users.FirstOrDefault(_ => _.Id == templateRow.UserId);
                var ticket = _supportTicket.FirstOrDefault(_ => _.Id == templateRow.TicketID);

                templateRow.CreatedByUserFullName = $"{creator?.FirstName} {creator?.LastName}";
                templateRow.OwnerUserFullName = $"{owner?.FirstName} {owner?.LastName}";
                templateRow.TicketNumber = ticket?.TicketID;
                templateRow.Type = AttributeDropdownHelper.GetValue<NotificationTypeAttribute>(templateRow.Type);
                if (templateRow.CreatedDate != null)
                    templateRow.CreatedDate = templateRow.CreatedDate.Value.ToLocalTime();
            }
        }
  
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

            var list = await _notificationService.GetAllNotificationsAsync();
            var data = list.Where(x => ids.Contains(x.Id)).ToList();

            return await ExportToExcel(data, "NotificationsSelected.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            var list = await _notificationService.GetAllNotificationsAsync();
            return await ExportToExcel(list, "Notifications.xlsx");
        }
        #endregion
    }
}
