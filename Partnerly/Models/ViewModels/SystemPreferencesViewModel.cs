using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class SystemPreferencesViewModel
    {
        #region Key - ID
        public int Id { get; set; }
        #endregion

        #region IsMaintenanceMode
        [Display(Name = FieldsDisplayNames.MaintenanceMode)]
        public bool? IsMaintenanceMode { get; set; }
        public bool IsMaintenanceModeBool { get => IsMaintenanceMode ?? false; set => IsMaintenanceMode = value; }
        #endregion

        #region EmailConfirmationTokenExpiredAtHours
        [Required]
        [Display(Name = FieldsDisplayNames.EmailConfirmationTokenHours)]
        public int? EmailConfirmationTokenExpiredAtHours { get; set; }
        #endregion

        #region ForgotPasswordTokenExpiredAtHours
        [Required]
        [Display(Name = FieldsDisplayNames.ForgotPasswordTokenExpiredAtHours)]
        public int? ForgotPasswordTokenExpiredAtHours { get; set; }
        #endregion

        #region OnlineStatusAutoRefreshMinute
        [Required]
        [Display(Name = FieldsDisplayNames.Automaticonlinestatusupdate)]
        public int? OnlineStatusAutoRefreshMinute { get; set; }
        #endregion
        public string? Note { get; set; }
    }
}
