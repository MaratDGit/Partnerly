using Partnerly.Infrastructure.Services;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailWithTemplateAsync(string? templateName, string? toEmail, object model);
        Task SendEmailWithoutTemplateAsync(string? toEmail, string? subject, string? message, List<EmailAttachment>? attachments = null);
    }
}
