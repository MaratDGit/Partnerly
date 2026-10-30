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

        #region TaskTickedID
        [Display(Name = "Task Ticked ID")]
        public string? TaskTickedID { get; set; }
        #endregion

        #region ScrollingText
        [Display(Name = "Scrolling Text")]
        public string? ScrollingText { get; set; }
        #endregion

        #region Appearance
        [MaxLength(100)]
        public string? ApplicationName { get; set; }

        [MaxLength(1000)]
        public string? MaintenanceMessage { get; set; }

        [MaxLength(10)]
        public string? DefaultLanguage { get; set; }

        [MaxLength(100)]
        public string? TimeZoneId { get; set; }

        public bool? IsAiAssistantEnabled { get; set; }
        #endregion

        #region Registration and access
        public Guid? DefaultNewUserRoleId { get; set; }

        public bool? EnableGoogleLogin { get; set; }

        public int? PersistentCookieDays { get; set; }

        public int? SessionExpireMinutes { get; set; }

        public int? MaxProfilePhotoSizeMb { get; set; }

        public int? MaxFailedLoginAttempts { get; set; }

        public int? LockoutDurationMinutes { get; set; }
        #endregion

        #region Mail and notifications
        public bool? EnableEmailSending { get; set; }

        public bool? EnableNotifications { get; set; }

        public bool? EmailAdminOnNewRegistration { get; set; }

        public bool? EmailAdminOnNewTicket { get; set; }

        [MaxLength(256)]
        public string? AdminNotificationEmail { get; set; }

        public bool? DefaultAllowSendEmails { get; set; }

        public bool? DefaultAllowSendNotifications { get; set; }
        #endregion

        #region Files
        public int? MaxAttachmentSizeMb { get; set; }

        [MaxLength(500)]
        public string? AllowedAttachmentExtensions { get; set; }
        #endregion

        #region Tickets
        public Guid? TicketAutoAssignUserId { get; set; }

        public Guid? TicketAutoAssignRoleId { get; set; }
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
