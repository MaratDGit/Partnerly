using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.Linq.Expressions;

namespace Partnerly.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string? email)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .FirstOrDefaultAsync(u => u.Email == email && email != null);
        }

        public async Task<User?> GetByRefCodeAsync(string? refCode)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .FirstOrDefaultAsync(u => u.MyReferralCode == refCode && refCode != null);
        }

        public async Task<User?> GetByPhoneAsync(string? phone)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Referrer)
                .FirstOrDefaultAsync(u => u.Phone == phone && phone != null);
        }

        //public IQueryable<User> QueryUsers()
        //{
        //    return _dbSet.AsNoTracking();
        //}
        public IQueryable<User> QueryUsers(IEnumerable<string> fields)
        {
            var fieldList = fields
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

            if (!fieldList.Any())
            {
                fieldList.Add("Id");
            }

            var parameter = Expression.Parameter(
                typeof(User),
                "u");

            var bindings = new List<MemberBinding>();

            foreach (var field in fieldList)
            {
                var property = typeof(User)
                    .GetProperties()
                    .FirstOrDefault(p =>
                        string.Equals(
                            p.Name,
                            field,
                            StringComparison.OrdinalIgnoreCase));

                if (property == null)
                {
                    continue;
                }

                var propertyAccess = Expression.Property(
                    parameter,
                    property);

                var binding = Expression.Bind(
                    property,
                    propertyAccess);

                bindings.Add(binding);
            }

            var body = Expression.MemberInit(
                Expression.New(typeof(User)),
                bindings);

            var selector =
                Expression.Lambda<Func<User, User>>(
                    body,
                    parameter);

            return _dbSet
                .AsNoTracking()
                .Select(selector);
        }
    }
}
