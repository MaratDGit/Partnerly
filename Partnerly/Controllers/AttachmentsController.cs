using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.GridViews;

namespace Partnerly.Controllers
{
    [Authorize]
    public class AttachmentsController : _BaseController
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, IAttachmentService attachmentService, ILogService logService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService)
        {
            _attachmentService = attachmentService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
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
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.Attachment))
            {
                return await GetGridDataAsync<AttachmentGridView, Attachment>("Attachments");
            }
            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(Attachment))
            {
                var att = await _attachmentService.GetAllAsync();

                return (IEnumerable<TEntity>)att;
            }

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is AttachmentGridView attRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "fileName", DisplayName = $"{FieldsDisplayNames.FileName}" },
                    //new GridField { FieldName = "storedFileName", DisplayName = FieldsDisplayNames.StoredFileName, },
                    new GridField { FieldName = "contentType", DisplayName = FieldsDisplayNames.ContentType, IsFilterable = true, IsSortable = true },
                    new GridField { FieldName = "fileSize", DisplayName = FieldsDisplayNames.FileSize, Format = "size"},
                    new GridField { FieldName = "storagePath", DisplayName = FieldsDisplayNames.StoragePath },
                    new GridField { FieldName = "entityType", DisplayName = FieldsDisplayNames.EntityType, IsFilterable = true, IsSortable = true },
                    //new GridField { FieldName = "entity", DisplayName = FieldsDisplayNames.Entity, IsFilterable = true, IsSortable = true },
                    new GridField { FieldName = "createdByUserFullName", DisplayName = FieldsDisplayNames.CreatorName, IsFilterable = true, IsSortable = true },
                    new GridField { FieldName = "isDeleted", DisplayName = FieldsDisplayNames.IsDeleted, IsFilterable = true, IsSortable = true, Type = "checkbox", DefaultValue = false},
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }

            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is AttachmentGridView attRow)
            {
                User? user = await _userService.GetUserByIDAsync(attRow.CreatedBy);
                attRow.CreatedByUserFullName = $"{user?.FirstName} {user?.LastName}";
            }
        }

        protected override List<GridAction> GetDefaultActions(object row)
        {
            List<GridAction> actions = base.GetDefaultActions(row);

            if (row is AttachmentGridView)
            {
                return new List<GridAction>();
            }

            return actions;
        }
    }
}