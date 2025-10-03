namespace Partnerly.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Partnerly.Descriptors.Attributes;
    using Partnerly.Descriptors.Attributes.BaseAttributes;
    using Partnerly.Descriptors.Messages;
    using Partnerly.Helpers;
    using Partnerly.Infrastructure.Interfaces;
    using Partnerly.Infrastructure.Services;
    using Partnerly.Models;
    using Partnerly.Models.GridViews;
    using Partnerly.Models.ViewModels;
    using System.Security.Claims;

    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class EmailTemplatesController : _BaseController, IDataTableController
    {
        private readonly IEmailTemplateService _emailTemplateService;

        public EmailTemplatesController(IUserService userService, ICurrentUserService currentUser, IEmailTemplateService emailTemplateService)
        : base(userService, currentUser)
        {
            _emailTemplateService = emailTemplateService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
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

            return View();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template != null)
            {
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetData()
        {
            return await GetGridDataAsync<EmailTemplateGridView, EmailTemplate>("EmailTemplates");
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(EmailTemplate))
                return (IEnumerable<TEntity>)await _emailTemplateService.GetAllTemplatesAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields()
        {
            return new List<GridField>
            {
                new GridField { FieldName = "subject", DisplayName = FieldsDisplayNames.Subject, LinkTemplate = "/EmailTemplates/Edit/{id}" },
                new GridField { FieldName = "createdByUserFullName", DisplayName = FieldsDisplayNames.CreatorName, IsVisible = true },
                new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, Format="date:MM/dd/yyyy" }
            };
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is EmailTemplateGridView templateRow)
            {
                var user = await _userService.GetUserByIDAsync(templateRow.CreatedBy);
                templateRow.CreatedByUserFullName = $"{user?.FirstName} {user?.LastName}";
            }
        }
    }
}
