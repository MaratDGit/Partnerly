using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Repositories
{
    public class LogRepository : Repository<Log>, ILogRepository
    {
        public LogRepository(AppDbContext context) : base(context) { }
    }
}
