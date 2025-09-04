namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    public class DefaultValueAttribute : Attribute
    {
        public object Value { get; }

        public DefaultValueAttribute(object value)
        {
            Value = value;
        }
    }
}
