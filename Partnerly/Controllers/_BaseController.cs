using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using System.Threading.Tasks;

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
        protected readonly DataTableQueryService _tableService;
        protected readonly IDataTableRegistry _dataTableRegistry;
        protected readonly ISystemSettingsService _systemSettingsService;

        public _BaseController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
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
                
                SystemSettings? setting = await _systemSettingsService.GetCurrentAsync();
                if (setting?.IsMaintenanceMode == true)
                {
                    message = string.IsNullOrWhiteSpace(setting.MaintenanceMessage)
                        ? Messages.SiteUnderMaintenanceMode
                        : setting.MaintenanceMessage;
                }
                else
                {
                    message = setting?.ScrollingText;
                }

                ViewBag.IsAiAssistantEnabled = setting?.IsAiAssistantEnabled != false;
                ViewBag.TimeZoneId = string.IsNullOrWhiteSpace(setting?.TimeZoneId)
                    ? SystemSettingsDefaults.TimeZoneId
                    : setting.TimeZoneId;
                ViewBag.ApplicationName = string.IsNullOrWhiteSpace(setting?.ApplicationName)
                    ? SystemSettingsDefaults.ApplicationName
                    : setting.ApplicationName;

                var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
                ViewData["Permissions"] = await permissionService.GetEffectivePermissionCodesAsync(_currentUser.UserId);
            }

            ViewData["ScrollingText"] = message;

            await next();
        }

        protected virtual bool IsSuperRole(Role? role)
        {
            return string.Equals(role?.Name?.Trim(), Constants.SuperRoleName, StringComparison.OrdinalIgnoreCase);
        }
        protected virtual bool IsSystemRole(string roleName)
        {
            return string.Equals(roleName.Trim(), Constants.Admin, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(roleName.Trim(), Constants.Employee, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(roleName.Trim(), Constants.User, StringComparison.OrdinalIgnoreCase);
        }
        protected IUserVisibilityService UserVisibility =>
            HttpContext.RequestServices.GetRequiredService<IUserVisibilityService>();

        protected async Task<bool> CanViewUserAsync(Guid? userId) =>
            await UserVisibility.CanViewUserAsync(userId);

        protected async Task<bool> CanViewSupportTicketAsync(SupportTicket? ticket) =>
            ticket != null && await UserVisibility.CanViewSupportTicketAsync(ticket.UserId, ticket.AssignedTo);

        protected static IEnumerable<string> WithQueryFields(IEnumerable<string> fields, params string[] extra) =>
            fields.Concat(extra);

        protected virtual async Task<bool> IsSuperUser(User? user = null)
        {
            if (user == null)
            {
                user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            }

            return string.Equals(
                user?.Email?.Trim(),
                Constants.SuperUserEmail,
                StringComparison.OrdinalIgnoreCase);
        }

        protected async Task<T> PrepareViewModelAsync<T>(T model) where T : IAuditableViewModel
        {
            if (model == null)
                return model;

            if (model.CreatedBy.HasValue)
            {
                var creator = await _userService.GetUserByIDAsync(model.CreatedBy.Value);

                if (creator != null)
                {
                    model.CreatedByUserName = $"{creator.FirstName} {creator.LastName}".Trim();
                }
            }

            if (model.UpdatedBy.HasValue)
            {
                var updater = await _userService.GetUserByIDAsync(model.UpdatedBy.Value);

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

            var items = await _tableService.ExecuteExportAsync(queryFactory, request, options);

            var settings = await _systemSettingsService.GetCurrentAsync();
            var timeZone = TimeZoneHelper.Resolve(settings?.TimeZoneId);
            var stamp = TimeZoneHelper.ToLocal(DateTime.UtcNow, timeZone);

            if (string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
            {
                var excelExporter = new DataTableExcelExporter();

                var bytes = excelExporter.Export(items, options.Columns, timeZone);

                var excelFileName = $"{fileName}-{stamp:yyyyMMdd-HHmmss}.xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    excelFileName);
            }

            var csvExporter = new DataTableCsvExporter();

            var csvBytes = csvExporter.Export(items, options.Columns, timeZone);

            var csvFileName = $"{fileName}-{stamp:yyyyMMdd-HHmmss}.csv";

            return File(
                csvBytes,
                "text/csv; charset=utf-8",
                csvFileName);
        }
    }
}
