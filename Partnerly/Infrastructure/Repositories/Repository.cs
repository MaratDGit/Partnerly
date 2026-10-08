using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace Partnerly.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid? id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public IQueryable<T> QueryEntity(IEnumerable<string> fields)
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
                typeof(T),
                "u");

            var bindings = new List<MemberBinding>();

            foreach (var field in fieldList)
            {
                var property = typeof(T)
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

                var isNotMapped =
                    property.GetCustomAttributes(
                        typeof(NotMappedAttribute),
                        true)
                    .Any();

                if (isNotMapped)
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
                Expression.New(typeof(T)),
                bindings);

            var selector =
                Expression.Lambda<Func<T, T>>(
                    body,
                    parameter);

            return _dbSet
                .AsNoTracking()
                .Select(selector);
        }
    }
}
