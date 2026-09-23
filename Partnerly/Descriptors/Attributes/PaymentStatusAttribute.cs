using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class PaymentStatusAttribute : StringListAttribute
    {
        public const string Open = "O";
        public const string Pending = "P";
        public const string Payed = "A";
        public const string Closed = "C";
        public const string Canceled = "D";
        public const string Rejected = "R";

        public PaymentStatusAttribute()
            : base(
                Open, FieldsDisplayNames.Open,
                Pending, FieldsDisplayNames.Pending,
                Payed, FieldsDisplayNames.Payed,
                Closed, FieldsDisplayNames.Closed,
                Canceled, FieldsDisplayNames.Canceled,
                Rejected, FieldsDisplayNames.Rejected)
        { }
    }
}
