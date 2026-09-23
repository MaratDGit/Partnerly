using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class NotificationTypeAttribute : StringListAttribute
    {
        public const string Information = "I";
        public const string Warning = "W";
        public const string Error = "E";
        public const string Success = "S";

        public NotificationTypeAttribute()
            : base(
                Information, FieldsDisplayNames.Information,
                Warning, FieldsDisplayNames.Warning,
                Error, FieldsDisplayNames.Error,
                Success, FieldsDisplayNames.Success)
        { }
    }
}
