using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class LogTypeAttribute : StringListAttribute
    {
        public const string Information = "I";
        public const string Warning = "W";
        public const string Error = "E";
        public const string Critical = "C";

        public LogTypeAttribute()
            : base(
                Information, Constants.Information,
                Warning, Constants.Warning,
                Error, Constants.Error,
                Critical, Constants.Critical)
        { }
    }
}
