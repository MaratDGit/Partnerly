using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;

        public RoleService(IRoleRepository roleRepo, IPermissionService permissionService, ICurrentUserService currentUserService)
        {
            _roleRepo = roleRepo;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
        }

        public async Task<Role?> CreateRoleAsync(Role? role)
        {
            if (role == null)
                return null;

            var newRole = new Role { Id = Guid.NewGuid() };

            newRole.Name = role.Name;
            newRole.Type = role.Type;
            newRole.IsDeleted = false;

            await _roleRepo.AddAsync(newRole);
            await _roleRepo.SaveChangesAsync();
            return newRole;
        }

        public async Task<Role?> GetRoleByIDAsync(Guid? id) =>
            await _roleRepo.GetByIdAsync(id);

        public async Task<Role?> GetRoleByNameAsync(string? name) =>
            await _roleRepo.GetByNameAsync(name);

        public async Task<IEnumerable<Role?>> GetAllRolesAsync() =>
            await _roleRepo.GetAllAsync();

        public async Task UpdateRoleAsync(Role? role)
        {
            if (role == null)
                return;

            if (await _roleRepo.GetByIdAsync(role.Id) != null)
            {
                if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, role))
                    throw new UnauthorizedAccessException(ErrorMessages.NoPermissionForThisAction);

                _roleRepo.Update(role);
                await _roleRepo.SaveChangesAsync();
            }
        }

        public async Task DeleteRoleAsync(Guid? id)
        {
            if (id == null)
                return;

            var role = await _roleRepo.GetByIdAsync((Guid)id);
            if (role != null)
            {
                if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, role))
                    throw new UnauthorizedAccessException(ErrorMessages.NoPermissionForThisAction);

                _roleRepo.Delete(role);
                await _roleRepo.SaveChangesAsync();
            }
        }
    }
}
