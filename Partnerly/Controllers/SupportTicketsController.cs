using Microsoft.AspNetCore.Mvc;
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
using System.Security.Claims;

namespace Partnerly.Controllers
{
    public class SupportTicketsController : _BaseController
    {
        protected readonly ISupportTicketService _supportTicketService;
        public SupportTicketsController(IEventBus eventBus, ISupportTicketService supportTicketService, IUserService userService, ICurrentUserService currentUser, ILogService logService)
        : base(eventBus, userService, currentUser, logService)
        {
            _supportTicketService = supportTicketService;
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> OpenNewCase()
        {
            var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            SupportTicketViewModel model = new SupportTicketViewModel { UserId = _currentUser.UserId, CreatorName = $"{user?.FirstName} {user?.LastName} / {user?.MyReferralCode}", Status = SupportTicketStatusAttribute.New };

            if (await HasLimitExpired(_currentUser.UserId))
            {
                TempData["CaseCreationLimit"] = Messages.CaseCreationLimit;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenNewCase(SupportTicketViewModel model)
        {
            if (await HasLimitExpired(_currentUser.UserId))
            {
                TempData["CaseCreationLimit"] = Messages.CaseCreationLimit;
                return View(model);
            }

            if (ModelState.IsValid)
            {
                User? user = await _userService.GetUserByIDAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("Subject", "Error");
                    return View(model);
                }

                SupportTicket newTicket = new SupportTicket();
                newTicket.Subject = model?.Subject;
                newTicket.Message = model?.Message;
                newTicket.UserId = model.UserId;

                var result = await _supportTicketService.CreateSupportTicketAsync(newTicket);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("Subject", error);
                    }
                    return View(model);
                }

                TempData["SupportSuccess"] = Messages.SupportSuccess;
                return RedirectToAction(nameof(OpenNewCase));
            }
            return View(model);
        }

        public async Task<bool> HasLimitExpired(Guid? userID)
        {
            bool retval = false;
            if (_currentUser.RoleName == RoleTypeAttribute.User)
            {
                var cases = await _supportTicketService.GetUserSupportTicketsAsync(userID);

                if (cases != null)
                {
                    var todayCases = cases.Where(_ => _.CreatedDate.Value.Day == DateTime.Now.Day).ToList();
                    if (todayCases.Count() >= 5)
                    {
                        return true;
                    }
                }
            }
            return retval;
        }

        public async Task<IActionResult> ViewCase(Guid? id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();

            var ticketAsView = await PropertyActionsHelper.CopyPropertiesAsync(ticket, new SupportTicketViewModel());
            if (ticketAsView == null) return NotFound();

            var creator = await _userService.GetUserByIDAsync(ticketAsView.UserId);
            ticketAsView.CreatorName = $"{creator?.FirstName} {creator?.LastName}";

            if (ticketAsView.AssignedTo != null)
            {
                var assigned = await _userService.GetUserByIDAsync(ticketAsView.AssignedTo);
                ticketAsView.AssignedToName = $"{assigned?.FirstName} {assigned?.LastName}";
            }

            return View(ticketAsView);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
        public async Task<IActionResult> Create()
        {
            SupportTicketViewModel model = new SupportTicketViewModel() { UserId = _currentUser.UserId, Status = SupportTicketStatusAttribute.New,};    
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
        public async Task<IActionResult> Create(SupportTicketViewModel model)
        {
            if (ModelState.IsValid)
            {
                User? user = await _userService.GetUserByIDAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("Subject", "Error");
                    return View(model);
                }

                SupportTicket newTicket = new SupportTicket();
                newTicket.Subject = model?.Subject;
                newTicket.Message = model?.Message;
                newTicket.UserId = model.UserId;
                newTicket.Status = model?.Status;
                newTicket.AssignedTo = model?.AssignedTo;

                var result = await _supportTicketService.CreateSupportTicketAsync(newTicket);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        TempData["FormToast"] = error;
                    }
                    return View(model);
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = result.Data.Id });
            }
            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();

            var ticketAsView = await PropertyActionsHelper.CopyPropertiesAsync(ticket, new SupportTicketViewModel());
            if (ticketAsView == null) return NotFound();

            var creator = await _userService.GetUserByIDAsync(ticketAsView.UserId);
            ticketAsView.CreatorName = $"{creator?.FirstName} {creator?.LastName}";

