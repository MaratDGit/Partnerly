using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    public class WebInspectorController : _BaseController
    {
        private readonly IWebPageInspectorService _inspector;
        private readonly IWebHostEnvironment _env;

        public WebInspectorController(
            IEventBus eventBus,
            IUserService userService,
            ICurrentUserService currentUser,
            ILogService logService,
            ISystemSettingsService systemSettingsService,
            IStringLocalizer<SharedResource> localizer,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry,
            IWebPageInspectorService inspector,
            IWebHostEnvironment env)
            : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _inspector = inspector;
            _env = env;
        }

        [HttpGet]
        [Authorize(Policy = "Permission:web_inspector.use")]
        public IActionResult Index()
        {
            ViewBag.ExtensionFolder = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath,
                "..",
                "tools",
                "partnerly-web-inspector-extension"));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:web_inspector.use")]
        public async Task<IActionResult> Inspect(
            [FromBody] WebInspectRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _inspector.InspectAsync(request?.Url, cancellationToken);
            if (!result.Success)
            {
                return Json(new
                {
                    success = false,
                    message = MapError(result.ErrorCode)
                });
            }

            return Json(new
            {
                success = true,
                data = result
            });
        }

        private string MapError(string? code) =>
            code switch
            {
                WebPageInspectResult.ErrorInvalidUrl => _localizer["WebInspectorInvalidUrl"].Value,
                WebPageInspectResult.ErrorBlockedUrl => _localizer["WebInspectorBlockedUrl"].Value,
                WebPageInspectResult.ErrorTimeout => _localizer["WebInspectorTimeout"].Value,
                WebPageInspectResult.ErrorTooLarge => _localizer["WebInspectorTooLarge"].Value,
                _ => _localizer["WebInspectorFetchFailed"].Value
            };
    }
}
