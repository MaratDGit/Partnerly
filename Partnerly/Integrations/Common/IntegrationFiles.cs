using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Integrations.Common
{
    // Files sent to Telegram / Drive come from Partnerly attachments, never
    // from arbitrary disk paths.
    public static class IntegrationFiles
    {
        public static async Task<IntegrationFile?> OpenAttachmentAsync(
            ActionContext context,
            string attachmentId,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(attachmentId, out var id))
            {
                return null;
            }

            var attachments = context.Services.GetService<IAttachmentService>();
            if (attachments == null)
            {
                return null;
            }

            var attachment = await attachments.GetByIdAsync(id);
            if (attachment == null || attachment.IsDeleted)
            {
                return null;
            }

            var stream = await attachments.GetFileAsync(id);
            return stream == null
                ? null
                : new IntegrationFile(stream, attachment.FileName, string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType);
        }
    }

    public sealed class IntegrationFile
    {
        public IntegrationFile(Stream stream, string fileName, string contentType)
        {
            Stream = stream;
            FileName = fileName;
            ContentType = contentType;
        }

        public Stream Stream { get; }
        public string FileName { get; }
        public string ContentType { get; }
    }
}
