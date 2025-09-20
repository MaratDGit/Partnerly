using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IEmailTemplateRepository : IRepository<EmailTemplate>
    {
        Task<EmailTemplate?> GetByNameAsync(string? name);
    }
}
