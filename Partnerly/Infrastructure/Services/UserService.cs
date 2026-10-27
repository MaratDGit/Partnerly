using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleService _roleService;
        private readonly ILogService _logService;
        private readonly IEventBus _eventBus;
        private readonly IPermissionService _permissionService;

        public UserService(
            IUserRepository userRepo, 
            IRoleService roleService, 
            ILogService logService, 
            IEventBus eventBus,
            IPermissionService permissionService)
        {
            _userRepo = userRepo;
            _roleService = roleService;
            _logService = logService;
            _eventBus = eventBus;
            _permissionService = permissionService;
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

        public async Task<ReferralTreeQueryResult> GetReferralTreeAsync(
            Guid rootUserId,
            Guid viewerId,
            bool viewerIsSuper,
            bool canViewRoot)
        {
            const int maxDepth = 25;
            var users = await _userRepo.GetReferralGraphUsersAsync();
            var byId = users.ToDictionary(u => u.Id);

            if (!byId.TryGetValue(rootUserId, out var rootUser))
            {
                return new ReferralTreeQueryResult();
            }

            if (IsSuperUserRole(rootUser) && !viewerIsSuper)
            {
                return new ReferralTreeQueryResult { AccessDenied = true };
            }

            if (rootUserId != viewerId
                && !canViewRoot
                && !IsInReferralDownline(byId, viewerId, rootUserId, maxDepth))
            {
                return new ReferralTreeQueryResult { AccessDenied = true };
            }

            var visibleUsers = viewerIsSuper
                ? users
                : users.Where(u => !IsSuperUserRole(u)).ToList();

            var childrenByReferrer = visibleUsers
                .Where(u => u.ReferrerId != null)
                .GroupBy(u => u.ReferrerId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList());

            var tree = BuildReferralNode(rootUserId, 0, new HashSet<Guid>(), childrenByReferrer, byId, maxDepth);
            return new ReferralTreeQueryResult { Tree = tree };
        }

        private static bool IsInReferralDownline(
            IReadOnlyDictionary<Guid, User> byId,
            Guid ancestorId,
            Guid descendantId,
            int maxDepth)
        {
            if (ancestorId == descendantId)
                return true;

            var visited = new HashSet<Guid>();
            var current = descendantId;
            var depth = 0;

            while (depth++ < maxDepth)
            {
                if (!visited.Add(current))
                    return false;

                if (!byId.TryGetValue(current, out var user) || user.ReferrerId == null)
                    return false;

                if (user.ReferrerId.Value == ancestorId)
                    return true;

                current = user.ReferrerId.Value;
            }

            return false;
        }

        private static ReferralTreeNodeViewModel? BuildReferralNode(
            Guid userId,
            int level,
            HashSet<Guid> path,
            IReadOnlyDictionary<Guid, List<User>> childrenByReferrer,
            IReadOnlyDictionary<Guid, User> byId,
            int maxDepth)
        {
            if (level >= maxDepth || !path.Add(userId) || !byId.TryGetValue(userId, out var user))
                return null;

            var children = new List<ReferralTreeNodeViewModel>();
            if (childrenByReferrer.TryGetValue(userId, out var childUsers))
            {
                foreach (var child in childUsers)
                {
                    var childNode = BuildReferralNode(
                        child.Id,
                        level + 1,
                        path,
                        childrenByReferrer,
                        byId,
                        maxDepth);

                    if (childNode != null)
                        children.Add(childNode);
                }
            }

            path.Remove(userId);

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = user.MyReferralCode ?? user.Email ?? user.Id.ToString();

            return new ReferralTreeNodeViewModel
            {
                Id = user.Id,
                DisplayName = displayName,
                ReferralCode = user.MyReferralCode,
                PhotoUrl = ToPublicPhotoUrl(user.PhotoUrl),
                RoleName = user.Role?.Name,
                DirectCount = children.Count,
                TotalCount = 1 + children.Sum(c => c.TotalCount),
                Level = level,
                IsBlocked = user.IsBlocked == true,
                IsOnline = user.IsOnlayn == true,
                CreatedDate = user.CreatedDate,
                Children = children
            };
        }

        private static bool IsSuperUserRole(User user) =>
            string.Equals(user.Role?.Name?.Trim(), Constants.SuperRoleName, StringComparison.OrdinalIgnoreCase);

        private static string ToPublicPhotoUrl(string? photoUrl)
        {
            var path = string.IsNullOrWhiteSpace(photoUrl)
                ? Constants.DefaultUserProfilePhotoPath
                : photoUrl;

            return path.StartsWith("~") ? path[1..] : path;
        }

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

        public async Task<UserPermission?> GetUserPermissionByIdAsync(Guid id) =>
            await _userRepo.GetUserPermissionByIdAsync(id);

        public async Task<ServiceResult<UserPermission?>> AddPermissionAsync(Guid userId, Guid permissionId, bool isGranted = true)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return ServiceResult<UserPermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "User") });
            }

            var catalogPermission = await _permissionService.GetActivePermissionAsync(permissionId);
            if (catalogPermission == null)
            {
                return ServiceResult<UserPermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "Permission") });
            }

            var existing = await _userRepo.GetUserPermissionAsync(userId, permissionId);
            if (existing != null)
            {
                return ServiceResult<UserPermission?>.Fail(new List<string> { $"{FieldsDisplayNames.Permission} {ErrorMessages.UniqueValue}" });
            }

            var permission = new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted
            };

            await _userRepo.AddUserPermissionAsync(permission);
            _permissionService.AddAccessAudit(
                action: "UserPermissionAdded",
                targetUserId: userId,
                permissionCode: catalogPermission.Code,
                details: $"IsGranted={isGranted}");
            await _userRepo.SaveChangesAsync();

            return ServiceResult<UserPermission?>.Ok(permission);
        }

        public async Task<ServiceResult<UserPermission?>> DeletePermissionAsync(Guid userId, Guid permissionId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return ServiceResult<UserPermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "User") });
            }

            var permission = await _userRepo.GetUserPermissionAsync(userId, permissionId);
            if (permission == null)
            {
                return ServiceResult<UserPermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "User Permission") });
            }

            var catalogPermission = await _permissionService.GetActivePermissionAsync(permissionId);

            _userRepo.DeleteUserPermission(permission);
            _permissionService.AddAccessAudit(
                action: "UserPermissionRemoved",
                targetUserId: userId,
                permissionCode: catalogPermission?.Code);
            await _userRepo.SaveChangesAsync();

            return ServiceResult<UserPermission?>.Ok(permission);
        }

        public async Task<ServiceResult<UserPermission?>> UpdatePermissionGrantAsync(Guid userPermissionId, bool isGranted)
        {
            var permission = await _userRepo.GetUserPermissionByIdAsync(userPermissionId);
            if (permission == null)
            {
                return ServiceResult<UserPermission?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "User Permission") });
            }

            permission.IsGranted = isGranted;
            var catalogPermission = await _permissionService.GetActivePermissionAsync(permission.PermissionId);
            _permissionService.AddAccessAudit(
                action: "UserPermissionGrantUpdated",
                targetUserId: permission.UserId,
                permissionCode: catalogPermission?.Code,
                details: $"IsGranted={isGranted}");
            await _userRepo.SaveChangesAsync();

            return ServiceResult<UserPermission?>.Ok(permission);
        }
    }
}
