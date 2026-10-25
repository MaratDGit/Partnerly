using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;

namespace Partnerly.Controllers
{
    [Route("AccessControl")]
    public sealed class AccessControlController : _BaseController
    {
        private readonly AppDbContext _context;
        private readonly IAccessControlService _accessControlService;

        public AccessControlController(
            IEventBus eventBus,
            IUserService userService,
            ICurrentUserService currentUser,
            IAttachmentService attachmentService,
            ILogService logService,
            IStringLocalizer<SharedResource> localizer,
            ISystemSettingsService systemSettingsService,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry,
            AppDbContext context,
            IAccessControlService accessControlService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _context = context;
            _accessControlService = accessControlService;
        }

        [HttpGet("Roles")]
        [Authorize(Policy = "Permission:roles.view")]
        public async Task<IActionResult> Roles()
        {
            var roles = await _context.Roles!
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.Name)
                .Select(r => new RoleListItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    IsActive = r.IsActive,
                    PermissionCount = r.RolePermissions.Count()
                })
                .ToListAsync();

            return View(roles);
        }

        [HttpGet("RolePermissions/{id:guid}")]
        [Authorize(Policy = "Permission:roles.manage_permissions")]
        public async Task<IActionResult> RolePermissions(Guid id)
        {
            var role = await _context.Roles!
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    !r.IsDeleted);

            if (role == null)
                return NotFound();

            var permissions =
                await _accessControlService.GetPermissionsAsync();

            var assigned =
                await _accessControlService.GetRolePermissionsAsync(id);

            var assignedIds = assigned
                .Select(p => p.Id)
                .ToHashSet();

            var model = new RolePermissionsViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                Permissions = permissions.Select(p =>
                    new PermissionSelectionViewModel
                    {
                        PermissionId = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Module = p.Module,
                        Selected = assignedIds.Contains(p.Id)
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost("RolePermissions/{id:guid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:roles.manage_permissions")]
        public async Task<IActionResult> SaveRolePermissions(
            Guid id,
            RolePermissionsViewModel model)
        {
            if (id != model.RoleId)
                return BadRequest();

            var success =
                await _accessControlService.SetRolePermissionsAsync(
                    id,
                    model.Permissions
                        .Where(p => p.Selected)
                        .Select(p => p.PermissionId));

            if (!success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The role or one of the selected permissions is invalid.");

                var role = await _context.Roles!
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Id == id &&
                        !r.IsDeleted);

                if (role == null)
                    return NotFound();

                model.RoleName = role.Name ?? string.Empty;

                var permissions =
                    await _accessControlService.GetPermissionsAsync();

                var selectedIds = model.Permissions
                    .Where(p => p.Selected)
                    .Select(p => p.PermissionId)
                    .ToHashSet();

                model.Permissions = permissions.Select(p =>
                    new PermissionSelectionViewModel
                    {
                        PermissionId = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Module = p.Module,
                        Selected = selectedIds.Contains(p.Id)
                    }).ToList();

                return View("RolePermissions", model);
            }

            TempData["Success"] = "Role permissions updated.";

            return RedirectToAction(nameof(RolePermissions), new { id });
        }

        [HttpGet("UserPermissions/{id:guid}")]
        //[Authorize(Policy = "Permission:users.manage_roles")]
        public async Task<IActionResult> UserPermissions(Guid id)
        {
            var user = await _context.Users!
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    !u.IsDeleted);

            if (user == null)
                return NotFound();

            var permissions =
                await _accessControlService.GetPermissionsAsync();

            var existing =
                await _accessControlService.GetUserPermissionsAsync(id);

            var rules = existing.ToDictionary(
                p => p.PermissionId,
                p => p.IsGranted);

            var model = new UserPermissionsViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Permissions = permissions.Select(p =>
                    new UserPermissionSelectionViewModel
                    {
                        PermissionId = p.Id,
                        Code = p.Code,
                        Module = p.Module,
                        HasOverride = rules.ContainsKey(p.Id),
                        IsGranted = rules.TryGetValue(p.Id, out var granted)
                            && granted
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost("UserPermissions/{id:guid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:users.manage_roles")]
        public async Task<IActionResult> SaveUserPermissions(
            Guid id,
            UserPermissionsViewModel model)
        {
            if (id != model.UserId)
                return BadRequest();

            var rules = model.Permissions
                .Where(p => p.HasOverride)
                .Select(p => new UserPermissionInput
                {
                    PermissionId = p.PermissionId,
                    IsGranted = p.IsGranted
                });

            var success =
                await _accessControlService.SetUserPermissionsAsync(
                    id, rules);

            if (!success)
            {
                TempData["Error"] =
                    "Could not update permissions. Check the user and permissions.";

                return RedirectToAction(nameof(UserPermissions), new { id });
            }

            TempData["Success"] = "User permissions updated.";

            return RedirectToAction(nameof(UserPermissions), new { id });
        }
    }

    public sealed class RoleListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int PermissionCount { get; set; }
    }

    public sealed class RolePermissionsViewModel
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        public List<PermissionSelectionViewModel> Permissions { get; set; }
            = new();
    }

    public sealed class PermissionSelectionViewModel
    {
        public Guid PermissionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public sealed class UserPermissionsViewModel
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;

        public List<UserPermissionSelectionViewModel> Permissions { get; set; }
            = new();
    }

    public sealed class UserPermissionSelectionViewModel
    {
        public Guid PermissionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;

        // false means inherit from role; true means use IsGranted.
        public bool HasOverride { get; set; }
        public bool IsGranted { get; set; }
    }
}
