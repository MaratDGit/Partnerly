using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UserGroupsController : _BaseController
    {
        private readonly IUserGroupService _userGroupService;
        private readonly IInlineUpdateService _inlineUpdateService;

        public UserGroupsController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            IUserGroupService userGroupService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService,
            IInlineUpdateService inlineUpdateService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _userGroupService = userGroupService;
            _inlineUpdateService = inlineUpdateService;
        }

        #region Index / Create / Edit / Delete
        public IActionResult Index()
        {
            UserGroupViewModel model = new UserGroupViewModel();
            model.groups = UserGroupViewModel.CreateTableOptions(DataTableNames.userGroupsTable, _dataTableRegistry);

            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public IActionResult Create()
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
                //if (model.MembersGuids.Any())
                //{
                //    foreach (var member in model.MembersGuids)
                //    {
                //        var user = await _userService.GetUserByIDAsync(member);
                //        if (user != null)
                //        {
                //            group.Members.Add(new UserGroupMember { UserId = user.Id });
                //        }
                //    }
                //}
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
                groupAsView.Members.Add(member);
            }

            if (groupAsView == null) return NotFound();

            groupAsView.groupMembers = UserGroupViewModel.CreateTableOptions(DataTableNames.userGroupsMembersTable, _dataTableRegistry);

            await PrepareViewModelAsync(groupAsView);

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

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null) return NotFound();

            var model = new UserGroupViewModel
            {
                Id = group.Id
            };
            return View(model);
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
        #endregion

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
        public async Task<IActionResult> LookupUsers(Guid groupId, string? search = null)
        {
            var users = await _userService.GetAllUsersAsync();

            var members = (await _userGroupService
                .QueryMembersEntity(groupId)
                .Select(x => x.UserId)
                .ToListAsync())
                .ToHashSet();

            var query = users.Where(u => !members.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(u =>
                    (u.FirstName ?? "").Contains(
                        term, StringComparison.OrdinalIgnoreCase) ||
                    (u.LastName ?? "").Contains(
                        term, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email ?? "").Contains(
                        term, StringComparison.OrdinalIgnoreCase));
            }

            var result = query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                //.Take(20)
                .Select(u => new
                {
                    id = u.Id,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    name = $"{u.FirstName} {u.LastName}".Trim(),
                    email = u.Email
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID is required."
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);

            if (options == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }

            if (request.TableID == DataTableNames.userGroupsMembersTable)
            {
                if (!request.ParentKeys.TryGetValue("GroupID", out var groupIdValue) || !Guid.TryParse(groupIdValue, out var groupId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid GroupID is required."
                    });
                }

                var result = await _tableService.ExecuteAsync(
                    fields => _userGroupService.QueryMembersEntity(groupId),
                    request,
                    options);

                return Json(result);
            }
            else if (request.TableID == DataTableNames.userGroupsTable)
            {
                var result = await _tableService.ExecuteAsync(
                fields => _userGroupService.QueryEntity(fields),
                request,
                options);

                return Json(result);
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInline(
        [FromBody] DataTableInlineUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Group ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var groupId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Group ID."
                });
            }

            var group = await _userGroupService.GetByIDAsync(groupId);

            if (group == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User group not found."
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);
            var result = _inlineUpdateService.ApplyChanges(
                group,
                request.Fields,
                options);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            if (result.IsChanged)
            {
                await _userGroupService.UpdateUserGroupAsync(group);
            }

            return Ok(new
            {
                success = true,
                changed = result.IsChanged
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline([FromBody] DataTableInlineCreateRequest request)
        {
            if (request.TableID == DataTableNames.userGroupsMembersTable)
            {
                if (!request.ParentKeys.
                    TryGetValue("GroupID", out var groupIdValue) || !Guid.TryParse(groupIdValue, out var groupId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid GroupID is required."
                    });
                }

                if (!request.Fields.TryGetValue(
                    nameof(UserGroupMember.UserId), out var userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid UserId is required."
                    });
                }

                var memberResult = await _userGroupService.AddMemberAsync(groupId, userId);

                if (!memberResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", memberResult.Errors)
                    });
                }

                return Json(new
                {
                    success = true,
                    id = userId
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);

            if (options == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid table."
                });
            }

            var result = _inlineUpdateService.Create<UserGroup>(request.Fields, options);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            var createResult = await _userGroupService.CreateUserGroupAsync((UserGroup)result.Entity!);

            if (!createResult.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.Join(
                        " ",
                        createResult.Errors)
                });
            }

            return Json(new
            {
                success = true,
                id = createResult.Data!.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInline([FromBody] DataTableInlineDeleteRequest request)
        {
            if (request.TableID == DataTableNames.userGroupsMembersTable)
            {
                if (!request.Keys.TryGetValue(
                        nameof(UserGroupMember.GroupId), out var groupIdValue) || !Guid.TryParse(groupIdValue, out var memberGroupId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid GroupID is required."
                    });
                }

                if (!request.Keys.TryGetValue
                    (nameof(UserGroupMember.UserId), out var userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid UserId is required."
                    });
                }

                var memberResult = await _userGroupService.DeleteMemberAsync(memberGroupId, userId);

                if (!memberResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", memberResult.Errors)
                    });
                }

                return Json(new
                {
                    success = true
                });
            }

            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Group ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var groupId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Group ID."
                });
            }

            var group = await _userGroupService.GetByIDAsync(groupId);

            if (group == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Group not found."
                });
            }

            await _userGroupService.DeleteUserGroupAsync(groupId);

            return Json(new
            {
                success = true
            });
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });

            if (request.TableID == DataTableNames.userGroupsTable)
            {
                return await ExportTableAsync(
                request,
                format,
                fields => _userGroupService.QueryEntity(fields),
                "groups");
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }
        }
    }
}
