using Partnerly.Descriptors.Messages;

namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    public class StringListAttribute : Attribute
    {
        public IReadOnlyList<(string Value, string Label)> Items { get; }

        public StringListAttribute(params string[] items)
        {
            if (items.Length % 2 != 0)
                throw new ArgumentException(ErrorMessages.Itemsshouldbepassedaspairs);

            var list = new List<(string, string)>();
            for (int i = 0; i < items.Length; i += 2)
            {
                list.Add((items[i], items[i + 1]));
            }

            Items = list;
        }
    }
}
