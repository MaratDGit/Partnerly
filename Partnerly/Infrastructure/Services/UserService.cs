using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<User> CreateUserAsync(User user)
        {
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

            await _userRepo.AddAsync(newUser);
            await _userRepo.SaveChangesAsync();
            return newUser;
        }

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _userRepo.GetByEmailAsync(email);

        public async Task<IEnumerable<User>> GetAllUsersAsync() =>
            await _userRepo.GetAllAsync();

        public async Task UpdateUserAsync(User user)
        {
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user != null)
            {
                _userRepo.Delete(user);
                await _userRepo.SaveChangesAsync();
            }
        }
    }
}
