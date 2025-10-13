using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes.BaseAttributes;

namespace Partnerly.Helpers
{
    public static class AttributeDropdownHelper
    {
        public static List<SelectListItem> FromAttribute<TAttribute>() where TAttribute : StringListAttribute, new()
        {
            var attribute = new TAttribute();
            var items = new List<SelectListItem>();

            foreach (var (value, label) in attribute.Items)
            {
                items.Add(new SelectListItem
                {
                    Value = value,
                    Text = label
                });
            }

            return items;
        }

        public static List<SelectListItem> ToSelectedList(List<string> list)
        {
            var items = new List<SelectListItem>();

            foreach (var line in list)
            {
                items.Add(new SelectListItem
                {
                    Value = line,
                    Text = line
                });
            }

            return items;
        }

        public static string? GetValue<TAttribute>(string text) where TAttribute : StringListAttribute, new()
        {
            string? retVal = null;
            var attribute = new TAttribute();

            if (text != null && attribute.Items.Count > 0)
            {
                foreach (var (value, label) in attribute.Items)
                {
                    if (value == text) return label;
                }
            }
            return retVal;
        }
    }

}
