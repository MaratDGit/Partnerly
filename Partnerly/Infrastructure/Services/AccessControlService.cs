using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Services
{
    public sealed class AccessControlService : IAccessControlService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AccessControlService(
            AppDbContext context, 
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<Permission>> GetPermissionsAsync()
        {
            return await _context.Permissions!
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Permission>> GetRolePermissionsAsync(
            Guid roleId)
        {
            return await _context.RolePermissions!
                .AsNoTracking()
                .Where(rp =>
                    rp.RoleId == roleId &&
                    !rp.Permission!.IsDeleted)
                .Select(rp => rp.Permission!)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();
        }

        public async Task<bool> SetRolePermissionsAsync(
            Guid roleId,
            IEnumerable<Guid> permissionIds)
        {
            var role = await _context.Roles!
                .FirstOrDefaultAsync(r =>
                    r.Id == roleId &&
                    !r.IsDeleted);

            if (role == null || !role.IsActive)
                return false;

            var requestedIds = permissionIds
                .Distinct()
                .ToHashSet();

            var validIds = await _context.Permissions!
                .Where(p =>
                    requestedIds.Contains(p.Id) &&
                    p.IsActive &&
                    !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

            // Reject invalid or inactive permission IDs.
            if (validIds.Count != requestedIds.Count)
                return false;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existing = await _context.RolePermissions!
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.RolePermissions!.RemoveRange(existing);

            foreach (var permissionId in validIds)
            {
                _context.RolePermissions!.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
            }

            _context.AccessControlAuditLogs.Add(
                new AccessControlAuditLog
                {
                    PerformedByUserId = _currentUserService.UserId,
                    TargetRoleId = roleId,
                    Action = "RolePermissionsUpdated",
                    Details = $"Assigned permission count: {validIds.Count}",
                    CreatedDateUtc = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        public async Task<IReadOnlyList<UserPermission>>
            GetUserPermissionsAsync(Guid userId)
        {
            return await _context.UserPermissions!
                .AsNoTracking()
                .Include(up => up.Permission)
                .Where(up =>
                    up.UserId == userId &&
                    !up.Permission!.IsDeleted)
                .OrderBy(up => up.Permission!.Module)
                .ThenBy(up => up.Permission!.Code)
                .ToListAsync();
        }

        public async Task<bool> SetUserPermissionsAsync(
            Guid userId,
            IEnumerable<UserPermissionInput> permissions)
        {
            var user = await _context.Users!
                .FirstOrDefaultAsync(u =>
                    u.Id == userId &&
                    !u.IsDeleted);

            if (user == null)
                return false;

            var input = permissions.ToList();

            if (input.Select(p => p.PermissionId).Distinct().Count()
                != input.Count)
            {
                return false;
            }

            var requestedIds = input
                .Select(p => p.PermissionId)
                .ToHashSet();

            var validIds = await _context.Permissions!
                .Where(p =>
                    requestedIds.Contains(p.Id) &&
                    p.IsActive &&
                    !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

            if (validIds.Count != requestedIds.Count)
                return false;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existing = await _context.UserPermissions!
                .Where(up => up.UserId == userId)
                .ToListAsync();

            _context.UserPermissions!.RemoveRange(existing);

            foreach (var item in input)
            {
                _context.UserPermissions!.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = item.PermissionId,
                    IsGranted = item.IsGranted
                });
            }

            _context.AccessControlAuditLogs.Add(
                new AccessControlAuditLog
                {
                    PerformedByUserId = _currentUserService.UserId,
                    TargetUserId = userId,
                    Action = "UserPermissionsUpdated",
                    Details = $"Individual permission rules updated: {input.Count}",
                    CreatedDateUtc = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
    }
}
