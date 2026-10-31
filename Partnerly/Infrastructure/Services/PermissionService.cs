using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public PermissionService(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<Permission?>> GetAllPermissionAsync() =>
            await _context.Permissions
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();

        public async Task<Permission?> GetActivePermissionAsync(Guid permissionId) =>
            await _context.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == permissionId &&
                    p.IsActive &&
                    !p.IsDeleted);

        public void AddAccessAudit(
            string action,
            Guid? targetRoleId = null,
            Guid? targetUserId = null,
            string? permissionCode = null,
            string? details = null)
        {
            _context.AccessControlAuditLogs.Add(new AccessControlAuditLog
            {
                PerformedByUserId = _currentUser.UserId,
                TargetRoleId = targetRoleId,
                TargetUserId = targetUserId,
                Action = action,
                PermissionCode = permissionCode,
                Details = details,
                CreatedDateUtc = DateTime.UtcNow
            });
        }

        public async Task<bool> HasPermissionAsync(
        Guid? userId,
        string permissionCode)
        {
            if (userId == null ||
                string.IsNullOrWhiteSpace(permissionCode))
            {
                return false;
            }

            var user = await _context.Users!
                .AsNoTracking()
                .Where(u =>
                    u.Id == userId.Value &&
                    !u.IsDeleted &&
                    u.IsBlocked != true)
                .Select(u => new
                {
                    u.Id,
                    u.RoleId,
                    RoleIsActive = u.Role != null && u.Role.IsActive,
                    RoleIsDeleted = u.Role == null || u.Role.IsDeleted
                })
                .FirstOrDefaultAsync();

            if (user == null ||
                user.RoleId == null ||
                !user.RoleIsActive ||
                user.RoleIsDeleted)
            {
                return false;
            }

            var permission = await _context.Permissions!
                .AsNoTracking()
                .Where(p =>
                    p.Code == permissionCode &&
                    p.IsActive &&
                    !p.IsDeleted)
                .Select(p => new
                {
                    p.Id
                })
                .FirstOrDefaultAsync();

            if (permission == null)
                return false;

            // Individual rule takes precedence over the role.
            var userRule = await _context.UserPermissions!
                .AsNoTracking()
                .Where(up =>
                    up.UserId == user.Id &&
                    up.PermissionId == permission.Id)
                .Select(up => (bool?)up.IsGranted)
                .FirstOrDefaultAsync();

            if (userRule.HasValue)
                return userRule.Value;

            // Otherwise, check the user's role.
            return await _context.RolePermissions!
                .AsNoTracking()
                .AnyAsync(rp =>
                    rp.RoleId == user.RoleId.Value &&
                    rp.PermissionId == permission.Id);
        }

        public async Task<HashSet<string>> GetEffectivePermissionCodesAsync(Guid? userId)
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (userId == null)
                return codes;

            var user = await _context.Users!
                .AsNoTracking()
                .Where(u =>
                    u.Id == userId.Value &&
                    !u.IsDeleted &&
                    u.IsBlocked != true)
                .Select(u => new { u.RoleId })
                .FirstOrDefaultAsync();

            if (user?.RoleId == null)
                return codes;

            var roleCodes = await _context.RolePermissions!
                .AsNoTracking()
                .Where(rp =>
                    rp.RoleId == user.RoleId.Value &&
                    rp.Permission.IsActive &&
                    !rp.Permission.IsDeleted)
                .Select(rp => rp.Permission.Code)
                .ToListAsync();

            foreach (var code in roleCodes)
                codes.Add(code);

            var overrides = await _context.UserPermissions!
                .AsNoTracking()
                .Where(up =>
                    up.UserId == userId.Value &&
                    up.Permission.IsActive &&
                    !up.Permission.IsDeleted)
                .Select(up => new { up.IsGranted, up.Permission.Code })
                .ToListAsync();

            foreach (var rule in overrides)
            {
                if (rule.IsGranted)
                    codes.Add(rule.Code);
                else
                    codes.Remove(rule.Code);
            }

            return codes;
        }

        public IQueryable<RolePermissionExt> QueryRolePermissionsExt(Guid? roleId = null)
        {
            if (roleId != null)
            {
                return _context.RolePermissions
                .AsNoTracking()
                .Where(x => x.RoleId == roleId)
                .Select(x => new RolePermissionExt
                {
                    Id = x.Id,
                    RoleId = x.RoleId,
                    PermissionId = x.PermissionId,
                    RoleName = x.Role.Name,
                    PermissionCode = x.Permission.Code,
                    PermissionName = x.Permission.Name,
                    Module = x.Permission.Module,
                    IsActive = x.Permission.IsActive,
                    IsDeleted = x.Permission.IsDeleted
                });
            }

            return _context.RolePermissions
               .AsNoTracking()
               .Select(x => new RolePermissionExt
               {
                   Id = x.Id,
                   RoleId = x.RoleId,
                   PermissionId = x.PermissionId,
                   RoleName = x.Role.Name,
                   PermissionCode = x.Permission.Code,
                   PermissionName = x.Permission.Name,
                   Module = x.Permission.Module,
                   IsActive = x.Permission.IsActive,
                    IsDeleted = x.Permission.IsDeleted
               });
        }

        public IQueryable<UserPermissionExt> QueryUserPermissionsExt(Guid? userId = null)
        {
            var query = _context.UserPermissions.AsNoTracking();

            if (userId != null)
                query = query.Where(x => x.UserId == userId);

            return query.Select(x => new UserPermissionExt
            {
                Id = x.Id,
                UserId = x.UserId,
                PermissionId = x.PermissionId,
                IsGranted = x.IsGranted,
                UserName = x.User != null
                    ? ((x.User.FirstName ?? "") + " " + (x.User.LastName ?? "")).Trim()
                    : "",
                PermissionCode = x.Permission.Code,
                PermissionName = x.Permission.Name,
                Module = x.Permission.Module,
                IsActive = x.Permission.IsActive,
                IsDeleted = x.Permission.IsDeleted
            });
        }

        public IQueryable<AccessControlAuditLogExt> QueryUserAccessAudit(Guid userId)
        {
            return _context.AccessControlAuditLogs
                .AsNoTracking()
                .Where(log => log.TargetUserId == userId)
                .Select(log => new AccessControlAuditLogExt
                {
                    Id = log.Id,
                    TargetUserId = log.TargetUserId,
                    Action = log.Action,
                    PermissionCode = log.PermissionCode,
                    Details = log.Details,
                    CreatedDateUtc = log.CreatedDateUtc,
                    PerformedByUserName = _context.Users
                        .Where(u => u.Id == log.PerformedByUserId)
                        .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim())
                        .FirstOrDefault() ?? ""
                });
        }
    }
}
