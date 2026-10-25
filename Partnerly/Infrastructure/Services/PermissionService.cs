using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission?>> GetAllPermissionAsync() =>
            await _context.Permissions.ToListAsync();

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

        //public async Task<bool> CanUpdateAsync<T>(
        //    Guid? userID,
        //    T entity)
        //    where T : class, IAuditableEntity
        //{
        //    if (userID == null)
        //        return false;

        //    if (await HasPermissionAsync(userID, "records.edit"))
        //        return true;

        //    return entity.CreatedBy == userID.Value;
        //}

        //public async Task<bool> CanDeleteAsync<T>(
        //    Guid? userID,
        //    T entity)
        //    where T : class, IAuditableEntity
        //{
        //    if (userID == null)
        //        return false;

        //    if (await HasPermissionAsync(userID, "records.delete"))
        //        return true;

        //    return entity.CreatedBy == userID.Value;
        //}

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

        //public async Task<bool> CanUpdateAsync<T>(Guid? userID, T entity) where T : class, IAuditableEntity
        //{
        //    bool retVal = false;

        //    if (userID == null || _context?.Users == null)
        //        return retVal;

        //    User? currUser = await _context.Users.FindAsync((Guid)userID);

        //    if (currUser == null) 
        //        return retVal;

        //    if (await HasPermision(currUser, RoleTypeAttribute.Update))
        //        return true;

        //    retVal = entity.CreatedBy == currUser.Id;

        //    return retVal;
        //}

        //public async Task<bool> CanDeleteAsync<T>(Guid? userID, T entity) where T : class, IAuditableEntity
        //{
        //    bool retVal = false;

        //    if (userID == null || _context?.Users == null)
        //        return retVal;

        //    User? currUser = await _context.Users.FindAsync((Guid)userID);

        //    if (currUser == null)
        //        return retVal;

        //    if (await HasPermision(currUser, RoleTypeAttribute.Delete))
        //        return true;

        //    retVal = entity.CreatedBy == currUser.Id;

        //    return retVal;
        //}

        //private async Task<bool> HasPermision(User user, string roleType)
        //{
        //    bool retval = false;

        //    if (_context?.Roles == null)
        //        return retval;

        //    var role = await _context.Roles.FindAsync(user.RoleId);

        //    if (role != null && roleType != null)
        //    {
        //        switch (role.Type)
        //        {
        //            case RoleTypeAttribute.View: return role.Type == roleType;
        //            case RoleTypeAttribute.Update: return role.Type == roleType;
        //            case RoleTypeAttribute.Delete: return true;
        //            default: return false;
        //        }
        //    }

        //    return retval;
        //}
    }
}
