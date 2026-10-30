using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    [Authorize]
    public class AttachmentsController : _BaseController
    {
        private readonly IAttachmentService _attachmentService;
        private readonly ISupportTicketService _supportTicketService;
        private readonly IPermissionService _permissionService;

        public AttachmentsController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            IAttachmentService attachmentService,
            ISupportTicketService supportTicketService,
            IPermissionService permissionService,
            ILogService logService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _attachmentService = attachmentService;
            _supportTicketService = supportTicketService;
            _permissionService = permissionService;
        }

        [Authorize(Policy = "Permission:attachments.view")]
        public IActionResult Index()
        {
            AttachmentViewModel model = new AttachmentViewModel();
            model.attachments = AttachmentViewModel.CreateTableOptions(DataTableNames.attachmentsTable, _dataTableRegistry);

            return View("~/Views/Shared/DataTable/_DataTablePartial.cshtml", model.attachments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files, string entityType, Guid entityId)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files were uploaded.");

            if (string.IsNullOrWhiteSpace(entityType))
                return BadRequest("Entity type is required.");

            if (entityId == Guid.Empty)
                return BadRequest("Entity ID is required.");

            if (!await CanAccessEntityAttachmentsAsync(entityType, entityId))
                return Forbid();

            var attachments = new List<object>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                Attachment attachment;
                try
                {
                    attachment = await _attachmentService.UploadAsync(file, entityType, entityId);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { success = false, message = ex.Message });
                }

                attachments.Add(new
                {
                    id = attachment.Id,
                    fileName = attachment.FileName,
                    contentType = attachment.ContentType,
                    fileSize = attachment.FileSize
                });
            }

            return Json(new
            {
                success = true,
                attachments
            });
        }

        [HttpGet]
        public async Task<IActionResult> Get(string entityType, Guid entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return BadRequest("Entity type is required.");

            if (entityId == Guid.Empty)
                return BadRequest("Entity ID is required.");

            if (!await CanAccessEntityAttachmentsAsync(entityType, entityId))
                return Forbid();

            var attachments = await _attachmentService.GetAsync(entityType, entityId);

            return Json(attachments.Select(x => new
            {
                id = x.Id,
                fileName = x.FileName,
                contentType = x.ContentType,
                fileSize = x.FileSize,
                sortOrder = x.SortOrder
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            var attachment = await _attachmentService.GetByIdAsync(id);

            if (attachment == null)
                return NotFound();

            if (!await CanAccessEntityAttachmentsAsync(attachment.EntityType, attachment.EntityId))
                return Forbid();

            var stream = await _attachmentService.GetFileAsync(id);

            if (stream == null)
                return NotFound();

            return File(
                stream,
                attachment.ContentType,
                attachment.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:attachments.manage")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var attachment = await _attachmentService.GetByIdAsync(id);

            if (attachment == null)
                return NotFound();

            if (!await CanAccessEntityAttachmentsAsync(attachment.EntityType, attachment.EntityId))
                return Forbid();

            await _attachmentService.DeleteAsync(id);

            return Json(new
            {
                success = true
            });
        }

        [HttpGet]
        [Authorize(Policy = "Permission:attachments.view")]
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

            if (request.TableID == DataTableNames.attachmentsTable)
            {
                var visibleUserIds = UserVisibility.VisibleUserIds();
                var res = await _tableService.ExecuteAsync(
                   fields => _attachmentService.QueryEntity(WithQueryFields(fields, nameof(Attachment.CreatedBy)))
                       .Where(a => a.CreatedBy != null && visibleUserIds.Contains(a.CreatedBy.Value)),
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
        [Authorize(Policy = "Permission:attachments.view")]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });

            if (request.TableID == DataTableNames.attachmentsTable)
            {
                var visibleUserIds = UserVisibility.VisibleUserIds();
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _attachmentService.QueryEntity(WithQueryFields(fields, nameof(Attachment.CreatedBy)))
                                .Where(a => a.CreatedBy != null && visibleUserIds.Contains(a.CreatedBy.Value)),
                            "attachments");
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

        private async Task<bool> CanAccessEntityAttachmentsAsync(string entityType, Guid entityId)
        {
            if (string.Equals(entityType, "SupportTicket", StringComparison.OrdinalIgnoreCase))
            {
                var ticket = await _supportTicketService.GetSupportTicketByIDAsync(entityId);
                return await CanViewSupportTicketAsync(ticket);
            }

            if (string.Equals(entityType, "User", StringComparison.OrdinalIgnoreCase))
                return await CanViewUserAsync(entityId);

            return await _permissionService.HasPermissionAsync(_currentUser.UserId, PermissionCodes.AttachmentsView);
        }
    }
}
