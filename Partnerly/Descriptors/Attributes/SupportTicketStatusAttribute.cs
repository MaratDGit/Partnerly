using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class SupportTicketStatusAttribute : StringListAttribute
    {
        public const string New = "N";
        public const string InProgress = "P";
        public const string Closed = "C";

        public SupportTicketStatusAttribute()
            : base(
                New, Constants.New,
                InProgress, Constants.InProgress,
                Closed, Constants.Closed)
        { }
    }
}
