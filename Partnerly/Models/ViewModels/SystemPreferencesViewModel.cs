using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class SystemPreferencesViewModel : IAuditableViewModel
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

        #region Audit fields
        [Display(Name = nameof(FieldsDisplayNames.CreatorName), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatedByUserName { get; set; }
        public Guid? CreatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.CreatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdaterName), ResourceType = typeof(FieldsDisplayNames))]
        public string? UpdatedByUserName { get; set; }
        public Guid? UpdatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        public FormActionsViewModel StickyButtons
        {
            get
            {
                return new Partnerly.Models.ViewModels.FormActionsViewModel
                {
                    Back = new Partnerly.Models.ViewModels.ActionBack(
                                visible: true,
                                fromController: "SystemPreferences",
                                toController: "Home",
                                fromAction: "Index",
                                toAction: "Index"
                                ) { Permission = PermissionCodes.SystemSettingsView },

                    Undo = new Partnerly.Models.ViewModels.ActionUndo(
                                visible: true,
                                fromController: "SystemPreferences",
                                toController: "SystemPreferences",
                                fromAction: "Index",
                                toAction: "Index"
                                ) { Permission = PermissionCodes.SystemSettingsView },

                    Save = new Partnerly.Models.ViewModels.ActionSave(
                                visible: true,
                                fromController: "SystemPreferences",
                                toController: "SystemPreferences",
                                fromAction: "Index",
                                toAction: "Index"
                                ) { Permission = PermissionCodes.SystemSettingsEdit },

                    Note = new Partnerly.Models.ViewModels.ActionNote(
                                visible: true,
                                note: Note
                                ) { Permission = PermissionCodes.SystemSettingsEdit },
                };
            }
        }
    }
}
