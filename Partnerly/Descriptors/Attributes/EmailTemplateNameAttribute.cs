using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class EmailTemplateNameAttribute : StringListAttribute
    {
        public const string EmailConfirmation = "EC";
        public const string ForgotPassword = "FP";
        public const string CriticalError = "CE";

        public EmailTemplateNameAttribute()
            : base(
                EmailConfirmation, FieldsDisplayNames.EmailConfirmation,
                ForgotPassword, FieldsDisplayNames.ForgotPassword,
                CriticalError, FieldsDisplayNames.CriticalError
              )
        { }
    }
}
