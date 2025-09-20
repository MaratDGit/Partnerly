using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class SystemSettingsRepository : Repository<SystemSettings>, ISystemSettingsRepository
    {
        public SystemSettingsRepository(AppDbContext context) : base(context) { }

        public async Task<SystemSettings?> GetBySetupIDAsync(int? id)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Id == id && id != null);
        }
    }
}
