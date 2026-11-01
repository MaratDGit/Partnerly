using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class LogActionsAttribute : StringListAttribute
    {
        public const string UserSignIn = "UI";
        public const string UserSignOut = "UO";

        public const string UserCreated = "UC";
        public const string UserDeleted = "UD";
        public const string UserUpdated = "UU";

        public const string RoleCreated = "RC";
        public const string RoleDeleted = "RD";
        public const string RoleUpdated = "RU";

        public const string PaymentCreated = "PC";
        public const string PaymentDeleted = "PD";
        public const string PaymentUpdated = "PU";

        public const string SystemSettingsCreated = "SC";
        public const string SystemSettingsDeleted = "SD";
        public const string SystemSettingsUpdated = "SU";

        public const string EmailConfirmationTokenCreated = "EC";
        public const string EmailConfirmationTokenDeleted = "ED";
        public const string EmailConfirmationTokenUpdated = "EU";

        public const string EmailTemplateCreated = "TC";
        public const string EmailTemplateDeleted = "TD";
        public const string EmailTemplateUpdated = "TU";

        public const string NotificationCreated = "NC";
        public const string NotificationDeleted = "ND";
        public const string NotificationUpdated = "NU";

        public const string UserGroupCreated = "GC";
        public const string UserGroupDeleted = "GD";
        public const string UserGroupUpdated = "GU";

        public const string SupportTicketCreated = "FC";
        public const string SupportTicketDeleted = "FD";
        public const string SupportTicketUpdated = "FU";

        public const string EmailSending = "ES";
        public const string DataTableBulkDeleted = "BD";

        public LogActionsAttribute()
            : base(
                UserSignIn, FieldsDisplayNames.UserSignIn,
                UserSignOut, FieldsDisplayNames.UserSignOut,

                UserCreated, FieldsDisplayNames.UserCreated,
                UserDeleted, FieldsDisplayNames.UserDeleted,
                UserUpdated, FieldsDisplayNames.UserUpdated,

                RoleCreated, FieldsDisplayNames.RoleCreated,
                RoleDeleted, FieldsDisplayNames.RoleDeleted,
                RoleUpdated, FieldsDisplayNames.RoleUpdated,

                PaymentCreated, FieldsDisplayNames.PaymentCreated,
                PaymentDeleted, FieldsDisplayNames.PaymentDeleted,
                PaymentUpdated, FieldsDisplayNames.PaymentUpdated,

                SystemSettingsCreated, FieldsDisplayNames.SystemSettingsCreated,
                SystemSettingsDeleted, FieldsDisplayNames.SystemSettingsDeleted,
                SystemSettingsUpdated, FieldsDisplayNames.SystemSettingsUpdated,

                EmailConfirmationTokenCreated, FieldsDisplayNames.EmailConfirmationTokenCreated,
                EmailConfirmationTokenDeleted, FieldsDisplayNames.EmailConfirmationTokenDeleted,
                EmailConfirmationTokenUpdated, FieldsDisplayNames.EmailConfirmationTokenUpdated,

                EmailTemplateCreated, FieldsDisplayNames.EmailTemplateCreated,
                EmailTemplateDeleted, FieldsDisplayNames.EmailTemplateDeleted,
                EmailTemplateUpdated, FieldsDisplayNames.EmailTemplateUpdated,

                NotificationCreated, FieldsDisplayNames.NotificationCreated,
                NotificationDeleted, FieldsDisplayNames.NotificationDeleted,
                NotificationUpdated, FieldsDisplayNames.NotificationUpdated,

                UserGroupCreated, FieldsDisplayNames.UserGroupCreated,
                UserGroupDeleted, FieldsDisplayNames.UserGroupDeleted,
                UserGroupUpdated, FieldsDisplayNames.UserGroupUpdated,

                SupportTicketCreated, FieldsDisplayNames.SupportTicketCreated,
                SupportTicketDeleted, FieldsDisplayNames.SupportTicketDeleted,
                SupportTicketUpdated, FieldsDisplayNames.SupportTicketUpdated,

                EmailSending, FieldsDisplayNames.EmailSending,

                DataTableBulkDeleted, FieldsDisplayNames.DataTableBulkDeleted
                )
        { }
    }
}
