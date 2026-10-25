using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Infrastructure.Repositories
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context) { }

        public async Task<Role?> GetByNameAsync(string? name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Name == name && name != null);
        }

        public async Task<RolePermission?> GetPermissionAsync(Guid roleId, Guid permissionId)
        {
            return await _context.Set<RolePermission>()
                .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
        }

        public async Task AddRolePermissionAsync(RolePermission member)
        {
            await _context.Set<RolePermission>().AddAsync(member);
        }

        public void DeleteRolePermission(RolePermission member)
        {
            _context.Set<RolePermission>().Remove(member);
        }
    }
}
