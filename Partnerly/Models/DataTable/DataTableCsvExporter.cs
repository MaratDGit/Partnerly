using System.Globalization;
using System.Reflection;
using System.Text;

namespace Partnerly.Models.DataTable
{
    public class DataTableCsvExporter
    {
        public byte[] Export<T>(
            IEnumerable<T> items,
            IEnumerable<DataTableColumn> columns)
        {
            var exportColumns =
                columns
                    .Where(x =>
                        x.Visible &&
                        x.Exportable)
                    .ToList();

            var builder =
                new StringBuilder();

            // Header
            builder.AppendLine(
                string.Join(
                    ",",
                    exportColumns.Select(
                        x => Escape(x.Title)
                    )
                )
            );


            foreach (var item in items)
            {
                var values =
                    exportColumns.Select(
                        column =>
                        {
                            var value =
                                GetValue(
                                    item,
                                    column.Field
                                );

                            return Escape(
                                FormatValue(
                                    value,
                                    column
                                )
                            );
                        }
                    );

                builder.AppendLine(
                    string.Join(",", values)
                );
            }


            return new UTF8Encoding(
                true
            ).GetBytes(
                builder.ToString()
            );
        }


        private object? GetValue<T>(
            T item,
            string field)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(field))
            {
                return null;
            }

            object? current =
                item;

            var parts =
                field.Split(
                    '.',
                    StringSplitOptions.RemoveEmptyEntries
                );


            foreach (var part in parts)
            {
                if (current == null)
                {
                    return null;
                }

                var property =
                    current.GetType()
                        .GetProperty(
                            part,
                            BindingFlags.Public |
                            BindingFlags.Instance |
                            BindingFlags.IgnoreCase
                        );

                if (property == null)
                {
                    return null;
                }

                current =
                    property.GetValue(
                        current
                    );
            }

            return current;
        }


        private string FormatValue(
            object? value,
            DataTableColumn column)
        {
            if (value == null)
            {
                return column.EmptyText;
            }

            if (value is DateTime dateTime)
            {
                if (!string.IsNullOrWhiteSpace(
                    column.Format))
                {
                    return dateTime.ToString(
                        column.Format,
                        CultureInfo.InvariantCulture
                    );
                }

                return dateTime.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }

            if (value is decimal decimalValue)
            {
                return decimalValue.ToString(
                    CultureInfo.InvariantCulture
                );
            }

            if (value is double doubleValue)
            {
                return doubleValue.ToString(
                    CultureInfo.InvariantCulture
                );
            }

            if (value is float floatValue)
            {
                return floatValue.ToString(
                    CultureInfo.InvariantCulture
                );
            }

            if (value is bool boolValue)
            {
                return boolValue
                    ? "Yes"
                    : "No";
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ) ?? "";
        }


        private string Escape(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            value =
                value.Replace(
                    "\"",
                    "\"\""
                );

            if (
                value.Contains(",") ||
                value.Contains("\"") ||
                value.Contains("\r") ||
                value.Contains("\n")
            )
            {
                return "\"" +
                       value +
                       "\"";
            }

            return value;
        }
    }
}