            if (ticketAsView.AssignedTo != null)
            {
               var assigned = await _userService.GetUserByIDAsync(ticketAsView.AssignedTo);
                ticketAsView.AssignedToName = $"{assigned?.FirstName} {assigned?.LastName}";
            }

            return View(ticketAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
        public async Task<IActionResult> Edit(Guid id, SupportTicketViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                SupportTicket? ticket = await _supportTicketService.GetSupportTicketByIDAsync(model.Id);
                if (ticket != null)
                {
                    ticket.Subject = model.Subject;
                    ticket.Message = model.Message;
                    ticket.IsRead = model.IsRead;
                    ticket.AssignedTo = model.AssignedTo;
                    ticket.Status = model.Status;

                    ServiceResult<SupportTicket?> result = await _supportTicketService.UpdateSupportTicketAsync(ticket);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            TempData["FormToast"] = error;
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
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();

            return View();
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket != null)
            {
                ServiceResult<SupportTicket?> result = await _supportTicketService.DeleteSupportTicketAsync(id);
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
            if (fieldName == "Subject")
            {
                var query = await _supportTicketService.GetAllSupportTicketAsync();

                var result = query
                    .Select(u => new
                    {
                        id = u.Id,
                        subject = u.Subject,
                        status = u.Status,
                        isRead = u.IsRead
                    })
                    .OrderBy(u => u.subject)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.SupportTicket))
            {
                return await GetGridDataAsync<SupportTicketGridView, SupportTicket>("SupportTickets");
            }
            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(SupportTicket))
                return (IEnumerable<TEntity>)await _supportTicketService.GetAllSupportTicketAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is SupportTicketGridView)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "ticketID", DisplayName = $"{FieldsDisplayNames.TaskNumber}", IsSortable = true, LinkTemplate = "/SupportTickets/Edit/{id}" },
                    new GridField { FieldName = "subject", DisplayName = $"{FieldsDisplayNames.Subject}", LinkTemplate = "/SupportTickets/Edit/{id}" },
                    new GridField { FieldName = "message", DisplayName = FieldsDisplayNames. Message},
                    new GridField { FieldName = "status", DisplayName = FieldsDisplayNames.Status, IsSortable = true, IsFilterable = true },
                    new GridField { FieldName = "creatorName", DisplayName = FieldsDisplayNames.CreatorName, IsSortable = true, IsFilterable = true, LinkTemplate = "/Users/Edit/{userId}"},
                    new GridField { FieldName = "assignedToName", DisplayName = FieldsDisplayNames.AssignedTo, DefaultValue = "", IsSortable = true, IsFilterable = true,  LinkTemplate = "/Users/Edit/{assignedTo}" },
                    new GridField { FieldName = "isRead", DisplayName = FieldsDisplayNames.IsRead, Type = "checkbox", IsSortable = true, IsFilterable = true},
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }

            return fields;
        }

        protected override List<GridAction> GetDefaultActions(object row)
        {
            List<GridAction> actions = base.GetDefaultActions(row);

            if (row is SupportTicketGridView)
            {
                actions.Add(new GridAction { Name = "ViewCase", DisplayName = FieldsDisplayNames.Viewcase, IsVisible = HasPermision(RoleTypeAttribute.Update), UrlTemplate = "/SupportTickets/ViewCase/{id}", CssClass = "dropdown-item", Icon = "bx bx-show me-1" });
            }

            return actions;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is SupportTicketGridView ticket)
            {
                var creator = await _userService.GetUserByIDAsync(ticket.UserId);
                if (creator != null)
                {
                    ticket.CreatorName = $"{creator.FirstName} {creator.LastName}";
                }

                if (ticket.AssignedTo != null)
                {
                    var assigned = await _userService.GetUserByIDAsync(ticket.AssignedTo);
                    if (assigned != null)
                    {
                        ticket.AssignedToName = $"{assigned.FirstName} {assigned.LastName}";
                    }
                }
                ticket.Status = AttributeDropdownHelper.GetValue<SupportTicketStatusAttribute>(ticket.Status);
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        //{
        //    var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

        //    var users = await _userService.GetAllUsersAsync();
        //    var data = users.Where(x => ids.Contains(x.Id)).ToList();

        //    return await ExportToExcel(data, "users.xlsx");
        //}

        //[HttpPost]
        //public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        //{
        //    var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
        //    var users = await _userService.GetAllUsersAsync();
        //    return await ExportToExcel(users, "users.xlsx");
        //}

    }
}
