using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;

        public UserService(IUserRepository userRepo, IRoleService roleService, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService)
        {
            _userRepo = userRepo;
            _roleService = roleService;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
        }

        public async Task<User?> GetUserByEmailAsync(string? email) =>
            await _userRepo.GetByEmailAsync(email);

        public async Task<User?> GetUserByRefCodeAsync(string? refCode) =>
            await _userRepo.GetByRefCodeAsync(refCode);

        public async Task<User?> GetUserByPhoneAsync(string? phone) =>
            await _userRepo.GetByPhoneAsync(phone);

        public async Task<User?> GetUserByIDAsync(Guid? id) =>
            await _userRepo.GetByIdAsync(id);

        public async Task<IEnumerable<User?>> GetAllUsersAsync() =>
            await _userRepo.GetAllAsync();

        public async Task<ServiceResult<User>> CreateUserAsync(User? user)
        {
            if (user == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, "User is null from controller");
                return ServiceResult<User>.Fail(new List<string> { });
            }

            var newUser = new User { Id = Guid.NewGuid() };

            string? refCode = null;
            bool freeCode = false;
            do
            {
                refCode = ReferralCodeGenerator.GenerateCode(user.FirstName ?? user.Email);
                freeCode = await GetUserByRefCodeAsync(refCode) == null;
            }
            while (!freeCode);

            Guid? referrerID = user.ReferrerId;
            if (referrerID == null)
            {
                User? superUser = await GetUserByRefCodeAsync(Constants.SuperReferralCode);
                referrerID = superUser?.Id;
            }
            else
            {
                User? referrerUser = await GetUserByIDAsync((Guid)referrerID);
                if (referrerUser == null || referrerUser?.IsBlocked == true || referrerUser?.IsDeleted == true)
                {
                    await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, ErrorMessages.ReferrerUserCannotBeFoundOrInactive);
                    return ServiceResult<User>.Fail(new List<string> { });
                }
            }

            Guid? roleID = user.RoleId;
            if (roleID == null)
            {
                Role? role = await _roleService.GetRoleByNameAsync(RoleTypeAttribute.User);
                roleID = role?.Id;
            }

            newUser.Email = user.Email;
            newUser.Phone = user.Phone;
            newUser.FirstName = user.FirstName;
            newUser.LastName = user.LastName;
            newUser.PasswordHash = user.PasswordHash;
            newUser.MyReferralCode = refCode;
            newUser.ReferrerId = referrerID;
            newUser.RoleId = roleID;
            newUser.IsBlocked = user.IsBlocked;
            newUser.IsDeleted = false;
            newUser.Balance = user.Balance ?? 0m;

            try
            {
                await _userRepo.AddAsync(newUser);
                await _userRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<User>.Fail(new List<string> { });
            }

            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult<User>> UpdateUserAsync(User? user)
        {
            if (user == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, "User is null from controller");
                return ServiceResult<User>.Fail(new List<string> {});
            }

            if (await _userRepo.GetByIdAsync(user.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, "User Cannot be found");
                return ServiceResult<User>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, user))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<User>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }

            try
            {
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<User>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }
  
            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult<User>> DeleteUserAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, "User ID is null from controller");
                return ServiceResult<User>.Fail(new List<string> { });
            }

            var user = await _userRepo.GetByIdAsync((Guid)id);

            if (user == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, "User cannot be found");
                return ServiceResult<User>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, user))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<User>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }

            _userRepo.Delete(user);
            await _userRepo.SaveChangesAsync();

            return ServiceResult<User>.Ok(user);
        }
    }
}
