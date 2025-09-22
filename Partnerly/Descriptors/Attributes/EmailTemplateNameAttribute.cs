using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class EmailTemplateNameAttribute : StringListAttribute
    {
        public const string EmailConfirmation = "EC";

        public EmailTemplateNameAttribute()
            : base(
                EmailConfirmation, Constants.EmailConfirmation
              )
        { }
    }
}
