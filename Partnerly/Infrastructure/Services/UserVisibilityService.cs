using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public sealed class UserVisibilityService : IUserVisibilityService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UserVisibilityService(
            AppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public IQueryable<Guid> VisibleUserIds()
        {
            var currentId = _currentUser.UserId;
            var roleName = _currentUser.RoleName?.Trim();

            if (currentId == null)
                return _context.Users.Where(_ => false).Select(u => u.Id);

            if (IsRole(roleName, Constants.SuperRoleName))
                return _context.Users.AsNoTracking().Select(u => u.Id);

            if (IsRole(roleName, Constants.Admin))
            {
                return _context.Users.AsNoTracking()
                    .Where(u =>
                        u.Id == currentId ||
                        !_context.Roles.Any(r =>
                            r.Id == u.RoleId &&
                            (r.Name == Constants.Admin || r.Name == Constants.SuperRoleName)))
                    .Select(u => u.Id);
            }

            if (IsRole(roleName, Constants.Employee))
            {
                return _context.Users.AsNoTracking()
                    .Where(u =>
                        u.Id == currentId ||
                        !_context.Roles.Any(r =>
                            r.Id == u.RoleId &&
                            (r.Name == Constants.Admin || r.Name == Constants.SuperRoleName)))
                    .Select(u => u.Id);
            }

            return _context.Users.AsNoTracking()
                .Where(u => u.Id == currentId)
                .Select(u => u.Id);
        }

        public async Task<bool> CanViewUserAsync(Guid? userId)
        {
            if (userId == null)
                return false;

            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            return await CanViewUserAsync(user);
        }

        public async Task<bool> CanViewSupportTicketAsync(Guid? creatorUserId, Guid? assignedTo)
        {
            if (_currentUser.UserId == null)
                return false;

            if (creatorUserId == _currentUser.UserId || assignedTo == _currentUser.UserId)
                return true;

            return await CanViewUserAsync(creatorUserId);
        }

        public Task<bool> CanViewUserAsync(User? user)
        {
            if (user == null || _currentUser.UserId == null)
                return Task.FromResult(false);

            if (user.Id == _currentUser.UserId)
                return Task.FromResult(true);

            var roleName = _currentUser.RoleName?.Trim();

            if (IsRole(roleName, Constants.SuperRoleName))
                return Task.FromResult(true);

            var targetRoleName = user.Role?.Name;
            if (string.IsNullOrWhiteSpace(targetRoleName) && user.RoleId != null)
            {
                targetRoleName = _context.Roles.AsNoTracking()
                    .Where(r => r.Id == user.RoleId)
                    .Select(r => r.Name)
                    .FirstOrDefault();
            }

            if (IsRole(roleName, Constants.Admin))
            {
                return Task.FromResult(
                    !IsRole(targetRoleName, Constants.Admin) &&
                    !IsRole(targetRoleName, Constants.SuperRoleName));
            }

            if (IsRole(roleName, Constants.Employee))
            {
                return Task.FromResult(
                    !IsRole(targetRoleName, Constants.Admin) &&
                    !IsRole(targetRoleName, Constants.SuperRoleName));
            }

            return Task.FromResult(false);
        }

        public async Task<IEnumerable<User>> FilterUsersAsync(IEnumerable<User?> users)
        {
            var result = new List<User>();
            foreach (var user in users)
            {
                if (user != null && await CanViewUserAsync(user))
                    result.Add(user);
            }

            return result;
        }

        private static bool IsRole(string? actual, string expected) =>
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}
