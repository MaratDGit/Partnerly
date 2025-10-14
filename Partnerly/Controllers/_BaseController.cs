using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeOpenXml;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.GridViews;

namespace Partnerly.Controllers
{
    [Authorize] // только для авторизованных
    public class _BaseController : Controller
    {
        protected readonly IEventBus _eventBus;
        protected readonly IUserService _userService;
        protected readonly ICurrentUserService _currentUser;
        protected readonly ILogService _logService;   

        public _BaseController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService)
        {
            _eventBus = eventBus;
            _userService = userService;
            _currentUser = currentUser;
            _logService = logService;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.UserPhotoUrl = _currentUser.UserPhotoUrl;
                ViewBag.UserFirstName = _currentUser.FirstName;
                ViewBag.UserLastName = _currentUser.LastName;
                ViewBag.UserRefCode = _currentUser.ReffCode;
                ViewBag.UserFullName = $"{_currentUser.FirstName} {_currentUser.LastName}";
                ViewBag.RoleType = _currentUser.RoleType;
                //var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
                //if (user != null)
                //{
                    
                //}
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            string? roleType = null;
            bool unknownAction = false;
            if (!string.IsNullOrEmpty(actionName))
            {
                if (actionName.ToLower() == Constants.Edit.ToLower()) roleType = RoleTypeAttribute.Update;
                else if (actionName.ToLower() == Constants.Delete.ToLower()
                    || actionName.ToLower() == Constants.DeleteConfirmed.ToLower()
                    || actionName.ToLower() == Constants.Create.ToLower()) roleType = RoleTypeAttribute.Delete;
                else unknownAction = true;
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
