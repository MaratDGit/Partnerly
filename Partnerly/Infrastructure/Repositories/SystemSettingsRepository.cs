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

        public async Task<string> GenerateNextCodeAsync(string prefix = "T-")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var setting = await _context.SystemSettings
                    .Where(s => s.Id == 1)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                string nextNumberStr = nextNumber.ToString();

                if (setting == null)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("System Preferences Cannot be found");
                }


                if (setting.TaskTickedID == null)
                {
                    setting.TaskTickedID = nextNumberStr;
                }
                else
                {
                    int lastNumber = int.TryParse(setting.TaskTickedID, out var num) ? num : 0;
                    nextNumber = lastNumber + 1;
                    setting.TaskTickedID = nextNumber.ToString();
                }

                _context.SystemSettings.Update(setting);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return $"{prefix}{nextNumber:D5}";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
