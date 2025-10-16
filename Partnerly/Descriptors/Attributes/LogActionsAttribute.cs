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

        public const string EmailSending = "ES";

        public LogActionsAttribute()
            : base(
                UserSignIn, Constants.UserSignIn,
                UserSignOut, Constants.UserSignOut,

                UserCreated, Constants.UserCreated,
                UserDeleted, Constants.UserDeleted,
                UserUpdated, Constants.UserUpdated,

                RoleCreated, Constants.RoleCreated,
                RoleDeleted, Constants.RoleDeleted,
                RoleUpdated, Constants.RoleUpdated,

                PaymentCreated, Constants.PaymentCreated,
                PaymentDeleted, Constants.PaymentDeleted,
                PaymentUpdated, Constants.PaymentUpdated,

                SystemSettingsCreated, Constants.SystemSettingsCreated,
                SystemSettingsDeleted, Constants.SystemSettingsDeleted,
                SystemSettingsUpdated, Constants.SystemSettingsUpdated,

                EmailConfirmationTokenCreated, Constants.EmailConfirmationTokenCreated,
                EmailConfirmationTokenDeleted, Constants.EmailConfirmationTokenDeleted,
                EmailConfirmationTokenUpdated, Constants.EmailConfirmationTokenUpdated,

                EmailTemplateCreated, Constants.EmailTemplateCreated,
                EmailTemplateDeleted, Constants.EmailTemplateDeleted,
                EmailTemplateUpdated, Constants.EmailTemplateUpdated,

                NotificationCreated, Constants.NotificationCreated,
                NotificationDeleted, Constants.NotificationDeleted,
                NotificationUpdated, Constants.NotificationUpdated,

                UserGroupCreated, Constants.NotificationCreated,
                UserGroupDeleted, Constants.NotificationDeleted,
                UserGroupUpdated, Constants.NotificationUpdated,

                EmailSending, Constants.EmailSending
                )
        { }
    }
}
