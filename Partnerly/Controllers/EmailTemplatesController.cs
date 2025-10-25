namespace Partnerly.Controllers
{
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
    using System.Text.Json;

    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class EmailTemplatesController : _BaseController
    {
        private readonly IEmailTemplateService _emailTemplateService;

        public EmailTemplatesController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, IEmailTemplateService emailTemplateService, ILogService logService)
        : base(eventBus, userService, currentUser, logService)
        {
            _emailTemplateService = emailTemplateService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
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
                            ModelState.AddModelError("", error);
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


            if (AttributeDropdownHelper.GetValue<EmailTemplateNameAttribute>(template.Name) != null)
            {
                var model = new { id = id };
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = ErrorMessages.CannotDeleteSystemRecords;
                return RedirectToAction("Edit", model);
            }

            return View();
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
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.EmailTemplate))
            {
                return await GetGridDataAsync<EmailTemplateGridView, EmailTemplate>("EmailTemplates");
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(EmailTemplate))
                return (IEnumerable<TEntity>)await _emailTemplateService.GetAllTemplatesAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is EmailTemplateGridView templateRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "subject", DisplayName = FieldsDisplayNames.Subject, LinkTemplate = "/EmailTemplates/Edit/{id}" },
                    new GridField { FieldName = "createdByUserFullName", DisplayName = FieldsDisplayNames.CreatorName, IsVisible = true },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }
            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is EmailTemplateGridView templateRow)
            {
                var user = await _userService.GetUserByIDAsync(templateRow.CreatedBy);
                templateRow.CreatedByUserFullName = $"{user?.FirstName} {user?.LastName}";
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

            var users = await _emailTemplateService.GetAllTemplatesAsync();
            var data = users.Where(x => ids.Contains(x.Id)).ToList();

            return await ExportToExcel(data, "Templates.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            //var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
            var users = await _emailTemplateService.GetAllTemplatesAsync();
            return await ExportToExcel(users, "Templates.xlsx");
        }
        #endregion
    }
}
