using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    [Authorize] // только для авторизованных
    public class _BaseController : Controller
    {
        protected readonly IUserService _userService;
        protected readonly ICurrentUserService _currentUser;

        public _BaseController(IUserService userService, ICurrentUserService currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewData["DashboardsViewModel"] = new DashboardsViewModel
                {
                    UserPhotoUrl = _currentUser.UserPhotoUrl,
                    UserFirstName = _currentUser.FirstName,
                    UserLastName = _currentUser.LastName,
                    UserRefCode = _currentUser.ReffCode,
                };
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            string? roleType = null;
            bool unknownAction = false;
            if (!string.IsNullOrEmpty(actionName))
            {
                if (actionName.ToLower() == Constants.Edit.ToLower()) roleType = RoleTypeAttribute.Update;
                else if (actionName.ToLower() == Constants.Delete.ToLower()) roleType = RoleTypeAttribute.Delete;
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
        protected virtual List<GridAction> GetDefaultActions()
        {
            return new List<GridAction>
            {
                new GridAction { Name = "Edit", DisplayName = FieldsDisplayNames.Edit, IsVisible = HasPermision(RoleTypeAttribute.Update), UrlTemplate = "/{controller}/Edit/{id}", CssClass = "dropdown-item", Icon = "bx bx-edit-alt me-1" },
                new GridAction { Name = "Delete", DisplayName = FieldsDisplayNames.Delete, IsVisible = HasPermision(RoleTypeAttribute.Delete), UrlTemplate = "/{controller}/Delete/{id}", CssClass = "dropdown-item", Icon = "bx bx-trash me-1" }
            };
        }
        #endregion

        #region Fields
        protected virtual List<GridField> GetFields()
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
            return GetDefaultActions().Select(a => new GridAction
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
                fields = GetFields(),
                data = list
            });
        }
        protected virtual Task CustomizeRowAsync(object row)
        {
            return Task.CompletedTask;
        }
        #endregion
        #endregion
    }
}
