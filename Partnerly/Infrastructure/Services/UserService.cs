using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleService _roleService;
        private readonly ILogService _logService;
        private readonly IEventBus _eventBus;

        public UserService(
            IUserRepository userRepo, 
            IRoleService roleService, 
            ILogService logService, 
            IEventBus eventBus)
        {
            _userRepo = userRepo;
            _roleService = roleService;
            _logService = logService;
            _eventBus = eventBus;
        }

        public async Task<User?> GetUserByEmailAsync(string? email) =>
            await _userRepo.GetByEmailAsync(email);

        public async Task<User?> GetUserByRefCodeAsync(string? refCode) =>
            await _userRepo.GetByRefCodeAsync(refCode);

        public async Task<User?> GetUserByPhoneAsync(string? phone) =>
            await _userRepo.GetByPhoneAsync(phone);

        public async Task<User?> GetUserByIDAsync(Guid? id) =>
            await _userRepo.GetByIdAsync(id);

        public async Task<User?> GetUserByIDAsync(string? id)
        {
            if (Guid.TryParse(id, out var guid))
            {
                return await GetUserByIDAsync(guid);
            }

            return null;
        }

        public async Task<IEnumerable<User?>> GetAllUsersAsync() =>
            await _userRepo.GetAllAsync();

        public async Task<IEnumerable<User?>> GetReferralsAsync(Guid? userID) =>
            await _userRepo.GetReferralsAsync(userID);

        public IQueryable<User> QueryEntity(IEnumerable<string> fields) =>
            _userRepo.QueryEntity(fields);

        public async Task<ServiceResult<User?>> CreateUserAsync(User? user)
        {
            if (user == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "User"));
                return ServiceResult<User?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "User") });
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
                    return ServiceResult<User?>.Fail(new List<string> { ErrorMessages.ReferrerUserCannotBeFoundOrInactive });
                }
            }

            Guid? roleID = user.RoleId;
            if (roleID == null)
            {
                Role? role = await _roleService.GetRoleByNameAsync(Constants.User);
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
            newUser.Balance = user.Balance ?? 0m;
            newUser.EmailConfirmed = user.EmailConfirmed ?? false;
            newUser.IsBlocked = user.IsBlocked ?? false;
            newUser.IsDeleted = false;
            newUser.AllowSendEmails = true;
            newUser.AllowSendNotifications = true;
            newUser.PhotoUrl = Constants.DefaultUserProfilePhotoPath;
            newUser.Note = user.Note;
            newUser.GoogleId = user.GoogleId;

            if (!string.IsNullOrEmpty(newUser.FirstName))
            {
                newUser.PhotoUrl = AvatarGenerator.GetOrCreateAvatar(newUser.FirstName);
            }

            try
            {
                string withResource = "Internal";
                if (newUser.GoogleId != null && newUser.PasswordHash == null)
                {
                    withResource = "Google";
                    var generatedPassword = PasswordGenerator.Generate();
                    newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword);
                }

                await _userRepo.AddAsync(newUser);
                await _userRepo.SaveChangesAsync();

                bool sendEmailTo = newUser?.Id == newUser?.CreatedBy;
                await _eventBus.PublishAsync(new UserRegisteredEvent(newUser: newUser, sendEmail: sendEmailTo, sendNote: true, registeredWithResource: withResource));
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<User?>.Fail(new List<string> { ex.Message });
            }

            return ServiceResult<User?>.Ok(newUser);
        }

        public async Task<ServiceResult<User?>> UpdateUserAsync(User? user, Guid? currentUserId = null)
        {           
            if (user == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "User"));
                return ServiceResult<User?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "User") });
            }

            if (await _userRepo.GetByIdAsync(user.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "User"));
                return ServiceResult<User?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "User") });
            }

            try
            {
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<User?>.Fail(new List<string> { ex.Message });
            }
  
            return ServiceResult<User?>.Ok(user);
        }

        public async Task<ServiceResult<User?>> DeleteUserAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "User ID"));
                return ServiceResult<User?>.Fail(new List<string> { });
            }

            var user = await _userRepo.GetByIdAsync((Guid)id);

            if (user == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "User"));
                return ServiceResult<User?>.Fail(new List<string> { });
            }

            try
            {
                //_userRepo.Delete(user);
                //await _userRepo.SaveChangesAsync();
                user.IsDeleted = true;
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<User?>.Fail(new List<string> { ex.Message });
            }

            return ServiceResult<User?>.Ok(user);
        }
    }
}
