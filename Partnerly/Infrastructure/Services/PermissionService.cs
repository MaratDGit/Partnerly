using Partnerly.Descriptors.Attributes;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanUpdateAsync<T>(Guid? userID, T entity) where T : class, IAuditableEntity
        {
            bool retVal = false;

            if (userID == null)
                return retVal;
            
            User? currUser = await _context.Users.FindAsync((Guid)userID);
            
            if (currUser == null) 
                return retVal;

            if (await HasPermision(currUser, RoleTypeAttribute.Update))
                return true;

            retVal = entity.CreatedBy == currUser.Id;

            return retVal;
        }

        public async Task<bool> CanDeleteAsync<T>(Guid? userID, T entity) where T : class, IAuditableEntity
        {
            bool retVal = false;

            if (userID == null)
                return retVal;

            User? currUser = await _context.Users.FindAsync((Guid)userID);

            if (currUser == null)
                return retVal;

            if (await HasPermision(currUser, RoleTypeAttribute.Delete))
                return true;

            retVal = entity.CreatedBy == currUser.Id;

            return retVal;
        }

        private async Task<bool> HasPermision(User user, string roleType)
        {
            var role = await _context.Roles.FindAsync(user.RoleId);

            if (role != null && roleType != null)
            {
                switch (role.Type)
                {
                    case RoleTypeAttribute.View: return role.Type == roleType;
                    case RoleTypeAttribute.Update: return role.Type == roleType;
                    case RoleTypeAttribute.Delete: return true;
                    default: return false;
                }
            }

            return false;
        }
    }
}
