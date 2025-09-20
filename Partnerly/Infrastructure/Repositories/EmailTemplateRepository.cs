using Microsoft.EntityFrameworkCore;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
    {
        public EmailTemplateRepository(AppDbContext context) : base(context) { }

        public async Task<EmailTemplate?> GetByNameAsync(string? name)
        {
            return await _dbSet.FirstOrDefaultAsync(u => name != null && u.Name == name);
        }
    }
}
