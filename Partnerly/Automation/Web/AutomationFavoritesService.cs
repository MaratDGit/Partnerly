using Microsoft.EntityFrameworkCore;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Models;

namespace Partnerly.Automation.Web
{
    // Per-user favorites of the builder palette (triggers / actions).
    public class AutomationFavoritesService
    {
        public const int MaxKeyLength = 200;
        private const int MaxPerUser = 500;

        private readonly AppDbContext _db;

        public AutomationFavoritesService(AppDbContext db)
        {
            _db = db;
        }

        public static bool IsValidKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
            {
                return false;
            }

            return key.All(ch => char.IsLetterOrDigit(ch) || ch == ':' || ch == '.' || ch == '_' || ch == '-');
        }

        public async Task<List<string>> ListAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _db.AutomationFavorites
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .OrderBy(item => item.CreatedDate)
                .Select(item => item.ItemKey)
                .ToListAsync(cancellationToken);
        }

        public async Task SetAsync(Guid userId, string key, bool on, CancellationToken cancellationToken)
        {
            var existing = await _db.AutomationFavorites
                .FirstOrDefaultAsync(item => item.UserId == userId && item.ItemKey == key, cancellationToken);

            if (on && existing == null)
            {
                if (await _db.AutomationFavorites.CountAsync(item => item.UserId == userId, cancellationToken) >= MaxPerUser)
                {
                    return;
                }

                _db.AutomationFavorites.Add(new AutomationFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ItemKey = key,
                    CreatedDate = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (!on && existing != null)
            {
                _db.AutomationFavorites.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        // One-time import of favorites that used to live in the browser's localStorage.
        public async Task MergeAsync(Guid userId, IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            var existing = (await ListAsync(userId, cancellationToken)).ToHashSet(StringComparer.Ordinal);
            var added = false;
            foreach (var key in keys.Where(IsValidKey).Distinct(StringComparer.Ordinal).Take(MaxPerUser))
            {
                if (existing.Add(key))
                {
                    _db.AutomationFavorites.Add(new AutomationFavorite
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ItemKey = key,
                        CreatedDate = DateTime.UtcNow
                    });
                    added = true;
                }
            }

            if (added)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
