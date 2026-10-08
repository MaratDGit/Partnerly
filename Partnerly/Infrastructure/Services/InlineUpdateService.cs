using Microsoft.AspNetCore.Mvc;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.Globalization;
using System.Reflection;

namespace Partnerly.Infrastructure.Services
{
    public class InlineUpdateService : IInlineUpdateService
    {
        public InlineUpdateResult ApplyChanges<T>(
            T entity,
            IDictionary<string, string?> fields,
            DataTableOptions tableOptions)
        {
            if (entity == null)
            {
                return InlineUpdateResult.Error(
                    "Entity cannot be null.");
            }

            if (tableOptions == null)
            {
                return InlineUpdateResult.Error(
                    "DataTable options cannot be null.");
            }

            if (fields == null || fields.Count == 0)
            {
                return InlineUpdateResult.NotChanged();
            }

            /*
             * Get only columns which are editable.
             */
            var editableColumns = tableOptions.Columns
                .Where(x => x.Editable)
                .ToDictionary(
                    x => x.Field,
                    StringComparer.OrdinalIgnoreCase);

            /*
             * Get entity properties.
             */
            var properties = typeof(T)
                .GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .ToDictionary(
                    x => x.Name,
                    StringComparer.OrdinalIgnoreCase);

            var isChanged = false;

            foreach (var field in fields)
            {
                /*
                 * Field must exist in DataTable.
                 */
                if (!editableColumns.TryGetValue(
                        field.Key,
                        out var column))
                {
                    return InlineUpdateResult.Error(
                        $"Field is not editable: {field.Key}");
                }

                /*
                 * Field must exist on entity.
                 */
                if (!properties.TryGetValue(
                        field.Key,
                        out var property))
                {
                    return InlineUpdateResult.Error(
                        $"Field does not exist on entity: {field.Key}");
                }

                /*
                 * Required validation.
                 */
                if (column.Required &&
                    string.IsNullOrWhiteSpace(field.Value))
                {
                    return InlineUpdateResult.Error(
                        $"{GetFieldDisplayName(column)} is required.");
                }

                object? newValue;

                try
                {
                    newValue = ConvertValue(
                        field.Value,
                        property.PropertyType);
                }
                catch (Exception ex)
                {
                    return InlineUpdateResult.Error(
                        $"Invalid value for field " +
                        $"'{GetFieldDisplayName(column)}': {ex.Message}");
                }

                /*
                 * Additional protection:
                 *
                 * Required value must not become null.
                 */
                if (column.Required &&
                    newValue == null)
                {
                    return InlineUpdateResult.Error(
                        $"{GetFieldDisplayName(column)} is required.");
                }

                /*
                 * Compare old and new value.
                 */
                var currentValue =
                    property.GetValue(entity);

                if (Equals(currentValue, newValue))
                {
                    continue;
                }

                /*
                 * Apply change.
                 */
                property.SetValue(
                    entity,
                    newValue);

                isChanged = true;
            }

            return isChanged
                ? InlineUpdateResult.Changed()
                : InlineUpdateResult.NotChanged();
        }

        private static string GetFieldDisplayName(
            DataTableColumn column)
        {
            if (!string.IsNullOrWhiteSpace(column.Title))
            {
                return column.Title;
            }

            return column.Field;
        }

        private static object? ConvertValue(
            string? value,
            Type targetType)
        {
            var underlyingType =
                Nullable.GetUnderlyingType(targetType);

            var actualType =
                underlyingType ?? targetType;

            /*
             * STRING
             */
            if (actualType == typeof(string))
            {
                return value ?? string.Empty;
            }

            /*
             * NULL / EMPTY
             */
            if (string.IsNullOrWhiteSpace(value))
            {
                if (underlyingType != null)
                {
                    return null;
                }

                /*
                 * Non-nullable value types cannot accept empty.
                 */
                if (actualType.IsValueType)
                {
                    throw new FormatException(
                        "A value is required.");
                }

                return null;
            }

            /*
             * GUID
             */
            if (actualType == typeof(Guid))
            {
                if (Guid.TryParse(value, out var guid))
                {
                    return guid;
                }

                throw new FormatException(
                    $"'{value}' is not a valid GUID.");
            }

            /*
             * BOOLEAN
             */
            if (actualType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    return boolValue;
                }

                if (value == "1")
                {
                    return true;
                }

                if (value == "0")
                {
                    return false;
                }

                throw new FormatException(
                    $"'{value}' is not a valid boolean value.");
            }

            /*
             * ENUM
             */
            if (actualType.IsEnum)
            {
                if (Enum.TryParse(
                        actualType,
                        value,
                        true,
                        out var enumValue))
                {
                    return enumValue;
                }

                throw new FormatException(
                    $"'{value}' is not a valid " +
                    $"{actualType.Name} value.");
            }

            /*
             * DATE / TIME
             */
            if (actualType == typeof(DateTime))
            {
                if (DateTime.TryParse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dateTime))
                {
                    return dateTime;
                }

                throw new FormatException(
                    $"'{value}' is not a valid date.");
            }

            /*
             * DATE TIME OFFSET
             */
            if (actualType == typeof(DateTimeOffset))
            {
                if (DateTimeOffset.TryParse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dateTimeOffset))
                {
                    return dateTimeOffset;
                }

                throw new FormatException(
                    $"'{value}' is not a valid date.");
            }

            /*
             * INTEGER
             */
            if (actualType == typeof(int))
            {
                if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var intValue))
                {
                    return intValue;
                }

                throw new FormatException(
                    $"'{value}' is not a valid integer.");
            }

            /*
             * LONG
             */
            if (actualType == typeof(long))
            {
                if (long.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var longValue))
                {
                    return longValue;
                }

                throw new FormatException(
                    $"'{value}' is not a valid number.");
            }

            /*
             * DECIMAL
             */
            if (actualType == typeof(decimal))
            {
                if (decimal.TryParse(
                        value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var decimalValue))
                {
                    return decimalValue;
                }

                throw new FormatException(
                    $"'{value}' is not a valid decimal.");
            }

            /*
             * DOUBLE
             */
            if (actualType == typeof(double))
            {
                if (double.TryParse(
                        value,
                        NumberStyles.Float |
                        NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out var doubleValue))
                {
                    return doubleValue;
                }

                throw new FormatException(
                    $"'{value}' is not a valid number.");
            }

            /*
             * FLOAT
             */
            if (actualType == typeof(float))
            {
                if (float.TryParse(
                        value,
                        NumberStyles.Float |
                        NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out var floatValue))
                {
                    return floatValue;
                }

                throw new FormatException(
                    $"'{value}' is not a valid number.");
            }

            /*
             * FALLBACK
             */
            return Convert.ChangeType(
                value,
                actualType,
                CultureInfo.InvariantCulture);
        }
    }
}
