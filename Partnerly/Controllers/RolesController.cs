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
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class RolesController : _BaseController
    {
        protected readonly IRoleService _roleService;
        public RolesController(IUserService userService, ICurrentUserService currentUser, ILogService logService, IRoleService roleService)
        : base(userService, currentUser, logService)
        {
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userService.GetUserByIDAsync(id);
            if (user == null) return NotFound();

            string? roleName = user.Role?.Name;
            if (roleName == null)
            {
                var role = await _roleService.GetRoleByIDAsync(user.RoleId);
                roleName = role?.Name;
            }

            UserRolesViewModel? modelAsView = null;
            if (roleName != null)
            {
                modelAsView = new UserRolesViewModel();
                modelAsView.Id = user.Id;
                modelAsView.UserName = $"{user.FirstName} {user.LastName}";
                modelAsView.Email = user.Email;
                modelAsView.Phone = user.Phone;
                modelAsView.OldRoleId = user.RoleId;
                modelAsView.OldRoleName = roleName;
            }
            ViewBag.RoleNamesList = AttributeDropdownHelper.ToSelectedList(RoleTypeAttribute.RoleNames);

            if (modelAsView == null) return NotFound();

            return View(modelAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UserRolesViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                User? user = await _userService.GetUserByIDAsync(model.Id);
                Role? role = await _roleService.GetRoleByNameAsync(model.NewRoleName);
                if (user != null && role != null)
                {
                    user.RoleId = role.Id;
                    user.LastRoleUpdateTime = DateTime.UtcNow;
                    ServiceResult<User?> result = await _userService.UpdateUserAsync(user);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View(model);
                    }

                    ViewBag.RoleNamesList = AttributeDropdownHelper.ToSelectedList(RoleTypeAttribute.RoleNames);
                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            List<UserRolesGridViewModel> userRolesList = new List<UserRolesGridViewModel>();

            if (tableModel == "UserRoles")
            {
                var roles = await _roleService.GetAllRolesAsync();
                var users = await _userService.GetAllUsersAsync();
                
                foreach (User user in users)
                {
                    string? roleName = user.Role?.Name;
                    if (roleName == null)
                    {
                        var role = roles.FirstOrDefault(_ => _.Id == user.RoleId);
                        roleName = role?.Name;
                    }
                    if (roleName != null)
                    {
                        UserRolesGridViewModel row = new UserRolesGridViewModel
                        {
                            Id = user.Id,
                            UserName = $"{user.FirstName} {user.LastName}",
                            Email = user.Email,
                            Phone = user.Phone,
                            OldRoleId = user.RoleId,
                            OldRoleName = roleName,
                        };
                        row.Actions = GetRowActions(row, "Roles");
                        userRolesList.Add(row);
                    }
                }
            }

            return Json(new { fields = GetFields(userRolesList.FirstOrDefault()), data = userRolesList });
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is UserRolesGridViewModel userRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "userName", DisplayName = FieldsDisplayNames.User },
                    new GridField { FieldName = "oldRoleName", DisplayName = FieldsDisplayNames.Role, LinkTemplate = "/Roles/Edit/{id}"},
                    new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Email },
                    new GridField { FieldName = "phone", DisplayName = FieldsDisplayNames.Phone},
                    
                };
            }

            return fields;
        }

        protected override List<GridAction> GetDefaultActions(object row)
        {
            if (row is UserRolesGridViewModel logRow)
            {
                return new List<GridAction>
                {
                   new GridAction { Name = "Edit", DisplayName = FieldsDisplayNames.Edit, IsVisible = HasPermision(RoleTypeAttribute.Update), UrlTemplate = "/Roles/Edit/{id}", CssClass = "dropdown-item", Icon = "bx bx-edit-alt me-1" },
                };
            }
            else
            {
                return base.GetDefaultActions(row);
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        //{
        //    //var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

        //    //var users = await _userService.GetAllUsersAsync();
        //    //var data = users.Where(x => ids.Contains(x.Id)).ToList();

        //    //return await ExportToExcel(data, "users.xlsx");
        //}

        //[HttpPost]
        //public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        //{
        //    var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
        //    var users = await _userService.GetAllUsersAsync();
        //    return await ExportToExcel(users, "users.xlsx");
        //}
    }
}
