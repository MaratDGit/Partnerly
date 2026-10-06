using Partnerly.Descriptors.Attributes;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Partnerly.Models
{
    public class EmailConfirmationToken : IAuditableEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public Guid? UserId { get; set; }
        [Required]
        public string? Token { get; set; }
        [EmailTokenType]
        [Required]
        public string? TokenType { get; set; }
        [Required]
        public DateTime? ExpiresAt { get; set; }
        public bool? Used { get; set; }

        #region Unbound fields
        [NotMapped]
        public bool Selected { get; set; }
        #endregion

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
