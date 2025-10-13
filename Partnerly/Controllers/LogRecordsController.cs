using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class LogRecordsController : _BaseController
    {
        public LogRecordsController(IUserService userService, ICurrentUserService currentUser, ILogService logService)
        : base(userService, currentUser, logService)
        {
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> View(Guid id)
        {
            var log = await _logService.GetLogByIDAsync(id);
            if (log == null) return NotFound();

            var logAsView = await PropertyActionsHelper.CopyPropertiesAsync(log, new LogsViewModel());
            if (logAsView == null) return NotFound();

            return View(logAsView);
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.Log))
            {
                return await GetGridDataAsync<LogGridViewModel, Log>("Log");
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override List<GridAction> GetDefaultActions(object row)
        {
            if (row is LogGridViewModel logRow)
            {
                return new List<GridAction> 
                {
                    new GridAction { Name = "View", DisplayName = FieldsDisplayNames.Details, IsVisible = HasPermision(RoleTypeAttribute.Update), UrlTemplate = "/LogRecords/View/{id}", CssClass = "dropdown-item", Icon = "bx bx-show me-1" },
                };
            }
            else
            {
                return base.GetDefaultActions(row);
            }
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(Log))
                return (IEnumerable<TEntity>)await _logService.GetAllLogsAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is LogGridViewModel logRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "actionView", DisplayName = FieldsDisplayNames.Action },
                    new GridField { FieldName = "typeView", DisplayName = FieldsDisplayNames.Type },
                    new GridField { FieldName = "logMessage", DisplayName = FieldsDisplayNames.Message, LinkTemplate = "/LogRecords/View/{id}" },
                    new GridField { FieldName = "filePath", DisplayName = FieldsDisplayNames.FilePath, DefaultValue = "" },
                    new GridField { FieldName = "method", DisplayName = FieldsDisplayNames.Method},
                    new GridField { FieldName = "lineNumber", DisplayName = FieldsDisplayNames.LineNumber},
                    new GridField { FieldName = "creatorUserName", DisplayName = FieldsDisplayNames.CreatorName },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, Format="date:lastActivity"},
                };
            }

            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is LogGridViewModel logRow && logRow != null)
            {
                if (logRow.CreatedBy != null)
                {
                    var creator = await _userService.GetUserByIDAsync(logRow.CreatedBy);
                    logRow.CreatorUserName = $"{creator?.FirstName} {creator?.LastName}";
                }

                if (logRow.CreatedDate != null)
                    logRow.CreatedDate = logRow.CreatedDate.Value.ToLocalTime();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

            var logs = await _logService.GetAllLogsAsync();
            var data = logs.Where(x => ids.Contains(x.Id)).ToList();

            return await ExportToExcel(data, "logs.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            var allData = JsonSerializer.Deserialize<List<LoginViewModel>>(data);
            var logs = await _logService.GetAllLogsAsync();
            return await ExportToExcel(logs, "logs.xlsx");
        }
    }
}
