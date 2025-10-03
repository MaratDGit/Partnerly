using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UsersController : _BaseController, IDataTableController
    {
        protected readonly IRoleService _roleService;
        public UsersController(IRoleService roleService, IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var template = await _userService.GetUserByIDAsync(id);
            if (template == null) return NotFound();

            var templatesAsView = await PropertyActionsHelper.CopyPropertiesAsync(template, new UserViewModel());
            if (templatesAsView == null) return NotFound();

            return View(templatesAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UserViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                User? user = await _userService.GetUserByIDAsync(model.Id);
                if (user != null)
                {
                    //template.Subject = model.Subject;
                    //template.BodyHtml = model.BodyHtml;
                    ServiceResult<User?> result = await _userService.UpdateUserAsync(user);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _userService.GetUserByIDAsync(id);
            if (template == null) return NotFound();

            return View();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var template = await _userService.GetUserByIDAsync(id);
            if (template != null)
            {
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetData()
        {
            return await GetGridDataAsync<UserGridViewModel, User>("Users");
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(User))
                return (IEnumerable<TEntity>)await _userService.GetAllUsersAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields()
        {
            return new List<GridField>
            {
                new GridField { FieldName = "userName", DisplayName = $"{FieldsDisplayNames.FirstName} {FieldsDisplayNames.LastName}", LinkTemplate = "/Users/Edit/{id}" },
                new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Email, LinkTemplate = "/Users/Edit/{id}" },
                new GridField { FieldName = "myReferralCode", DisplayName = FieldsDisplayNames.ReferrerCode, LinkTemplate = "/Users/Edit/{id}" },
                new GridField { FieldName = "referrerName", DisplayName = FieldsDisplayNames.ReffererName, DefaultValue = "" },
                new GridField { FieldName = "phone", DisplayName = FieldsDisplayNames.Phone},
                new GridField { FieldName = "balance", DisplayName = FieldsDisplayNames.Balance, DefaultValue = "0"},
                new GridField { FieldName = "lastActivity", DisplayName = FieldsDisplayNames.LastActivity, Format="date:MM/dd/yyyy" },
                new GridField { FieldName = "isOnlayn", DisplayName = FieldsDisplayNames.IsOnlayn, Type = "checkbox"},
                new GridField { FieldName = "isBlocked", DisplayName = FieldsDisplayNames.IsBlocked, Type = "checkbox"},
                new GridField { FieldName = "emailConfirmed", DisplayName = FieldsDisplayNames.EmailConfirmed, Type = "checkbox"},
                new GridField { FieldName = "roleName", DisplayName = FieldsDisplayNames.Role},
                new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, Format="date:MM/dd/yyyy" }
            };
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is UserGridViewModel userRow)
            {
                userRow.UserName = $"{userRow.FirstName} {userRow.LastName}";

                if (userRow.ReferrerId != null)
                {
                    var Referrer = await _userService.GetUserByIDAsync(userRow.ReferrerId);
                    userRow.ReferrerName = $"{Referrer?.FirstName} {Referrer?.LastName}";
                }

                Role? role = await _roleService.GetRoleByIDAsync(userRow.RoleId);
                userRow.RoleName = role?.Name;
            }
        }
    }
}
