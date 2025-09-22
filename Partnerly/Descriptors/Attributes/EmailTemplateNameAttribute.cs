using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class EmailTemplateNameAttribute : StringListAttribute
    {
        public const string EmailConfirmation = "EC";
        public const string ForgotPassword = "FP";

        public EmailTemplateNameAttribute()
            : base(
                EmailConfirmation, Constants.EmailConfirmation,
                ForgotPassword, Constants.ForgotPassword
              )
        { }
    }
}
