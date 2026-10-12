using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    [Authorize]
    public class AttachmentsController : _BaseController
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, IAttachmentService attachmentService, ILogService logService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService, DataTableQueryService tableService, IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _attachmentService = attachmentService;
        }

        public IActionResult Index()
        {
            AttachmentViewModel model = new AttachmentViewModel();
            model.attachments = AttachmentViewModel.CreateTableOptions(DataTableNames.attachmentsTable, _dataTableRegistry);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            List<IFormFile> files,
            string entityType,
            Guid entityId)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files were uploaded.");

            if (string.IsNullOrWhiteSpace(entityType))
                return BadRequest("Entity type is required.");

            if (entityId == Guid.Empty)
                return BadRequest("Entity ID is required.");

            var attachments = new List<object>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                var attachment = await _attachmentService.UploadAsync(
                    file,
                    entityType,
                    entityId);

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
        public async Task<IActionResult> Get(
            string entityType,
            Guid entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return BadRequest("Entity type is required.");

            if (entityId == Guid.Empty)
                return BadRequest("Entity ID is required.");

            var attachments = await _attachmentService.GetAsync(
                entityType,
                entityId);

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
        public async Task<IActionResult> Delete(Guid id)
        {
            var attachment = await _attachmentService.GetByIdAsync(id);

            if (attachment == null)
                return NotFound();

            await _attachmentService.DeleteAsync(id);

            return Json(new
            {
                success = true
            });
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

            if (request.TableID == DataTableNames.attachmentsTable)
            {
                var res = await _tableService.ExecuteAsync(
                   fields => _attachmentService.QueryEntity(fields),
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

            if (request.TableID == DataTableNames.attachmentsTable)
            {
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _attachmentService.QueryEntity(fields),
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
    }
}