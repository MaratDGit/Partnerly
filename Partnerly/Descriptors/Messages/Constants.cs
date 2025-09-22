using System.ComponentModel;

namespace Partnerly.Descriptors.Messages
{
    [Localizable(true)]
    public class Constants
    {
        public const string SuperReferralCode = "BRANCH111";
        public const string SuperUserEmail = "marat.iigservices@gmail.com";

        // Roles
        public const string View = "View";
        public const string Update = "Update";
        public const string Delete = "Delete";
        // Payments
        public const string Open = "Open";
        public const string Pending = "Pending";
        public const string Closed = "Closed";
        public const string Canceled = "Canceled";
        public const string Rejected = "Rejected";
        // Log 
        public const string UserSignIn = "User Sign In";
        public const string UserSignOut = "User Sign Out";

        public const string UserCreated = "User Creating";
        public const string UserDeleted = "User Deleting";
        public const string UserUpdated = "User Updating";

        public const string RoleCreated = "Role Creating";
        public const string RoleDeleted = "Role Deleting";
        public const string RoleUpdated = "Role Updating";

        public const string PaymentCreated = "Payment Creating";
        public const string PaymentDeleted = "Payment Deleting";
        public const string PaymentUpdated = "Payment Updating";

        public const string SystemSettingsCreated = "System Settings Creating";
        public const string SystemSettingsDeleted = "System Settings Deleting";
        public const string SystemSettingsUpdated = "System Settings Updating";

        public const string EmailConfirmationTokenCreated = "Email Confirmation Token Creating";
        public const string EmailConfirmationTokenDeleted = "Email Confirmation Token Deleting";
        public const string EmailConfirmationTokenUpdated = "Email Confirmation Token Updating";

        public const string EmailTemplateCreated = "Email Template Creating";
        public const string EmailTemplateDeleted = "Email Template Deleting";
        public const string EmailTemplateUpdated = "Email Template Updating";

        public const string EmailSending = "Email Sending"; 
        // Log types
        public const string Information = "Information";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Critical = "Critical";

        // Email Template Names
        public const string EmailConfirmation = "Email Confirmation";

        // Email Token Types
        public const string Registration = "Registration";
        public const string ForgotPassword = "Forgot Password";

    }
}
