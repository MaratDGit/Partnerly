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
        // Log types
        public const string Information = "Information";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Critical = "Critical";
    }
}
