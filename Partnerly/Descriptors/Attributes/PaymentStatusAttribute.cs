using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class PaymentStatusAttribute : StringListAttribute
    {
        public const string Open = "O";
        public const string Pending = "P";
        public const string Closed = "C";
        public const string Canceled = "D";
        public const string Rejected = "R";

        public PaymentStatusAttribute()
            : base(
                Open, Constants.Open,
                Pending, Constants.Pending,
                Closed, Constants.Closed,
                Canceled, Constants.Canceled,
                Rejected, Constants.Rejected)
        { }
    }
}
