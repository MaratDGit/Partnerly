namespace Partnerly.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Partnerly.Helpers;
    using Partnerly.Infrastructure.Interfaces;
    using Partnerly.Models;
    using Partnerly.Models.ViewModels;

    public class EmailTemplatesController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailTemplateService _emailTemplateService;

        public EmailTemplatesController(IUserService userService, ICurrentUserService currentUser, IEmailTemplateService emailTemplateService)
        {
            _userService = userService;
            _currentUser = currentUser;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<IActionResult> Index()
        {
            var templatesAsView = new List<EmailTemplateViewModel>();
            var templates = await _emailTemplateService.GetAllTemplatesAsync();

            if (templates != null && templates.Count() > 0)
            {
                templatesAsView = await PropertyActionsHelper.CopyPropertiesListAsync<EmailTemplate, EmailTemplateViewModel>(templates.ToList(), templatesAsView);

                if (templatesAsView != null)
                {
                    foreach (EmailTemplateViewModel model in templatesAsView.ToList())
                    {
                        model.CreatedByUser = await _userService.GetUserByIDAsync(model.CreatedBy);
                    }
                }
            }
            
            return View(templatesAsView);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template == null) return NotFound();

            var templatesAsView = await PropertyActionsHelper.CopyPropertiesAsync(template, new EmailTemplateViewModel());
            if (templatesAsView == null) return NotFound();

            return View(templatesAsView);
        }

        // Создать
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
                //_context.Add(template);
                //await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // Редактировать
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
                //_context.Update(template);
                //await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // Удалить
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
                //_context.EmailTemplates.Remove(template);
                //await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }

}
