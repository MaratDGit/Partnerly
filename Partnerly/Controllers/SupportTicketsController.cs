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
        private readonly IAttachmentService _attachmentService;
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
            IInlineUpdateService inlineUpdateService,
            IAttachmentService attachmentService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _supportTicketService = supportTicketService;
            _inlineUpdateService = inlineUpdateService;
            _attachmentService = attachmentService;
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
            var model = await BuildOpenNewCaseModelAsync();
            if (await HasLimitExpired(_currentUser.UserId))
            {
                ViewBag.CaseCreationLimit = Messages.CaseCreationLimit;
            }

            return OpenNewCaseResult(model);
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
            model ??= new SupportTicketViewModel();
            await FillOpenNewCaseModelAsync(model);

            if (await HasLimitExpired(_currentUser.UserId))
            {
                ViewBag.CaseCreationLimit = Messages.CaseCreationLimit;
                if (IsAjax())
                {
                    return OpenNewCaseJson(false, Messages.CaseCreationLimit);
                }

                return OpenNewCaseResult(model);
            }

            if (ModelState.IsValid)
            {
                model.UserId = _currentUser.UserId;
                User? user = await _userService.GetUserByIDAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError("Subject", "Error");
                    return OpenNewCaseInvalid(model);
                }

                SupportTicket newTicket = new SupportTicket();
                newTicket.Subject = model.Subject;
                newTicket.Message = model.Message;
                newTicket.UserId = model.UserId;

                var result = await _supportTicketService.CreateSupportTicketAsync(newTicket);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("Subject", error);
                    }
                    return OpenNewCaseInvalid(model);
                }

                if (IsAjax())
                {
                    return OpenNewCaseJson(true, Messages.SupportSuccess);
                }

                TempData["SupportSuccess"] = Messages.SupportSuccess;
                return RedirectToAction(nameof(OpenNewCase));
            }

            return OpenNewCaseInvalid(model);
        }

        private bool IsAjax()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task<SupportTicketViewModel> BuildOpenNewCaseModelAsync()
        {
            var model = new SupportTicketViewModel
            {
                UserId = _currentUser.UserId,
                Status = SupportTicketStatusAttribute.New
            };
            await FillOpenNewCaseModelAsync(model);
            return model;
        }

        private async Task FillOpenNewCaseModelAsync(SupportTicketViewModel model)
        {
            var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            model.UserId = _currentUser.UserId;
            model.Status = string.IsNullOrWhiteSpace(model.Status)
                ? SupportTicketStatusAttribute.New
                : model.Status;
            model.CreatorName = $"{user?.FirstName} {user?.LastName} / {user?.MyReferralCode}";
        }

        private IActionResult OpenNewCaseResult(SupportTicketViewModel model)
        {
            if (IsAjax())
            {
                return PartialView("~/Views/Shared/Support/_OpenNewCasePopup.cshtml", model);
            }

            ViewBag.SupportCaseStandalone = true;
            return View("OpenNewCase", model);
        }

        private IActionResult OpenNewCaseJson(bool success, string? message, Dictionary<string, string>? errors = null)
        {
            return Json(new { success, message, errors });
        }

        private IActionResult OpenNewCaseInvalid(SupportTicketViewModel model)
        {
            if (IsAjax())
            {
                var errors = ModelState
                    .Where(item => item.Value != null && item.Value.Errors.Count > 0)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!.Errors[0].ErrorMessage);
                var message = errors.Values.FirstOrDefault();
                return OpenNewCaseJson(false, message, errors);
            }

            return OpenNewCaseResult(model);
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

        private static string FormatTicketUserName(User? user)
        {
            if (user == null)
            {
                return string.Empty;
            }

            var name = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name)
                ? (user.Email ?? string.Empty)
                : name;
        }

        [HttpGet]
        [Authorize(Policy = "Permission:support_tickets.view")]
        public async Task<IActionResult> ViewCase(Guid? id)
        {
            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null) return NotFound();
            if (!await CanViewSupportTicketAsync(ticket)) return Forbid();

            var ticketAsView = await PropertyActionsHelper.CopyPropertiesAsync(ticket, new SupportTicketViewModel());
            if (ticketAsView == null) return NotFound();

            var creator = await _userService.GetUserByIDAsync(ticketAsView.UserId);
            ticketAsView.CreatorName = FormatTicketUserName(creator);
            ticketAsView.CreatorEmail = creator?.Email;
            ticketAsView.CreatorPhotoUrl = creator?.PhotoUrl;

            if (ticketAsView.AssignedTo != null)
            {
                var assigned = await _userService.GetUserByIDAsync(ticketAsView.AssignedTo);
                ticketAsView.AssignedToName = FormatTicketUserName(assigned);
                ticketAsView.AssignedToEmail = assigned?.Email;
                ticketAsView.AssignedToPhotoUrl = assigned?.PhotoUrl;
            }

            ticketAsView.IsAssignedToCurrentUser =
                ticketAsView.AssignedTo != null &&
                ticketAsView.AssignedTo == _currentUser.UserId;

            if (ticketAsView.UpdatedBy != null)
            {
                var updater = await _userService.GetUserByIDAsync(ticketAsView.UpdatedBy);
                ticketAsView.UpdatedByUserName = FormatTicketUserName(updater);
            }

            var files = await _attachmentService.GetAsync("SupportTicket", ticketAsView.Id);
            ticketAsView.TicketFiles = files
                .Select(file => new TicketFileItem
                {
                    Id = file.Id,
                    FileName = file.FileName,
                    FileSize = file.FileSize,
                    ContentType = file.ContentType
                })
                .ToList();

            return View(ticketAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:support_tickets.edit")]
        public async Task<IActionResult> ViewCase(Guid id, string? Note)
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            if (!await CanViewSupportTicketAsync(ticket))
            {
                return Forbid();
            }

            ticket.Note = Note;
            var result = await _supportTicketService.UpdateSupportTicketAsync(ticket);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    TempData["FormToast"] = error;
                }

                return RedirectToAction(nameof(ViewCase), new { id });
            }

            TempData["ToastMessage"] = Messages.RecordSaved;
            return RedirectToAction(nameof(ViewCase), new { id });
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
