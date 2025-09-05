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
                PaymentUpdated, Constants.PaymentUpdated)
        { }
    }
}
