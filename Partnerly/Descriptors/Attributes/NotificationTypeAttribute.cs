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
                Information, Constants.Information,
                Warning, Constants.Warning,
                Error, Constants.Error,
                Success, Constants.Success)
        { }
    }
}
