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
                if (model.MembersGuids.Any())
                {
                    foreach (var member in model.MembersGuids)
                    {
                        var user = await _userService.GetUserByIDAsync(member);
                        if (user != null)
                        {
                            group.Members.Add(new UserGroupMember { UserId = user.Id });
                        }
                    }
                }
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
            var groupMembers = await _userGroupService.GetAllUsersByGroupIDAsync(group.Id);
            foreach (var member in groupMembers)
            {
                groupAsView.MembersGuids.Add(member.UserId.ToString());
            }
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
                    group.Note = model.Note;

                    List<UserGroupMember> members = new List<UserGroupMember>();
                    if (model.MembersGuids.Any())
                    {
                        foreach (var member in model.MembersGuids)
                        {
                            var user = await _userService.GetUserByIDAsync(member);
                            if (user != null)
                            {
                                members.Add(new UserGroupMember { UserId = user.Id, GroupId = id });
                            }
                        }
                    }
                    ServiceResult<UserGroup?> result = await _userGroupService.UpdateUserGroupAsync(group, members);
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

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null) return NotFound();

            return View();
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
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
        public async Task<JsonResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();

            var result = users.Select(u => new { id = u.Id, name = u.FirstName })
                .ToList();

            return Json(users);
        }


        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.UserGroup))
            {
                return await GetGridDataAsync<UserGroupGridViewModel, UserGroup>("UserGroups");
            }
            else if (tableModel == nameof(Partnerly.Models.User))
            {
                List<UserGridViewModel> userList = new List<UserGridViewModel>();

                var members = await _userGroupService.GetAllUsersGroupsAsync();
                var users = await _userService.GetAllUsersAsync();

                //var result = from u in users
                //             join gm in members on u.Id equals gm.UserId into gj
                //             from subGroup in gj.DefaultIfEmpty()
                //             orderby subGroup != null ? subGroup.GroupId : int.MaxValue, // сортируем по GroupId, не входящие в группу — в конец
                //                     subGroup == null ? 1 : 0                            // чтобы пользователи без группы были после
                //             select new
                //             {
                //                 UserId = u.UserId,
                //                 u.Name,
                //                 GroupId = subGroup?.GroupId, // если null — не в группе
                //                 InGroup = subGroup != null
                //             };

                foreach (User? user in users)
                {
                    UserGridViewModel row = new UserGridViewModel
                    {
                        Id = user.Id,
                        UserName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email,
                        CreatedDate = user.CreatedDate,
                    };
                    userList.Add(row);
                }

                if (userList.Any())
                {
                    return Json(new { fields = GetFields(userList.First()), data = userList });
                }
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(UserGroup))
            {
                return (IEnumerable<TEntity>)await _userGroupService.GetAllUserGroupsAsync();
            }
            else if (typeof(TEntity) == typeof(Partnerly.Models.User))
            {

            }

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridAction> GetDefaultActions(object row)
        {
            if (row is UserGridViewModel)
            {
                return new List<GridAction>
                {
                };
            }
            else
            {
                return base.GetDefaultActions(row);
            }
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
            else if(row is UserGridViewModel)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "userName", DisplayName = FieldsDisplayNames.User, LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Description },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }

            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is UserGroupGridViewModel userGroupRow)
            {
            }
            if (row is UserGridViewModel userRow)
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
