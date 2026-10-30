using Microsoft.AspNetCore.Mvc.Rendering;
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

        #region Appearance
        [Display(Name = nameof(FieldsDisplayNames.ApplicationName), ResourceType = typeof(FieldsDisplayNames))]
        [StringLength(100)]
        public string? ApplicationName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.MaintenanceMessage), ResourceType = typeof(FieldsDisplayNames))]
        [StringLength(1000)]
        public string? MaintenanceMessage { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.DefaultLanguage), ResourceType = typeof(FieldsDisplayNames))]
        public string? DefaultLanguage { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.TimeZone), ResourceType = typeof(FieldsDisplayNames))]
        public string? TimeZoneId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.AiAssistantEnabled), ResourceType = typeof(FieldsDisplayNames))]
        public bool? IsAiAssistantEnabled { get; set; }
        public bool IsAiAssistantEnabledBool { get => IsAiAssistantEnabled ?? true; set => IsAiAssistantEnabled = value; }
        #endregion

        #region Registration and access
        public Guid? DefaultNewUserRoleId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.DefaultNewUserRole), ResourceType = typeof(FieldsDisplayNames))]
        public string? DefaultNewUserRoleName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.EnableGoogleLogin), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EnableGoogleLogin { get; set; }
        public bool EnableGoogleLoginBool { get => EnableGoogleLogin ?? true; set => EnableGoogleLogin = value; }

        [Display(Name = nameof(FieldsDisplayNames.PersistentCookieDays), ResourceType = typeof(FieldsDisplayNames))]
        [Range(1, 365)]
        public int? PersistentCookieDays { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.SessionExpireMinutes), ResourceType = typeof(FieldsDisplayNames))]
        [Range(1, 1440)]
        public int? SessionExpireMinutes { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.MaxProfilePhotoSizeMb), ResourceType = typeof(FieldsDisplayNames))]
        [Range(1, 50)]
        public int? MaxProfilePhotoSizeMb { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.MaxFailedLoginAttempts), ResourceType = typeof(FieldsDisplayNames))]
        [Range(0, 50)]
        public int? MaxFailedLoginAttempts { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.LockoutDurationMinutes), ResourceType = typeof(FieldsDisplayNames))]
        [Range(1, 1440)]
        public int? LockoutDurationMinutes { get; set; }
        #endregion

        #region Mail and notifications
        [Display(Name = nameof(FieldsDisplayNames.EnableEmailSending), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EnableEmailSending { get; set; }
        public bool EnableEmailSendingBool { get => EnableEmailSending ?? true; set => EnableEmailSending = value; }

        [Display(Name = nameof(FieldsDisplayNames.EnableNotifications), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EnableNotifications { get; set; }
        public bool EnableNotificationsBool { get => EnableNotifications ?? true; set => EnableNotifications = value; }

        [Display(Name = nameof(FieldsDisplayNames.EmailAdminOnNewRegistration), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EmailAdminOnNewRegistration { get; set; }
        public bool EmailAdminOnNewRegistrationBool { get => EmailAdminOnNewRegistration ?? false; set => EmailAdminOnNewRegistration = value; }

        [Display(Name = nameof(FieldsDisplayNames.EmailAdminOnNewTicket), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EmailAdminOnNewTicket { get; set; }
        public bool EmailAdminOnNewTicketBool { get => EmailAdminOnNewTicket ?? false; set => EmailAdminOnNewTicket = value; }

        [Display(Name = nameof(FieldsDisplayNames.AdminNotificationEmail), ResourceType = typeof(FieldsDisplayNames))]
        [EmailAddress]
        [StringLength(256)]
        public string? AdminNotificationEmail { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.DefaultAllowSendEmails), ResourceType = typeof(FieldsDisplayNames))]
        public bool? DefaultAllowSendEmails { get; set; }
        public bool DefaultAllowSendEmailsBool { get => DefaultAllowSendEmails ?? true; set => DefaultAllowSendEmails = value; }

        [Display(Name = nameof(FieldsDisplayNames.DefaultAllowSendNotifications), ResourceType = typeof(FieldsDisplayNames))]
        public bool? DefaultAllowSendNotifications { get; set; }
        public bool DefaultAllowSendNotificationsBool { get => DefaultAllowSendNotifications ?? true; set => DefaultAllowSendNotifications = value; }
        #endregion

        #region Files
        [Display(Name = nameof(FieldsDisplayNames.MaxAttachmentSizeMb), ResourceType = typeof(FieldsDisplayNames))]
        [Range(1, 200)]
        public int? MaxAttachmentSizeMb { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.AllowedAttachmentExtensions), ResourceType = typeof(FieldsDisplayNames))]
        [StringLength(500)]
        public string? AllowedAttachmentExtensions { get; set; }
        #endregion

        #region Tickets
        public Guid? TicketAutoAssignUserId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.TicketAutoAssignUser), ResourceType = typeof(FieldsDisplayNames))]
        public string? TicketAutoAssignUserName { get; set; }

        public Guid? TicketAutoAssignRoleId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.TicketAutoAssignRole), ResourceType = typeof(FieldsDisplayNames))]
        public string? TicketAutoAssignRoleName { get; set; }
        #endregion

        public IEnumerable<SelectListItem> TimeZoneOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LanguageOptions { get; set; } = new List<SelectListItem>();

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
