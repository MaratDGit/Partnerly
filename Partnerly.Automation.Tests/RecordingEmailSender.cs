using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Tests
{
    public sealed class RecordingEmailSender : IEmailSender
    {
        public int Calls { get; private set; }

        public Task SendEmailWithTemplateAsync(string? templateName, string? toEmail, object model)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task SendEmailWithoutTemplateAsync(
            string? toEmail,
            string? subject,
            string? message,
            List<Attachment>? attachments = null)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
