using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Models;

namespace Partnerly.Descriptors.Attributes
{
    public class NotificationTypeAttribute : StringListAttribute
    {
        public const string Information = "I";
        public const string Warning = "W";
        public const string Error = "E";
        public const string Success = "S";
        public static List<string> TypeDisplayNames = new List<string> { FieldsDisplayNames.Information, FieldsDisplayNames.Warning, FieldsDisplayNames.Error, FieldsDisplayNames.Success };

        public NotificationTypeAttribute()
            : base(
                Information, FieldsDisplayNames.Information,
                Warning, FieldsDisplayNames.Warning,
                Error, FieldsDisplayNames.Error,
                Success, FieldsDisplayNames.Success)
        { }
    }
}
