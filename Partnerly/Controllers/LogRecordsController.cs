using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    public class LogRecordsController : _BaseController
    {
        public LogRecordsController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
        }

        [Authorize(Policy = "Permission:logs.view")]
        public IActionResult Index()
        {
            LogsViewModel model = new LogsViewModel();
            model.logs = LogsViewModel.CreateTableOptions(DataTableNames.logsTable, _dataTableRegistry);

            return View("~/Views/Shared/DataTable/_DataTablePartial.cshtml", model.logs);
        }

        [Authorize(Policy = "Permission:logs.view")]
        public async Task<IActionResult> View(Guid id)
        {
            var log = await _logService.GetLogByIDAsync(id);
            if (log == null) return NotFound();
            if (log.CreatedBy != null && !await CanViewUserAsync(log.CreatedBy))
                return Forbid();

            var logAsView = await PropertyActionsHelper.CopyPropertiesAsync(log, new LogsViewModel());
            if (logAsView == null) return NotFound();

            await PrepareViewModelAsync(logAsView);

            return View(logAsView);
        }

        [HttpGet]
        [Authorize(Policy = "Permission:logs.view")]
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

            if (request.TableID == DataTableNames.logsTable)
            {
                var visibleUserIds = UserVisibility.VisibleUserIds();
                var res = await _tableService.ExecuteAsync(
                   fields => _logService.QueryEntity(WithQueryFields(fields, nameof(Log.CreatedBy)))
                       .Where(l => l.CreatedBy != null && visibleUserIds.Contains(l.CreatedBy.Value)),
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
        [Authorize(Policy = "Permission:logs.view")]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });

            if (request.TableID == DataTableNames.logsTable)
            {
                var visibleUserIds = UserVisibility.VisibleUserIds();
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _logService.QueryEntity(WithQueryFields(fields, nameof(Log.CreatedBy)))
                                .Where(l => l.CreatedBy != null && visibleUserIds.Contains(l.CreatedBy.Value)),
                            "logs");
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
