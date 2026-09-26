using Microsoft.AspNetCore.Http;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IAttachmentService
    {
        Task<Attachment> UploadAsync(
                IFormFile file,
                string entityType,
                Guid entityId);

        Task<List<Attachment>> GetAsync(
            string entityType,
            Guid entityId);

        Task<Attachment?> GetByIdAsync(Guid id);

        Task DeleteAsync(Guid id);

        Task<Stream?> GetFileAsync(Guid id);
        Task<IEnumerable<Attachment?>> GetAllAsync();
    }

    public interface IFileStorageService
    {
        Task<(string storedFileName, string storagePath)> SaveAsync(
            IFormFile file);

        Task DeleteAsync(string storagePath);

        Task<Stream?> OpenAsync(string storagePath);
    }
}