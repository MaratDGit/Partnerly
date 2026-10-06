using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Partnerly.Models
{
    public class EmailTemplate : IAuditableEntity
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Subject { get; set; }
        [Required]
        public string? BodyHtml { get; set; }
        public string? BodyPlain { get; set; }

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
