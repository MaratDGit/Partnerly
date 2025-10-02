namespace Partnerly.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Partnerly.Helpers;
    using Partnerly.Infrastructure.Interfaces;
    using Partnerly.Infrastructure.Services;
    using Partnerly.Models;
    using Partnerly.Models.GridViews;
    using Partnerly.Models.ViewModels;

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
            TempData["SaveMessage"] = null;
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

                    TempData["SaveMessage"] = "Запись успешно сохранена!";
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template == null) return NotFound();

            var templatesAsView = await PropertyActionsHelper.CopyPropertiesAsync(template, new EmailTemplateViewModel());
            if (templatesAsView == null) return NotFound();

            return View(templatesAsView);
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
            var templatesAsView = new List<EmailTemplateGridView>();
            var templates = await _emailTemplateService.GetAllTemplatesAsync();

            if (templates != null && templates.Count() > 0)
            {
                templatesAsView = await PropertyActionsHelper.CopyPropertiesListAsync<EmailTemplate, EmailTemplateGridView>(templates.ToList(), templatesAsView);

                if (templatesAsView != null)
                {
                    var tasks = templatesAsView.Select(async model =>
                    {
                        model.CreatedByUser = await _userService.GetUserByIDAsync(model.CreatedBy);
                    });
                    await Task.WhenAll(tasks);
                }
            }
            return Json(new { data = templatesAsView });

            //var templatesAsView = new List<EmailTemplateGridView>();
            //var templates = await _emailTemplateService.GetAllTemplatesAsync();

            //if (templates == null || templates.Count() == 0)
            //{
            //    return Json(new { data = templatesAsView });
            //}
            //templatesAsView = (List<EmailTemplateGridView>)templates;

            //var tasks = templatesAsView.Select(async model =>
            //{
            //    model.CreatedByUser = await _userService.GetUserByIDAsync(model.CreatedBy);
            //});
            //await Task.WhenAll(tasks);

            //return Json(new { data = templatesAsView });
        }
    }
}
