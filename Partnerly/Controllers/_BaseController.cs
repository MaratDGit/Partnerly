using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using OfficeOpenXml;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.GridViews;

namespace Partnerly.Controllers
{
    [Authorize] // for authorized users only
    public class _BaseController : Controller
    {
        protected readonly IEventBus _eventBus;
        protected readonly IUserService _userService;
        protected readonly ICurrentUserService _currentUser;
        protected readonly ILogService _logService;
        protected readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ISystemSettingsService _systemSettingsService;
        protected readonly DataTableQueryService _tableService;
        protected readonly IDataTableRegistry _dataTableRegistry;

        public _BaseController(IEventBus eventBus, IUserService userService, 
            ICurrentUserService currentUser, ILogService logService, IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, DataTableQueryService tableService, IDataTableRegistry dataTableRegistry)
        {
            _eventBus = eventBus;
            _userService = userService;
            _currentUser = currentUser;
            _logService = logService;
            _localizer = localizer;
            _systemSettingsService = systemSettingsService;
            _tableService = tableService;
            _dataTableRegistry = dataTableRegistry;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string? message = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.UserPhotoUrl = _currentUser.UserPhotoUrl;
                ViewBag.UserFirstName = _currentUser.FirstName;
                ViewBag.UserLastName = _currentUser.LastName;
                ViewBag.UserRefCode = _currentUser.ReffCode;
                ViewBag.UserFullName = $"{_currentUser.FirstName} {_currentUser.LastName}";
                ViewBag.RoleType = _currentUser.RoleType;
                
                SystemSettings? setting = await _systemSettingsService.GetSetupByIDAsync(1);
                message = setting?.IsMaintenanceMode == true ? Messages.SiteUnderMaintenanceMode : setting?.ScrollingText;
            }

            ViewData["ScrollingText"] = message;

            #region every click/action URL is being checked 
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();
            string? roleType = null;
            bool unknownAction = false;
            if (!string.IsNullOrEmpty(actionName))
            {
                if (actionName.ToLower() == Constants.Edit.ToLower() || actionName.ToLower() == Constants.Create.ToLower()) 
                    roleType = RoleTypeAttribute.Update;
                else if (actionName.ToLower() == Constants.Delete.ToLower() || actionName.ToLower() == Constants.DeleteConfirmed.ToLower()) 
                    roleType = RoleTypeAttribute.Delete;
                else 
                    unknownAction = true;
            }
            if (!unknownAction && !HasPermision(roleType))
            {
                var referer = context.HttpContext.Request.Headers["Referer"].ToString();

                if (!string.IsNullOrEmpty(referer))
                {
                    context.Result = new RedirectResult(referer);
                    return;
                }
                else
                {
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }
            #endregion

            await next();
        }

        protected bool HasPermision(string? roleType)
        {
            bool retval = false;
            if (string.IsNullOrEmpty(roleType))
                return retval;

            if (roleType == RoleTypeAttribute.View) return _currentUser?.RoleName == RoleTypeAttribute.Admin || _currentUser?.RoleName == RoleTypeAttribute.Employee || _currentUser?.RoleName == RoleTypeAttribute.User;
            if (roleType == RoleTypeAttribute.Update) return _currentUser?.RoleName == RoleTypeAttribute.Admin || _currentUser?.RoleName == RoleTypeAttribute.Employee;
            if (roleType == RoleTypeAttribute.Delete) return _currentUser?.RoleName == RoleTypeAttribute.Admin;

            return false;
        }

        protected async Task<T> PrepareViewModelAsync<T>(T model)
        where T : IAuditableViewModel
        {
            if (model == null)
                return model;

            if (model.CreatedBy.HasValue)
            {
                var creator = await _userService
                    .GetUserByIDAsync(model.CreatedBy.Value);

                if (creator != null)
                {
                    model.CreatedByUserName = $"{creator.FirstName} {creator.LastName}".Trim();
                }
            }

            if (model.UpdatedBy.HasValue)
            {
                var updater = await _userService
                    .GetUserByIDAsync(model.UpdatedBy.Value);

                if (updater != null)
                {
                    model.UpdatedByUserName = $"{updater.FirstName} {updater.LastName}".Trim();
                }
            }

            return model;
        }

