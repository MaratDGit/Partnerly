using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class Attachment : IAuditableEntity
    {
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Original file name uploaded by the user.
        /// Example: invoice.pdf
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        /// <summary>
        /// Unique file name used in physical storage.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string StoredFileName { get; set; } = null!;

        /// <summary>
        /// MIME type.
        /// Example: application/pdf
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ContentType { get; set; } = null!;

        /// <summary>
        /// File size in bytes.
        /// </summary>
        [Required]
        public long FileSize { get; set; }

        /// <summary>
        /// Relative path to the stored file.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string StoragePath { get; set; } = null!;

        /// <summary>
        /// Entity type.
        /// Example: EmailTemplate, SupportTicket, User
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = null!;

        /// <summary>
        /// ID of the entity this attachment belongs to.
        /// </summary>
        [Required]
        public Guid EntityId { get; set; }

        /// <summary>
        /// Display order of the attachment.
        /// </summary>
        public int SortOrder { get; set; }

        #region System Columns

        public string? Note { get; set; }

        public bool IsDeleted { get; set; } = false;

        [Required]
        public Guid? CreatedBy { get; set; }

        [Required]
        public DateTime? CreatedDate { get; set; }

        [Required]
        public Guid? UpdatedBy { get; set; }

        [Required]
        public DateTime? UpdatedDate { get; set; }

        #endregion
    }
}