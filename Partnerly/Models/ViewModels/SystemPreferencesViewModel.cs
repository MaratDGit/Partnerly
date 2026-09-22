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
        [Display(Name = nameof(FieldsDisplayNames.MaintenanceMode), ResourceType = typeof(FieldsDisplayNames))]
        public bool? IsMaintenanceMode { get; set; }
        public bool IsMaintenanceModeBool { get => IsMaintenanceMode ?? false; set => IsMaintenanceMode = value; }
        #endregion

        #region EmailConfirmationTokenExpiredAtHours
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.EmailConfirmationTokenHours), ResourceType = typeof(FieldsDisplayNames))]
        public int? EmailConfirmationTokenExpiredAtHours { get; set; }
        #endregion

        #region ForgotPasswordTokenExpiredAtHours
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.ForgotPasswordTokenExpiredAtHours), ResourceType = typeof(FieldsDisplayNames))]
        public int? ForgotPasswordTokenExpiredAtHours { get; set; }
        #endregion

        #region OnlineStatusAutoRefreshMinute
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Automaticonlinestatusupdate), ResourceType = typeof(FieldsDisplayNames))]
        public int? OnlineStatusAutoRefreshMinute { get; set; }
        #endregion

        #region ScrollingText
        [Display(Name = nameof(FieldsDisplayNames.ScrollingText), ResourceType = typeof(FieldsDisplayNames))]
        public string? ScrollingText { get; set; }
        #endregion
        public string? Note { get; set; }
    }
}
