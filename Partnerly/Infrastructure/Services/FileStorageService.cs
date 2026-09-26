using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        private const string AttachmentsFolder = "uploads/attachments";

        public FileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<(string storedFileName, string storagePath)> SaveAsync(
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            var extension = Path.GetExtension(file.FileName);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";

            var datePath = Path.Combine(
                DateTime.UtcNow.Year.ToString(),
                DateTime.UtcNow.Month.ToString("D2"));

            var relativeDirectory = Path.Combine(
                AttachmentsFolder,
                datePath);

            var physicalDirectory = Path.Combine(
                _environment.WebRootPath,
                relativeDirectory);

            Directory.CreateDirectory(physicalDirectory);

            var physicalPath = Path.Combine(
                physicalDirectory,
                storedFileName);

            await using var stream = new FileStream(
                physicalPath,
                FileMode.Create);

            await file.CopyToAsync(stream);

            var storagePath = Path.Combine(
                relativeDirectory,
                storedFileName)
                .Replace("\\", "/");

            return (storedFileName, storagePath);
        }

        public Task DeleteAsync(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                return Task.CompletedTask;

            var physicalPath = Path.Combine(
                _environment.WebRootPath,
                storagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            return Task.CompletedTask;
        }

        public Task<Stream?> OpenAsync(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                return Task.FromResult<Stream?>(null);

            var physicalPath = Path.Combine(
                _environment.WebRootPath,
                storagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!File.Exists(physicalPath))
                return Task.FromResult<Stream?>(null);

            Stream stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return Task.FromResult<Stream?>(stream);
        }
    }
}