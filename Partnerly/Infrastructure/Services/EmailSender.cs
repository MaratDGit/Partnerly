using HandlebarsDotNet;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace Partnerly.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogService _logService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IAttachmentService _attachmentService;

        public EmailSender(IConfiguration config, ILogService logService, IEmailTemplateService emailTemplateService, IAttachmentService attachmentService)
        {
            _config = config;
            _logService = logService;
            _emailTemplateService = emailTemplateService;
            _attachmentService = attachmentService;
        }

        public async Task SendEmailWithTemplateAsync(string? templateName, string? toEmail, object model)
        {
            if (string.IsNullOrEmpty(templateName) || string.IsNullOrEmpty(toEmail))
                return;

            var emailMessage = await BuildEmailAsync(templateName, toEmail, model);

            if (emailMessage == null)
                return;

            await SendEmailAsync(emailMessage.To, emailMessage.Subject, emailMessage.BodyHtml, emailMessage.Attachments);
        }

        public async Task SendEmailWithoutTemplateAsync(string? toEmail, string? subject, string? message, List<Partnerly.Models.Attachment>? attachments = null)
        {
            if (string.IsNullOrEmpty(toEmail) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
                return;

            await SendEmailAsync(toEmail, subject, message, attachments);
        }

        private async Task<EmailMessage?> BuildEmailAsync(string templateName, string toEmail, object model)
        {
            var template = await _emailTemplateService.GetTemplateByNameAsync(templateName);

            if (template == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailSending, LogTypeAttribute.Error,
                    string.Format(ErrorMessages.Cannotbefound, $"templateName - {templateName}"));
                return null;
            }

            var subjectTemplate = Handlebars.Compile(template.Subject);
            string subject = subjectTemplate(model);

            var bodyHtmlTemplate = Handlebars.Compile(template.BodyHtml);
            string bodyHtml = bodyHtmlTemplate(model);

            string? bodyPlain = null;
            if (!string.IsNullOrEmpty(template.BodyPlain))
            {
                var bodyPlainTemplate = Handlebars.Compile(template.BodyPlain);
                bodyPlain = bodyPlainTemplate(model);
            }

            var attachments = await _attachmentService.GetAsync("EmailTemplate", template.Id);

            return new EmailMessage
            {
                To = toEmail,
                Subject = subject,
                BodyHtml = bodyHtml,
                BodyPlain = bodyPlain,
                Attachments = attachments
            };
        }

        private async Task SendEmailAsync(string email, string subject, string message, List<Partnerly.Models.Attachment>? attachments = null)
        {
            var smtpHost = _config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_config["EmailSettings:Port"]);
            var smtpUser = _config["EmailSettings:Username"];
            var smtpPass = _config["EmailSettings:Password"];
            var companyName = _config["EmailSettings:CompanyName"];

            if (smtpHost == null || smtpUser == null || smtpPass == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailSending, LogTypeAttribute.Error,
                                        $"Application Json required fields is empty SmtpServer - {smtpHost}, Username - {smtpUser}, Password - {smtpPass}");
                return;
            }

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;

                var fromAddress = new MailAddress(smtpUser, companyName);

                using (var mailMessage = new MailMessage(fromAddress, new MailAddress(email))
                {
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                })
                {
                    if (attachments != null && attachments.Any())
                    {
                        foreach (var att in attachments)
                        {
                            try
                            {
                                var stream =
                                    await _attachmentService.GetFileAsync(att.Id);

                                if (stream == null)
                                {
                                    await _logService.CreateLogAsync(
                                        LogActionsAttribute.EmailSending,
                                        LogTypeAttribute.Error,
                                        $"Attachment file not found: {att.FileName}");

                                    continue;
                                }

                                var mailAttachment =
                                    new System.Net.Mail.Attachment(
                                        stream,
                                        att.FileName,
                                        att.ContentType);

                                mailMessage.Attachments.Add(mailAttachment);
                            }
                            catch (Exception ex)
                            {
                                await _logService.CreateLogAsync(
                                    LogActionsAttribute.EmailSending,
                                    LogTypeAttribute.Error,
                                    $"Error adding attachment {att.FileName}: {ex.Message}");
                            }
                        }
                    }

                    await client.SendMailAsync(mailMessage);
                }
            }
        }
    }

    public class EmailMessage
    {
        public string To { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string BodyHtml { get; set; } = null!;
        public string? BodyPlain { get; set; }
        public List<Partnerly.Models.Attachment> Attachments { get; set; } = new();
    }
    public class EmailAttachment
    {
        public string FileName { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
