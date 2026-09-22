using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class SupportTicketStatusAttribute : StringListAttribute
    {
        public const string New = "N";
        public const string InProgress = "P";
        public const string Closed = "C";
        public static List<string> SupportTicketStatuses = new List<string> { New, InProgress, Closed };

        public SupportTicketStatusAttribute()
            : base(
                New, FieldsDisplayNames.New,
                InProgress, FieldsDisplayNames.InProgress,
                Closed, FieldsDisplayNames.Closed)
        { }
    }
}
