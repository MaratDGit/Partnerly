using Partnerly.Descriptors.Attributes;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class Log : IAuditableEntity
    {
        #region ID
        [Required]
        public Guid? Id { get; set; } = Guid.NewGuid();
        #endregion
        #region UserID
        [Required]
        [Display(Name = "Creator User ID")]
        public Guid? CreatorUserId { get; set; }
        #endregion
        #region Action
        [Required]
        [Display(Name = "Action")]
        public string? Action { get; set; }
        #endregion
        #region Type
        [Required]
        [Display(Name = "Type")]
        [LogType]
        public string? Type { get; set; }
        #endregion
        #region LogMessage
        [Display(Name = "Message")]
        public string? LogMessage { get; set; }
        #endregion

        #region System Columns
        public bool IsDeleted { get; set; } = false;
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        public User? LogCreator { get; set; }
    }
}
