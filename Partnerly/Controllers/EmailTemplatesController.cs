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
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class EmailTemplatesController : _BaseController
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IInlineUpdateService _inlineUpdateService;

        public EmailTemplatesController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            IEmailTemplateService emailTemplateService, 
            ILogService logService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry,
            IInlineUpdateService inlineUpdateService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _emailTemplateService = emailTemplateService;
            _inlineUpdateService = inlineUpdateService;
        }

        public IActionResult Index()
        {
            EmailTemplateViewModel model = new EmailTemplateViewModel();
            model.templates = EmailTemplateViewModel.CreateTableOptions(DataTableNames.emailTemplatesTable, _dataTableRegistry);

            return View("~/Views/Shared/DataTable/_DataTablePartial.cshtml", model.templates);
        }

        public IActionResult Create()
        {
            EmailTemplateViewModel model = new EmailTemplateViewModel { Id = Guid.NewGuid() };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var template = await _emailTemplateService.GetTemplateByNameAsync(model.Name);
                if (template != null)
                {
                    ModelState.AddModelError("Name", $"{FieldsDisplayNames.TemplateType} {ErrorMessages.UniqueValue}");
                    return View(model);
                }

                EmailTemplate newTemp = new EmailTemplate();
                newTemp.Name = model.Name;
                newTemp.Subject = model.Subject;
                newTemp.BodyHtml = model.BodyHtml;
                newTemp.BodyPlain = model.BodyPlain;

                var result = await _emailTemplateService.CreateTemplateAsync(newTemp);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("Name", error);
                    }
                    return View(model);
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = result?.Data?.Id });
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template == null) return NotFound();

            var templatesAsView = await PropertyActionsHelper.CopyPropertiesAsync(template, new EmailTemplateViewModel());
            if (templatesAsView == null) return NotFound();

            await PrepareViewModelAsync(templatesAsView);

            return View(templatesAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EmailTemplateViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                EmailTemplate? template = await _emailTemplateService.GetTemplateByIDAsync(model.Id);
                if (template != null)
                {
                    template.Subject = model.Subject;
                    template.BodyHtml = model.BodyHtml;
                    template.Note = model.Note; 
                    ServiceResult<EmailTemplate?> result = await _emailTemplateService.UpdateTemplateAsync(template);
                    
                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("Subject", error);
                        }
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template == null) return NotFound();


            if (AttributeDropdownHelper.GetValue<EmailTemplateNameAttribute>(template.Name ?? "") != null)
            {
                var model = new { id = id };
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = ErrorMessages.CannotDeleteSystemRecords;

                return RedirectToAction("Edit", model);
            };

            var deleteModel = new DeleteConfirmationViewModel
            {
                Id = template.Id,
                Title = ErrorMessages.Deletingthetemplate,
                Message = ErrorMessages.Areyousureyouwanttodeletethisfile,
                DeleteAction = "DeleteConfirmed",
                DeleteController = "EmailTemplates",
                CancelAction = "Index",
                CancelController = "EmailTemplates"
            };


            return View("~/Views/Shared/_DeleteConfirmation.cshtml", deleteModel);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template != null)
            {
                ServiceResult<EmailTemplate?> result = await _emailTemplateService.DeleteTemplateAsync(id);
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
            if (fieldName == "Name")
            {
                var query = await _emailTemplateService.GetAllTemplatesAsync();

                var result = query
                    .Select(u => new
                    {
                        id = u.Id,
                        name = AttributeDropdownHelper.GetValue<EmailTemplateNameAttribute>(u.Name) ?? u.Name,
                        subject = u.Subject
                    })
                    .OrderBy(u => u.name)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }

        #region Grids 
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
                    message = "email Templates table."
                });
            }

            if (request.TableID == DataTableNames.emailTemplatesTable)
            {
                var res = await _tableService.ExecuteAsync(
                   fields => _emailTemplateService.QueryEntity(fields),
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInline(
        [FromBody] DataTableInlineUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "email Template ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var templateId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid email Template ID."
                });
            }

            var template = await _emailTemplateService.GetTemplateByIDAsync(templateId);

            if (template == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "email Template not found."
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);
            var result = _inlineUpdateService.ApplyChanges(
                template,
                request.Fields,
                options);

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
                await _emailTemplateService.UpdateTemplateAsync(template);
            }

            return Ok(new
            {
                success = true,
                changed = result.IsChanged
            });
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

            if (request.TableID == DataTableNames.emailTemplatesTable)
            {
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _emailTemplateService.QueryEntity(fields),
                            "emailTemplates");
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
