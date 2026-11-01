using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;

namespace Partnerly.Controllers
{
    public class DocumentationController : _BaseController
    {
        public DocumentationController(
            IEventBus eventBus,
            IUserService userService,
            ICurrentUserService currentUser,
            ILogService logService,
            ISystemSettingsService systemSettingsService,
            IStringLocalizer<SharedResource> localizer,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry)
            : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = _localizer["Documentation"].Value;
            return View("~/Views/Home/Documentation.cshtml");
        }
    }
}
