namespace Partnerly.Helpers
{
    using Partnerly.Descriptors.Attributes.BaseAttributes;
    using System.Reflection;

    public static class DefaultValueHelper
    {
        public static void ApplyDefaults(object obj)
        {
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<DefaultValueAttribute>();
                if (attr != null && prop.CanWrite)
                {
                    var currentValue = prop.GetValue(obj);
                    var defaultValue = attr.Value;

                    if (currentValue == null || currentValue.Equals(GetDefault(prop.PropertyType)))
                    {
                        object convertedValue = Convert.ChangeType(defaultValue, prop.PropertyType);
                        prop.SetValue(obj, convertedValue);
                    }
                }
            }
        }

        private static object? GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
