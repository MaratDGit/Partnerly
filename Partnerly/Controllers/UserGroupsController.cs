using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UserGroupsController : _BaseController
    {
        private readonly IUserGroupService _userGroupService;

        public UserGroupsController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, IUserGroupService userGroupService)
        : base(eventBus, userService, currentUser, logService)
        {
            _userGroupService = userGroupService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _userGroupService.GetByGroupNameAsync(model.Name);
                if (existing != null)
                {
                    ModelState.AddModelError("Name", $"{FieldsDisplayNames.Group} {ErrorMessages.UniqueValue}");
                    return View(model);
                }

                UserGroup group = new UserGroup();
                group.Name = model.Name;
                group.Description = model.Description;
                var result = await _userGroupService.CreateUserGroupAsync(group);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(model);
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = result?.Data?.Id });
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null) return NotFound();

            var groupAsView = await PropertyActionsHelper.CopyPropertiesAsync(group, new UserGroupViewModel());
            if (groupAsView == null) return NotFound();

            return View(groupAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UserGroupViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var group = await _userGroupService.GetByIDAsync(id);
                if (group != null)
                {
                    var existing = await _userGroupService.GetByGroupNameAsync(model.Name);
                    if (existing != null && existing.Id != group.Id)
                    {
                        ModelState.AddModelError("Name", $"{FieldsDisplayNames.Group} {ErrorMessages.UniqueValue}");
                        return View(model);
                    }

                    group.Name = model.Name;
                    group.Description = model.Description;

                    ServiceResult<UserGroup?> result = await _userGroupService.UpdateUserGroupAsync(group);
                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View();
                    }
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = model.Id });
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null) return NotFound();

            return View();
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group != null)
            {
                ServiceResult<UserGroup?> result = await _userGroupService.DeleteUserGroupAsync(id);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetLookupList(string fieldName)
        {
            if (fieldName == "Name")
            {
                var query = await _userGroupService.GetAllUserGroupsAsync();

                var result = query
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        description = u.Description
                    })
                    .OrderBy(u => u.name)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.UserGroup))
            {
                return await GetGridDataAsync<UserGroupGridViewModel, UserGroup>("UserGroups");
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(UserGroup))
                return (IEnumerable<TEntity>)await _userGroupService.GetAllUserGroupsAsync();

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is UserGroupGridViewModel)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "name", DisplayName = FieldsDisplayNames.Name, LinkTemplate = "/UserGroups/Edit/{id}" },
                    new GridField { FieldName = "description", DisplayName = FieldsDisplayNames.Description, LinkTemplate = "/UserGroups/Edit/{id}" },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }

            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is UserGroupGridViewModel userRow)
            {
            }
        }


        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

            var users = await _userGroupService.GetAllUserGroupsAsync();
            var data = users.Where(x => ids.Contains(x.Id)).ToList();

            return await ExportToExcel(data, "users.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
            var users = await _userGroupService.GetAllUserGroupsAsync();
            return await ExportToExcel(users, "users.xlsx");
        }
    }
}