        protected async Task<IActionResult> ExportTableAsync<T>(DataTableRequest request, string format, Func<IEnumerable<string>, IQueryable<T>> queryFactory, string fileName)
        {
            var options = _dataTableRegistry.Get(request.TableID);

            var items = await _tableService.ExecuteExportAsync(
                queryFactory,
                request,
                options);

            if (string.Equals(
                format,
                "excel",
                StringComparison.OrdinalIgnoreCase))
            {
                var excelExporter = new DataTableExcelExporter();

                var bytes = excelExporter.Export(
                    items,
                    options.Columns);

                var excelFileName =
                    $"{fileName}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    excelFileName);
            }

            var csvExporter = new DataTableCsvExporter();

            var csvBytes = csvExporter.Export(
                items,
                options.Columns);

            var csvFileName =
                $"{fileName}-{DateTime.Now:yyyyMMdd-HHmmss}.csv";

            return File(
                csvBytes,
                "text/csv; charset=utf-8",
                csvFileName);
        }

        #region Data Grid Related Functions
        #region Default Actions
        protected virtual List<GridAction> GetDefaultActions(object row)
        {
            return new List<GridAction>
            {
                new GridAction { Name = "Edit", DisplayName = FieldsDisplayNames.Edit, IsVisible = HasPermision(RoleTypeAttribute.Update), UrlTemplate = "/{controller}/Edit/{id}", CssClass = "dropdown-item", Icon = "bx bx-edit-alt me-1" },
                new GridAction { Name = "Delete", DisplayName = FieldsDisplayNames.Delete, IsVisible = HasPermision(RoleTypeAttribute.Delete), UrlTemplate = "/{controller}/Delete/{id}", CssClass = "dropdown-item", Icon = "bx bx-trash me-1" }
            };
        }
        #endregion

        #region Fields
        protected virtual List<GridField> GetFields(object row)
        {
            return new List<GridField>();
        }
        #endregion

        #region Entities
        protected virtual Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>() where TEntity : class
        {
            return Task.FromResult(Enumerable.Empty<TEntity>());
        }

        #endregion

        #region Row Actions
        protected virtual List<GridAction> GetRowActions<TViewModel>(TViewModel row, string controllerName)
        {
            return GetDefaultActions(row).Select(a => new GridAction
            {
                Name = a.Name,
                DisplayName = a.DisplayName,
                IsVisible = a.IsVisible,
                UrlTemplate = a.UrlTemplate?.Replace("{id}", row.GetType().GetProperty("Id")?.GetValue(row)?.ToString() ?? "")
                                           .Replace("{controller}", controllerName),
                CssClass = a.CssClass,
                Icon = a.Icon,
                Attr = a.Attr
            }).ToList();
        }
        #endregion

        #region GetGridData
        protected virtual async Task<JsonResult> GetGridDataAsync<TViewModel, TEntity>(string controllerName)
            where TViewModel : class, new()
            where TEntity : class
        {
            var entities = await GetEntitiesAsync<TEntity>();
            var list = new List<TViewModel>();

            if (entities != null && entities.Any())
            {
                list = await PropertyActionsHelper.CopyPropertiesListAsync<TEntity, TViewModel>(entities.ToList(), list);

                foreach (var row in list)
                {
                    row.GetType().GetProperty("Actions")?.SetValue(row, GetRowActions(row, controllerName));
                    await CustomizeRowAsync(row);
                }
            }

            return Json(new
            {
                fields = GetFields(list.FirstOrDefault()),
                data = list
            });
        }
        protected virtual Task CustomizeRowAsync(object row)
        {
            return Task.CompletedTask;
        }
        #endregion

        public async Task<IActionResult> ExportToExcel<T>(IEnumerable<T> data, string excelFileName)
        {
            ExcelPackage.License.SetNonCommercialOrganization("Branch Armenia");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Export");

                var properties = typeof(T).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = properties[i].Name;
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }

                int row = 2;
                foreach (var item in data)
                {
                    for (int col = 0; col < properties.Length; col++)
                    {
                        ws.Cells[row, col + 1].Value = properties[col].GetValue(item);
                    }
                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var fileContent = package.GetAsByteArray();
                return File(fileContent,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            excelFileName);
            }
        }
        #endregion
    }
}
