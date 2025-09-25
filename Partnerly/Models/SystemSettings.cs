using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class SystemSettings : IAuditableEntity
    {
        #region Key - ID
        [Key]
        public int Id { get; set; }
        #endregion

        #region IsMaintenanceMode
        [Display(Name = "Is Maintenance Mode")]
        public bool? IsMaintenanceMode { get; set; }
        #endregion

        #region EmailConfirmationTokenExpiredAtHours
        [Required]
        [Display(Name = "Email Confirmation Token ExpiredAt Hours")]
        public int? EmailConfirmationTokenExpiredAtHours { get; set; }
        #endregion

        #region ForgotPasswordTokenExpiredAtHours
        [Required]
        [Display(Name = "Forgot Password Token ExpiredAt Hours")]
        public int? ForgotPasswordTokenExpiredAtHours { get; set; }
        #endregion

        #region OnlineStatusAutoRefreshMinute
        [Required]
        [Display(Name = "Online Status Auto Refresh (Minute)")]
        public int? OnlineStatusAutoRefreshMinute { get; set; }
        #endregion

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
