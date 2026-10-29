using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class SupportTicketsController : _BaseController
    {
        protected readonly ISupportTicketService _supportTicketService;
        private readonly IInlineUpdateService _inlineUpdateService;
        public SupportTicketsController(
            IEventBus eventBus, 
            ISupportTicketService supportTicketService, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService,
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry,
            IInlineUpdateService inlineUpdateService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _supportTicketService = supportTicketService;
            _inlineUpdateService = inlineUpdateService;
        }

        [Authorize(Policy = "Permission:support_tickets.view")]
        public IActionResult Index()
        {
            SupportTicketViewModel model = new SupportTicketViewModel();
            model.tickets = SupportTicketViewModel.CreateTableOptions(DataTableNames.supportTicketsTable, _dataTableRegistry);

            return View("~/Views/Shared/DataTable/_DataTablePartial.cshtml", model.tickets);
        }

        [Authorize(Policy = "Permission:support_tickets.create")]
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

        [Authorize(Policy = "Permission:support_tickets.edit")]
        public async Task<IActionResult> StartCase(Guid? id)
        {
            if (id != null)
            {
                var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
                if (ticket == null || !await CanViewSupportTicketAsync(ticket))
                    return Forbid();
                string? status = ticket.Status;
                if (status != null && status != SupportTicketStatusAttribute.InProgress)
                {
                    ticket.IsRead = true;
                    ticket.Status = SupportTicketStatusAttribute.InProgress;
                    await _supportTicketService.ChangeTicketStatus(ticket, status);
                }
            }

            return RedirectToAction(nameof(ViewCase), new { id = id });
        }

        [Authorize(Policy = "Permission:support_tickets.edit")]
        public async Task<IActionResult> CloseCase(Guid? id)
        {
            if (id != null)
            {
                var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
                if (ticket == null || !await CanViewSupportTicketAsync(ticket))
                    return Forbid();
                string? status = ticket.Status;
                if (status != null && status != SupportTicketStatusAttribute.Closed)
                {
                    ticket.IsRead = true;
                    ticket.Status = SupportTicketStatusAttribute.Closed;
                    await _supportTicketService.ChangeTicketStatus(ticket, status);
                }
            }

            return RedirectToAction(nameof(ViewCase), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:support_tickets.create")]
        public async Task<IActionResult> OpenNewCase(SupportTicketViewModel model)
        {
            if (await HasLimitExpired(_currentUser.UserId))
            {
                TempData["CaseCreationLimit"] = Messages.CaseCreationLimit;
                return View(model);
            }

            if (ModelState.IsValid)
            {
                model.UserId = _currentUser.UserId;
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
            if (_currentUser.RoleName == Constants.User)
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

        [Authorize(Policy = "Permission:support_tickets.view")]
        public async Task<IActionResult> ViewCase(Guid? id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();
            if (!await CanViewSupportTicketAsync(ticket)) return Forbid();

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

        [Authorize(Policy = "Permission:support_tickets.create")]
        public IActionResult Create()
        {
            SupportTicketViewModel model = new SupportTicketViewModel() 
            { 
                Title = FieldsDisplayNames.SupportTicketCreated,
                UserId = _currentUser.UserId, 
                Status = SupportTicketStatusAttribute.New, 
            };
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:support_tickets.create")]
        public async Task<IActionResult> Create(SupportTicketViewModel model)
        {
            if (ModelState.IsValid && model != null)
            {
                if (!await CanViewUserAsync(model.UserId))
                    return Forbid();

                User? user = await _userService.GetUserByIDAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("Subject", "Error");
                    return View("Form", model);
                }

                SupportTicket newTicket = new SupportTicket();
                newTicket.Subject = model.Subject;
                newTicket.Message = model.Message;
                newTicket.UserId = model.UserId;
                newTicket.Status = model.Status;
                newTicket.AssignedTo = model.AssignedTo;

                var result = await _supportTicketService.CreateSupportTicketAsync(newTicket);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        TempData["FormToast"] = error;
                    }
                    return View("Form", model);
                }
                else if (result.Data != null)
                {
                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Edit", new { id = result.Data.Id });
                }
            }
            return View("Form", model);
        }

        [Authorize(Policy = "Permission:support_tickets.edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();
            if (!await CanViewSupportTicketAsync(ticket)) return Forbid();

            var ticketAsView = await PropertyActionsHelper.CopyPropertiesAsync(ticket, new SupportTicketViewModel());
            if (ticketAsView == null) return NotFound();

            var creator = await _userService.GetUserByIDAsync(ticketAsView.UserId);
            ticketAsView.CreatorName = $"{creator?.FirstName} {creator?.LastName}";

            if (ticketAsView.AssignedTo != null)
            {
               var assigned = await _userService.GetUserByIDAsync(ticketAsView.AssignedTo);
                ticketAsView.AssignedToName = $"{assigned?.FirstName} {assigned?.LastName}";
            }

            await PrepareViewModelAsync(ticketAsView);

            ticketAsView.Title = FieldsDisplayNames.SupportTicketUpdated;
            return View("Form", ticketAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:support_tickets.edit")]
        public async Task<IActionResult> Edit(Guid id, SupportTicketViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                SupportTicket? ticket = await _supportTicketService.GetSupportTicketByIDAsync(model.Id);
                if (ticket != null)
                {
                    if (!await CanViewSupportTicketAsync(ticket)) return Forbid();
                    string? oldStatus = ticket.Status;
                    Guid? oldAssignedTo = ticket.AssignedTo;

                    ticket.Subject = model.Subject;
                    ticket.Message = model.Message;
                    ticket.IsRead = model.IsRead;
                    ticket.AssignedTo = model.AssignedTo;
                    ticket.Status = model.Status;
                    ticket.Note = model.Note;

                    ServiceResult<SupportTicket?> result = await _supportTicketService.UpdateSupportTicketAsync(ticket, oldAssignedTo, oldStatus);

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

        [Authorize(Policy = "Permission:support_tickets.delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();
            if (!await CanViewSupportTicketAsync(ticket)) return Forbid();

            var deleteModel = new DeleteConfirmationViewModel
            {
                Id = ticket.Id,
                Title = ErrorMessages.Deletetask,
                Message = ErrorMessages.ConfirmDelete,
                DeleteAction = "DeleteConfirmed",
                DeleteController = "SupportTickets",
                CancelAction = "Index",
                CancelController = "SupportTickets"
            };

            return View("~/Views/Shared/_DeleteConfirmation.cshtml", deleteModel);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:support_tickets.delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket != null)
            {
                if (!await CanViewSupportTicketAsync(ticket)) return Forbid();
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
        [Authorize(Policy = "Permission:support_tickets.view")]
        public async Task<IActionResult> GetLookupList(string fieldName)
        {
            if (fieldName == "Subject")
            {
                var query = await _supportTicketService.GetAllSupportTicketAsync();
                var visibleIds = await UserVisibility.VisibleUserIds().ToListAsync();
                var currentId = _currentUser.UserId;
                query = query.Where(u => u != null && (
                    (u.UserId != null && visibleIds.Contains(u.UserId.Value)) ||
                    (currentId != null && u.AssignedTo == currentId)));

                var result = query
                    .Select(u => new
                    {
                        id = u.Id,
                        ticketID = u.TicketID,
                        subject = u.Subject,
                        status = AttributeDropdownHelper.GetValue<SupportTicketStatusAttribute>(u.Status),
                        isRead = u.IsRead
                    })
                    .OrderBy(u => u.subject)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }


        #region Grids 
        [HttpGet]
        [Authorize(Policy = "Permission:support_tickets.view")]
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
                    message = "Tickets table."
                });
            }

            if (request.TableID == DataTableNames.supportTicketsTable)
            {
                var visibleUserIds = UserVisibility.VisibleUserIds();
                var currentId = _currentUser.UserId;
                var res = await _tableService.ExecuteAsync(
                   fields => _supportTicketService.QueryEntityExt(fields)
                       .Where(t =>
                           (t.UserId != null && visibleUserIds.Contains(t.UserId.Value)) ||
                           (currentId != null && t.AssignedTo == currentId)),
                   request,
                   options);

                return Json(res);
            }

            return BadRequest(new
            {
                success = false,
                message = "Unknown table."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:support_tickets.edit")]
        public async Task<IActionResult> UpdateInline(
        [FromBody] DataTableInlineUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ticket ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var templateId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Ticket ID."
                });
            }

            var template = await _supportTicketService.GetSupportTicketByIDAsync(templateId);

            if (template == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Ticket not found."
                });
            }

            if (!await CanViewSupportTicketAsync(template))
                return Forbid();

            var oldStatus = template.Status;
            var oldAssignedTo = template.AssignedTo;

            var options = _dataTableRegistry.Get(request.TableID);
            var result = _inlineUpdateService.ApplyChanges(template, request.Fields, options);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            if (result.IsChanged)
            {
                await _supportTicketService.UpdateSupportTicketAsync(template, oldAssignedTo, oldStatus ?? string.Empty);
            }

            return Ok(new
            {
                success = true,
                changed = result.IsChanged
            });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:support_tickets.view")]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });

            if (request.TableID == DataTableNames.supportTicketsTable)
            {
                var visibleUserIds = UserVisibility.VisibleUserIds();
                var currentId = _currentUser.UserId;
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _supportTicketService.QueryEntityExt(fields)
                                .Where(t =>
                                    (t.UserId != null && visibleUserIds.Contains(t.UserId.Value)) ||
                                    (currentId != null && t.AssignedTo == currentId)),
                            "supportTickets");
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
        #endregion
    }
}
