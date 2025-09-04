using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes
{
    public class RoleTypeAttribute : StringListAttribute
    {
        public const string View = "V";
        public const string Update = "U";
        public const string Delete = "D";

        public RoleTypeAttribute()
            : base(
                View, Constants.View,
                Update, Constants.Update,
                Delete, Constants.Delete)
        { }
    }
}
