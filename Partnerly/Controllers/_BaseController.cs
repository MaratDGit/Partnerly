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
                var dashboardModel = await GetUserProfileAsync();
                if (dashboardModel != null)
                {
                    ViewData["DashboardsViewModel"] = dashboardModel;
                }
            }

            await next();
        }

        #region User Profile info
        private async Task<DashboardsViewModel?> GetUserProfileAsync()
        {
            var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            if (user == null || user.IsBlocked == true)
                return null;

            return new DashboardsViewModel
            {
                UserPhotoUrl = string.IsNullOrEmpty(user.PhotoUrl)
                    ? Constants.DefaultUserProfilePhotoPath
                    : user.PhotoUrl,
                UserFirstName = user.FirstName,
                UserLastName = user.LastName,
                UserRefCode = user.MyReferralCode,
            };
        }

        protected bool HasPermision(string roleType)
        {
            if (roleType == RoleTypeAttribute.View) return _currentUser?.Role == RoleTypeAttribute.Admin || _currentUser?.Role == RoleTypeAttribute.Employee || _currentUser?.Role == RoleTypeAttribute.User;
            if (roleType == RoleTypeAttribute.Update) return _currentUser?.Role == RoleTypeAttribute.Admin || _currentUser?.Role == RoleTypeAttribute.Employee;
            if (roleType == RoleTypeAttribute.Delete) return _currentUser?.Role == RoleTypeAttribute.Admin;

            return false;
        }
        #endregion

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
