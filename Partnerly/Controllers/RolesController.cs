using Microsoft.AspNetCore.Authorization;
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
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;
using System.Diagnostics;
using System.Linq;

namespace Partnerly.Controllers
{
    public class RolesController : _BaseController
    {
        protected readonly IRoleService _roleService;
        private readonly IInlineUpdateService _inlineUpdateService;
        private readonly IPermissionService _permissionService;
        public RolesController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser,
            ILogService logService, 
            IRoleService roleService, 
            IStringLocalizer<SharedResource> localizer, 
            ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry,
            IInlineUpdateService inlineUpdateService,
            IPermissionService permissionService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _roleService = roleService;
            _inlineUpdateService = inlineUpdateService;
            _permissionService = permissionService;
        }

        [Authorize(Policy = "Permission:roles.view")]
        public IActionResult Index()
        {
            RolesViewModel model = new RolesViewModel();
            model.roles = RolesViewModel.CreateTableOptions(DataTableNames.rolesTable, _dataTableRegistry);

            return View("~/Views/Shared/DataTable/_DataTablePartial.cshtml", model.roles);
        }

        [Authorize(Policy = "Permission:roles.create")]
        public IActionResult Create()
        {
            var model = new RolesViewModel { Title = FieldsDisplayNames.RoleCreated };
            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:roles.create")]
        public async Task<IActionResult> Create(RolesViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _roleService.GetRoleByNameAsync(model.Name);
                if (existing != null)
                {
                    ModelState.AddModelError("Name", $"{FieldsDisplayNames.Role} {ErrorMessages.UniqueValue}");
                    return View("Form", model);
                }

                Role group = new Role();
                group.Name = model.Name;
                group.IsActive = true;

                var result = await _roleService.CreateRoleAsync(group);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View("Form", model);
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = result?.Data?.Id });
            }
            return View("Form", model);
        }

        [Authorize(Policy = "Permission:roles.edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var role = await _roleService.GetRoleByIDAsync(id);
            if (role == null) return NotFound();

            if (base.IsSuperRole(role) && !await base.IsSuperUser()) return Forbid();

            var roleAsView = await PropertyActionsHelper.CopyPropertiesAsync(role, new RolesViewModel());
            if (roleAsView == null) return NotFound();

            roleAsView.rolePermissions = RolesViewModel.CreateTableOptions(DataTableNames.rolePermissionsTable, _dataTableRegistry);

            await PrepareViewModelAsync(roleAsView);

            roleAsView.Title = FieldsDisplayNames.RoleUpdated;
            return View("Form", roleAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:roles.edit")]
        public async Task<IActionResult> Edit(Guid id, RolesViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var role = await _roleService.GetRoleByIDAsync(id);
                if (role != null)
                {
                    if (base.IsSuperRole(role) && !await base.IsSuperUser()) return Forbid();

                    var existing = await _roleService.GetRoleByNameAsync(model.Name);
                    if (existing != null && existing.Id != role.Id)
                    {
                        ModelState.AddModelError("Name", $"{FieldsDisplayNames.Role} {ErrorMessages.UniqueValue}");
                        return View("Form", model);
                    }

                    if (base.IsSystemRole(role.Name))
                    {
                        TempData["ToastType"] = "error";
                        TempData["ToastMessage"] = ErrorMessages.CannotDeleteSystemRecords;

                        return View("Form", model);
                    }

                    role.Name = model.Name;
                    role.Note = model.Note;

                    ServiceResult<Role?> result = await _roleService.UpdateRoleAsync(role);
                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View("Form", model);
                    }
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = model.Id });
            }

            return View("Form", model);
        }

        [Authorize(Policy = "Permission:roles.delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var role = await _roleService.GetRoleByIDAsync(id);
            if (role == null) 
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorCode = ErrorMessages.NotFound,
                });

            if (base.IsSuperRole(role)) return Forbid();

            if (base.IsSystemRole(role.Name))
            {
                var model = new { id = id };
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = ErrorMessages.CannotDeleteSystemRecords;

                return RedirectToAction("Edit", model);
            }

            var deleteModel = new DeleteConfirmationViewModel
            {
                Id = role.Id,
                Title = ErrorMessages.DeleteRole,
                Message = ErrorMessages.ConfirmDelete,
                DeleteAction = "DeleteConfirmed",
                DeleteController = "Roles",
                CancelAction = "Index",
                CancelController = "Roles"
            };

            return View("~/Views/Shared/_DeleteConfirmation.cshtml", deleteModel);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:roles.delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var role = await _roleService.GetRoleByIDAsync(id);
            if (role != null)
            {
                if (base.IsSuperRole(role) || base.IsSystemRole(role.Name)) return Forbid();

                ServiceResult<Role?> result = await _roleService.DeleteRoleAsync(id);
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
        [Authorize(Policy = "Permission:roles.view")]
        public async Task<IActionResult> GetLookupList(string fieldName)
        {
            var roles = await _roleService.GetAllRolesAsync();
            bool isSuperUser = await base.IsSuperUser();

            if (fieldName == "Name")
            {
                List<UserRolesViewModel> userRolesList = new List<UserRolesViewModel>();
                
                var users = await _userService.GetAllUsersAsync();

                foreach (User? user in users)
                {
                    if (await base.IsSuperUser(user) && !isSuperUser)
                        continue;

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

            if (fieldName == "RoleName")
            {

                if (!isSuperUser)
                {
                    roles = roles.Where(_ => !base.IsSuperRole(_));
                }

                var result = roles.Where(_ => _.IsActive == true && _.IsDeleted != true)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        isActive = u.IsActive,
                    })
                    .OrderBy(u => u.name)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }

        [HttpGet]
        public async Task<IActionResult> LookupPermissions(string? search = null)
        {
            var userIdTextEarly = Request.Query["userId"].ToString();
            if (Guid.TryParse(userIdTextEarly, out var lookupUserId))
            {
                if (!await CurrentUserHasAsync("users.manage_roles") || !await CanViewUserAsync(lookupUserId))
                    return Forbid();
            }
            else if (!await CurrentUserHasAsync("roles.manage_permissions"))
            {
                return Forbid();
            }

            var permissions = await _permissionService.GetAllPermissionAsync();
            var roleIdText = Request.Query["roleId"].ToString();
            var userIdText = Request.Query["userId"].ToString();

            HashSet<Guid> assignedIds;

            if (Guid.TryParse(roleIdText, out var roleId))
            {
                assignedIds = (await _permissionService
                    .QueryRolePermissionsExt(roleId)
                    .Select(x => x.PermissionId)
                    .ToListAsync())
                    .ToHashSet();
            }
            else if (Guid.TryParse(userIdText, out var userId))
            {
                assignedIds = (await _permissionService
                    .QueryUserPermissionsExt(userId)
                    .Select(x => x.PermissionId)
                    .ToListAsync())
                    .ToHashSet();
            }
            else
            {
                return Json(new { });
            }

            var query = permissions.Where(u =>
                u != null &&
                u.IsActive == true &&
                u.IsDeleted != true &&
                !assignedIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    (u!.Name != null && u.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Module != null && u.Module.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Code != null && u.Code.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var result = query
                .OrderBy(u => u!.Name)
                .Select(u => new
                {
                    id = u!.Id,
                    permissionId = u.Id,
                    name = u.Name,
                    module = u.Module,
                    code = u.Code,
                    isActive = u.IsActive,
                    isDeleted = u.IsDeleted
                })
                .ToList();

            return Json(result);
        }

        [Authorize(Policy = "Permission:users.manage_roles")]
        public async Task<IActionResult> UserRoleEdit(Guid id)
        {
            var user = await _userService.GetUserByIDAsync(id);
            if (user == null) return NotFound();
            if (!await CanViewUserAsync(user.Id)) return Forbid();

            //if (_currentUser.RoleId == user.RoleId) return Forbid();

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

            AttachUserPermissionsTable(modelAsView);
            modelAsView.Title = FieldsDisplayNames.UserRole;
            return View(modelAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:users.manage_roles")]
        public async Task<IActionResult> UserRoleEdit(Guid id, UserRolesViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                User? user = await _userService.GetUserByIDAsync(model.Id);

                if (user == null)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = "user not found";

                    return RedirectToAction("UserRoleEdit", model);
                }

                if (!await CanViewUserAsync(user.Id)) return Forbid();
                //if (_currentUser.RoleId == user.RoleId) return NotFound();

                Role? role = await _roleService.GetRoleByNameAsync(model.NewRoleName);

                if (role == null)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = "role not found";

                    return RedirectToAction("UserRoleEdit", model);
                }

                user.RoleId = role.Id;
                user.LastRoleUpdateTime = DateTime.UtcNow;
                user.Note = model.Note;
                ServiceResult<User?> result = await _userService.UpdateUserAsync(user);

                if (!result.Success)
                {
                    TempData["ToastType"] = "error";
                    foreach (var error in result.Errors)
                    {
                        TempData["ToastMessage"] += " " + error;
                    }
                    
                    return RedirectToAction("UserRoleEdit", new { id = model.Id });
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("UserRoleEdit", new { id = model.Id });
            }

            AttachUserPermissionsTable(model);
            return View(model);
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

            if (request.TableID == DataTableNames.rolesTable)
            {
                if (!await CurrentUserHasAsync("roles.view"))
                    return Forbid();

                var result =
                    !await base.IsSuperUser() 
                    ?
                    await _tableService.ExecuteAsync(
                    fields => _roleService.QueryEntity(fields).Where(r => r.Name != Constants.SuperRoleName),
                    request,
                    options) 
                    :
                    await _tableService.ExecuteAsync(
                    fields => _roleService.QueryEntity(fields),
                    request,
                    options);

                return Json(result);
            }
            else if (request.TableID == DataTableNames.rolePermissionsTable)
            {
                if (!await CurrentUserHasAsync("roles.manage_permissions"))
                    return Forbid();

                if (!request.ParentKeys.TryGetValue("RoleId", out var groupIdValue) || !Guid.TryParse(groupIdValue, out var groupId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid Role Id is required."
                    });
                }

                var result = await _tableService.ExecuteAsync(
                    fields => _permissionService.QueryRolePermissionsExt(groupId),
                    request,
                    options);

                return Json(result);
            }
            else if (request.TableID == DataTableNames.userPermissionsTable)
            {
                if (!await CurrentUserHasAsync("users.manage_roles"))
                    return Forbid();

                if (!request.ParentKeys.TryGetValue("UserId", out var userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid User Id is required."
                    });
                }

                if (!await CanViewUserAsync(userId))
                    return Forbid();

                var result = await _tableService.ExecuteAsync(
                    fields => _permissionService.QueryUserPermissionsExt(userId),
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
            if (request.TableID == DataTableNames.userPermissionsTable)
            {
                if (!await CurrentUserHasAsync("users.manage_roles"))
                    return Forbid();

                if (string.IsNullOrWhiteSpace(request.Id) || !Guid.TryParse(request.Id, out var userPermissionId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid User Permission Id is required."
                    });
                }

                if (!request.Fields.TryGetValue(nameof(UserPermission.IsGranted), out var grantedValue) ||
                    !TryParseBool(grantedValue, out var isGranted))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid IsGranted value is required."
                    });
                }

                var existingGrant = await _userService.GetUserPermissionByIdAsync(userPermissionId);
                if (existingGrant == null)
                    return NotFound();
                if (!await CanViewUserAsync(existingGrant.UserId))
                    return Forbid();

                var grantResult = await _userService.UpdatePermissionGrantAsync(userPermissionId, isGranted);
                if (!grantResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", grantResult.Errors)
                    });
                }

                return Ok(new
                {
                    success = true,
                    changed = true
                });
            }

            if (!await CurrentUserHasAsync("roles.edit"))
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Role ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var roleId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Role ID."
                });
            }

            var role = await _roleService.GetRoleByIDAsync(roleId);

            if (role == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Role not found."
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);
            var result = _inlineUpdateService.ApplyChanges(
                role,
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

            if (!string.IsNullOrWhiteSpace(role.Name))
            {
                var existing = await _roleService.GetAllRolesAsync();
                if (existing != null && 
                    existing.Count() > 0 && 
                    existing.FirstOrDefault(_ => _.Id != role.Id && _.Name.Trim().ToLower() == role.Name.Trim().ToLower()) != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"{FieldsDisplayNames.Role} - '{role.Name.Trim()}' - {ErrorMessages.UniqueValue}",
                    });
                }
            }

            if (result.IsChanged)
            {
                await _roleService.UpdateRoleAsync(role);
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
            if (request.TableID == DataTableNames.userPermissionsTable)
            {
                if (!await CurrentUserHasAsync("users.manage_roles"))
                    return Forbid();

                if (!request.ParentKeys.TryGetValue("UserId", out var userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid UserId is required."
                    });
                }

                if (!request.Fields.TryGetValue(
                    nameof(UserPermission.PermissionId), out var userPermissionIdValue) || !Guid.TryParse(userPermissionIdValue, out var userPermissionId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid PermissionId is required."
                    });
                }

                var isGranted = true;
                if (request.Fields.TryGetValue(nameof(UserPermission.IsGranted), out var grantedValue) &&
                    TryParseBool(grantedValue, out var parsedGranted))
                {
                    isGranted = parsedGranted;
                }

                if (!await CanViewUserAsync(userId))
                    return Forbid();

                var userPermissionResult = await _userService.AddPermissionAsync(userId, userPermissionId, isGranted);
                if (!userPermissionResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", userPermissionResult.Errors)
                    });
                }

                return Json(new
                {
                    success = true,
                    id = userPermissionResult.Data!.Id
                });
            }

            if (request.TableID == DataTableNames.rolePermissionsTable)
            {
                if (!await CurrentUserHasAsync("roles.manage_permissions"))
                    return Forbid();

                if (!request.ParentKeys.
                    TryGetValue("RoleId", out var roleIdValue) || !Guid.TryParse(roleIdValue, out var roleId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid RoleId is required."
                    });
                }

                if (!request.Fields.TryGetValue(
                    nameof(RolePermission.PermissionId), out var permissionIdValue) || !Guid.TryParse(permissionIdValue, out var permissionId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid PermissionId is required."
                    });
                }

                var resultPermission = await _roleService.AddPermissionAsync(roleId, permissionId);

                if (!resultPermission.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", resultPermission.Errors)
                    });
                }

                return Json(new
                {
                    success = true,
                    id = permissionId
                });
            }

            if (!await CurrentUserHasAsync("roles.create"))
                return Forbid();

            var options = _dataTableRegistry.Get(request.TableID);

            if (options == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid table."
                });
            }

            var result = _inlineUpdateService.Create<Role>(request.Fields, options);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            var role = (Role?)result.Entity;

            if (role == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Role not found."
                });
            }

            if (!string.IsNullOrWhiteSpace(role.Name))
            {
                var existing = await _roleService.GetRoleByNameAsync(role.Name);
                if (existing != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"{FieldsDisplayNames.Role} - '{role.Name.Trim()}' - {ErrorMessages.UniqueValue}",
                    });
                }
            }

            var createResult = await _roleService.CreateRoleAsync(role);

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
            if (request.TableID == DataTableNames.userPermissionsTable)
            {
                if (!await CurrentUserHasAsync("users.manage_roles"))
                    return Forbid();

                if (!request.Keys.TryGetValue(
                        nameof(UserPermission.UserId), out var userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid UserId is required."
                    });
                }

                if (!await CanViewUserAsync(userId))
                    return Forbid();

                if (!request.Keys.TryGetValue(
                        nameof(UserPermission.PermissionId), out var userPermissionIdValue) || !Guid.TryParse(userPermissionIdValue, out var userPermissionId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid PermissionId is required."
                    });
                }

                var userResult = await _userService.DeletePermissionAsync(userId, userPermissionId);
                if (!userResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", userResult.Errors)
                    });
                }

                return Json(new
                {
                    success = true
                });
            }

            if (request.TableID == DataTableNames.rolePermissionsTable)
            {
                if (!await CurrentUserHasAsync("roles.manage_permissions"))
                    return Forbid();

                if (!request.Keys.TryGetValue(
                        nameof(RolePermission.RoleId), out var roleIdValue) || !Guid.TryParse(roleIdValue, out var roleId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid RoleId is required."
                    });
                }

                if (!request.Keys.TryGetValue
                    (nameof(RolePermission.PermissionId), out var permissionIdValue) || !Guid.TryParse(permissionIdValue, out var permissionId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A valid PermissionId is required."
                    });
                }

                var result = await _roleService.DeletePermissionAsync(roleId, permissionId);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = string.Join(" ", result.Errors)
                    });
                }

                return Json(new
                {
                    success = true
                });
            }

            return Json(new
            {
                success = false
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

            if (request.TableID == DataTableNames.rolesTable)
            {
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _roleService.QueryEntity(fields),
                            "roles");
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

        private void AttachUserPermissionsTable(UserRolesViewModel model)
        {
            model.userPermissions = UserRolesViewModel.CreateTableOptions(
                DataTableNames.userPermissionsTable,
                _dataTableRegistry);
        }

        private Task<bool> CurrentUserHasAsync(string permissionCode) =>
            _permissionService.HasPermissionAsync(_currentUser.UserId, permissionCode);

        private static bool TryParseBool(string? value, out bool result)
        {
            if (bool.TryParse(value, out result))
                return true;

            if (value == "1")
            {
                result = true;
                return true;
            }

            if (value == "0")
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }
    }
}
