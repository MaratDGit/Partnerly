using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class EmailConfirmationToken : IAuditableEntity
    {
        [Key]
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool? Used { get; set; }

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
