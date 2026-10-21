using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class RolesController : _BaseController
    {
        protected readonly IRoleService _roleService;
        public RolesController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser,
            ILogService logService, 
            IRoleService roleService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _roleService = roleService;
        }

        public IActionResult Index()
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
                modelAsView.Note = user.Note;   
            }

            if (modelAsView == null) return NotFound();

            modelAsView.Title = FieldsDisplayNames.RoleUpdated;
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
                    user.Note = model.Note;
                    ServiceResult<User?> result = await _userService.UpdateUserAsync(user);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View(model);
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetLookupList(string fieldName)
        {
            if (fieldName == "Name")
            {
                List<UserRolesViewModel> userRolesList = new List<UserRolesViewModel>();
                var roles = await _roleService.GetAllRolesAsync();
                var users = await _userService.GetAllUsersAsync();

                foreach (User? user in users)
                {
                    string? roleName = user?.Role?.Name;
                    if (roleName == null)
                    {
                        var role = roles.FirstOrDefault(_ => _.Id == user.RoleId);
                        roleName = role?.Name;
                    }
                    if (roleName != null)
                    {
                        UserRolesViewModel row = new UserRolesViewModel
                        {
                            Id = user.Id,
                            UserName = $"{user.FirstName} {user.LastName}",
                            Email = user.Email,
                            Phone = user.Phone,
                            OldRoleId = user.RoleId,
                            OldRoleName = roleName,
                        };
                        
                        userRolesList.Add(row);
                    }
                }

                var result = userRolesList
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.UserName,
                        role = u.OldRoleName
                    })
                    .OrderBy(u => u.name)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }
    }
}
