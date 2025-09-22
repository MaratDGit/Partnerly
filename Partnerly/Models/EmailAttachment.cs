using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class EmailAttachment : IAuditableEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? FileName { get; set; }
        [Required]
        public string? Path { get; set; }
        [Required]
        public string? ContentType { get; set; } = "application/octet-stream";

        #region System Columns
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
