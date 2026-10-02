using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Partnerly.Models.DataTable
{
    public sealed class DataTablePropertyResolver
    {
        public bool TryResolve<T>(
            ParameterExpression parameter,
            string field,
            out Expression propertyExpression,
            out Type propertyType)
        {
            propertyExpression = parameter;
            propertyType = typeof(T);

            if (string.IsNullOrWhiteSpace(field))
            {
                return false;
            }

            Expression currentExpression = parameter;
            Type currentType = typeof(T);

            var parts = field.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var property = currentType.GetProperty(
                    part,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

                if (property == null)
                {
                    return false;
                }

                var notMapped =
                    property.GetCustomAttributes(
                        typeof(NotMappedAttribute),
                        true)
                    .Any();

                if (notMapped)
                {
                    return false;
                }

                currentExpression = Expression.Property(
                    currentExpression,
                    property);

                currentType = property.PropertyType;
            }

            propertyExpression = currentExpression;
            propertyType = currentType;

            return true;
        }
    }
}
