using Partnerly.Infrastructure.Services;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<EmailTemplate?> GetTemplateByIDAsync(Guid? name);
        Task<EmailTemplate?> GetTemplateByNameAsync(string? name);
        Task<IEnumerable<EmailTemplate?>> GetAllTemplatesAsync();

        Task<ServiceResult<EmailTemplate?>> CreateTemplateAsync(EmailTemplate? template);
        Task<ServiceResult<EmailTemplate?>> UpdateTemplateAsync(EmailTemplate? template);
        Task<ServiceResult<EmailTemplate?>> DeleteTemplateAsync(Guid? id);
    }
}
