using Partnerly.Descriptors.Attributes;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class Payment : IAuditableEntity
    {
        #region ID
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        #endregion
        #region UserId
        [Required]
        [Display(Name = "User ID")]
        public Guid UserId { get; set; }
        #endregion
        #region Amount
        [Required]
        [Display(Name = "Amount")]
        public decimal? Amount { get; set; }
        #endregion
        #region PaymentMethod
        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }
        #endregion
        #region Status
        [Required]
        [Display(Name = "Payment Method")]
        [PaymentStatus]
        public string Status { get; set; }
        #endregion
        #region System Columns
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        #endregion

        public User? User { get; set; }
    }

}
