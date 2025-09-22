using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class EmailTokenTypeAttribute : StringListAttribute
    {
        public const string Registration = "R";
        public const string ForgotPassword = "F";

        public EmailTokenTypeAttribute()
            : base(
                Registration, Constants.Registration,
                ForgotPassword, Constants.ForgotPassword)
        { }
    }
}
