using Partnerly.Descriptors.Attributes;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class SupportTicket : IAuditableEntity
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid? UserId { get; set; }
        [Required]
        public string? TicketID { get; set; }
        [Required]
        public string? Subject { get; set; }
        [Required]
        [SupportTicketStatus]
        public string? Message { get; set; }
        [Required]
        public string? Status { get; set; }
        public Guid? AssignedTo { get; set; }
        public bool? IsRead { get; set; } = false;

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
