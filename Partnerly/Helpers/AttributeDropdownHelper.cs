using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
    }

}
