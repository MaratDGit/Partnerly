using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly ISystemSettingsService _systemSettingsService;

        public AttachmentService(
            AppDbContext context,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUserService,
            IAttachmentRepository attachmentRepository,
            ISystemSettingsService systemSettingsService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _attachmentRepository = attachmentRepository;
            _systemSettingsService = systemSettingsService;
        }

        public IQueryable<Attachment> QueryEntity(IEnumerable<string> fields) =>
            _attachmentRepository.QueryEntity(fields);

        public async Task<Attachment> UploadAsync(
            IFormFile file,
            string entityType,
            Guid entityId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            var settings = await _systemSettingsService.GetCurrentAsync();
            var maxMb = settings?.MaxAttachmentSizeMb ?? SystemSettingsDefaults.MaxAttachmentSizeMb;
            if (maxMb > 0 && file.Length > maxMb * 1024L * 1024L)
                throw new InvalidOperationException(string.Format(ErrorMessages.FileTooLarge, maxMb));

            var allowedRaw = string.IsNullOrWhiteSpace(settings?.AllowedAttachmentExtensions)
                ? SystemSettingsDefaults.AllowedAttachmentExtensions
                : settings.AllowedAttachmentExtensions;

            var allowed = allowedRaw
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().TrimStart('.').ToLowerInvariant())
                .Where(x => x.Length > 0)
                .ToHashSet();

            var extension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !allowed.Contains(extension))
                throw new InvalidOperationException(ErrorMessages.FileExtensionNotAllowed);

            var (storedFileName, storagePath) =
                await _fileStorageService.SaveAsync(file);

            var maxSortOrder = await _context.Attachments
                .Where(x =>
                    x.EntityType == entityType &&
                    x.EntityId == entityId &&
                    !x.IsDeleted)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0;

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),

                FileName = Path.GetFileName(file.FileName),

                StoredFileName = storedFileName,

                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,

                FileSize = file.Length,

                StoragePath = storagePath,

                EntityType = entityType,

                EntityId = entityId,

                SortOrder = maxSortOrder + 1,
            };

            _context.Attachments.Add(attachment);

            await _context.SaveChangesAsync();

            return attachment;
        }

        public async Task<IEnumerable<Attachment?>> GetAllAsync()
        {
            return await _context.Attachments.ToListAsync();
        }

        public async Task<List<Attachment>> GetAsync(
            string entityType,
            Guid entityId)
        {
            return await _context.Attachments
                .AsNoTracking()
                .Where(x =>
                    x.EntityType == entityType &&
                    x.EntityId == entityId &&
                    !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<Attachment?> GetByIdAsync(Guid id)
        {
            return await _context.Attachments
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.IsDeleted);
        }

        public async Task<Stream?> GetFileAsync(Guid id)
        {
            var attachment = await GetByIdAsync(id);

            if (attachment == null)
                return null;

            return await _fileStorageService.OpenAsync(
                attachment.StoragePath);
        }

        public async Task DeleteAsync(Guid id)
        {
            var attachment = await GetByIdAsync(id);

            if (attachment == null)
                return;

            attachment.IsDeleted = true;

            await _context.SaveChangesAsync();

            await _fileStorageService.DeleteAsync(
                attachment.StoragePath);
        }
    }
}