using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Messages;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Services
{
    public static class PermissionSeeder
    {
        private static readonly string[] EmployeePermissions =
        {
            PermissionCodes.UsersView,
            PermissionCodes.UserGroupsView,
            PermissionCodes.NotificationsView,
            PermissionCodes.SupportTicketsView,
            PermissionCodes.SupportTicketsCreate,
            PermissionCodes.SupportTicketsEdit,
            PermissionCodes.AttachmentsView,
            PermissionCodes.WebInspectorUse
        };

        private static readonly string[] UserRolePermissions =
        {
            PermissionCodes.SupportTicketsView,
            PermissionCodes.SupportTicketsCreate
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            var permissionDefinitions = PermissionCodes.All
                .Select(code => new
                {
                    Code = code,
                    Module = code.Split('.')[0]
                })
                .ToList();

            var existingCodes = await context.Permissions!
                .Select(p => p.Code)
                .ToListAsync();
            bool allowUpdate = false;
            foreach (var definition in permissionDefinitions)
            {
                if (existingCodes.Contains(definition.Code))
                    continue;

                allowUpdate = true;
                context.Permissions!.Add(new Permission
                {
                    Code = definition.Code,
                    Name = definition.Code,
                    Module = definition.Module,
                    Description = $"Permission: {definition.Code}",
                    IsActive = true,
                    IsDeleted = false
                });
            }

            if (allowUpdate)
                await context.SaveChangesAsync();

            var roles = await context.Roles!
                .Where(r => !r.IsDeleted)
                .ToListAsync();

            var permissions = await context.Permissions!
                .Where(p => p.IsActive && !p.IsDeleted)
                .ToListAsync();

            allowUpdate = false;
            foreach (var role in roles)
            {
                string[] roleCodes;

                if (role.Name == Constants.SuperRoleName ||
                    role.Name == Constants.Admin)
                {
                    roleCodes = PermissionCodes.All;
                }
                else if (role.Name == Constants.Employee)
                {
                    roleCodes = EmployeePermissions;
                }
                else if (role.Name == Constants.User)
                {
                    roleCodes = UserRolePermissions;
                }
                else
                {
                    continue;
                }

                var allowedPermissionIds = permissions
                    .Where(p => roleCodes.Contains(p.Code))
                    .Select(p => p.Id)
                    .ToHashSet();

                var existingAssignments = await context.RolePermissions!
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var existingSet = existingAssignments.ToHashSet();

                foreach (var permissionId in allowedPermissionIds)
                {
                    if (existingSet.Contains(permissionId))
                        continue;

                    allowUpdate = true;
                    context.RolePermissions!.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId
                    });
                }
            }

            if (allowUpdate)
                await context.SaveChangesAsync();
        }
    }
}
