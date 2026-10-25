using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Partnerly.Descriptors.Attributes;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Services
{
    public static class PermissionSeeder
    {
        private static readonly string[] AllPermissions =
        {
            "users.view",
            "users.create",
            "users.edit",
            "users.delete",
            "users.manage_roles",

            "roles.view",
            "roles.create",
            "roles.edit",
            "roles.delete",
            "roles.manage_permissions",

            "permissions.view",
            "permissions.manage",

            "user_groups.view",
            "user_groups.create",
            "user_groups.edit",
            "user_groups.delete",

            "notifications.view",

            "support_tickets.view",
            "support_tickets.create",
            "support_tickets.edit",
            "support_tickets.delete",

            "system_settings.view",
            "system_settings.edit",


        };

        private static readonly string[] EmployeePermissions =
        {
            "user_groups.view",
            "user_groups.create",
            "user_groups.edit",

            "notifications.view",

            "support_tickets.view",
            "support_tickets.create",
            "support_tickets.edit"
        };

        private static readonly string[] UserPermissions =
        {
            "notifications.view",
            "support_tickets.view",
            "support_tickets.create"
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            var permissionDefinitions = AllPermissions
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

                if (role.Name == Descriptors.Messages.Constants.SuperRoleName)
                {
                    roleCodes = AllPermissions;
                }
                if (role.Name == Descriptors.Messages.Constants.Admin)
                {
                    roleCodes = AllPermissions;
                }
                else if (role.Name == Descriptors.Messages.Constants.Employee)
                {
                    roleCodes = EmployeePermissions;
                }
                else if (role.Name == Descriptors.Messages.Constants.User)
                {
                    roleCodes = UserPermissions;
                }
                else
                {
                    // Custom roles receive no automatic privileges.
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
