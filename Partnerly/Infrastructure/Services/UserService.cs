using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUser;

        public UserService(IUserRepository userRepo, IPermissionService permissionService, ICurrentUserService currentUser)
        {
            _userRepo = userRepo;
            _permissionService = permissionService;
            _currentUser = currentUser;
        }

        public async Task<User?> CreateUserAsync(User? user)
        {
            if (user == null)
                return null;

            var newUser = new User { Id = Guid.NewGuid() };
            newUser.Email = user.Email;
            newUser.Phone = user.Phone;
            newUser.FirstName = user.FirstName;
            newUser.LastName = user.LastName;
            newUser.PasswordHash = user.PasswordHash;
            newUser.ReferrerId = user.ReferrerId;
            newUser.RoleId = user.RoleId;

            newUser.IsDeleted = false;
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.UpdatedDate = DateTime.UtcNow;
            newUser.CreatedBy = newUser.Id;
            newUser.UpdatedBy = newUser.Id;

            string? result = ValidationHelper.ValidateEntityRequiredFields(newUser, out bool isValid);
            if (!isValid)
            {
                throw new ValidationException(String.Format(ErrorMessages.RequiredFieldsValidationFailed, result));
            }

            await _userRepo.AddAsync(newUser);
            await _userRepo.SaveChangesAsync();
            return newUser;
        }

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _userRepo.GetByEmailAsync(email);

        public async Task<User?> GetUserByIDAsync(Guid id) =>
            await _userRepo.GetByIdAsync(id);

        public async Task<IEnumerable<User?>> GetAllUsersAsync() =>
            await _userRepo.GetAllAsync();

        public async Task UpdateUserAsync(User? user)
        {
            if (_currentUser.UserId == null || user == null)
                return;

            if (!await _permissionService.CanUpdateAsync(_currentUser.UserId, user))
                throw new UnauthorizedAccessException(ErrorMessages.NoPermissionForThisAction);

            string? result = ValidationHelper.ValidateEntityRequiredFields(user, out bool isValid);
            
            if (!isValid)
                throw new ValidationException(String.Format(ErrorMessages.RequiredFieldsValidationFailed, result));

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(Guid? id)
        {
            if (id == null || _currentUser.UserId == null)
                return;

            var user = await _userRepo.GetByIdAsync((Guid)id);
            if (user != null)
            {
                if (!await _permissionService.CanDeleteAsync(_currentUser.UserId, user))
                    throw new UnauthorizedAccessException(ErrorMessages.NoPermissionForThisAction);

                _userRepo.Delete(user);
                await _userRepo.SaveChangesAsync();
            }
        }
    }
}
