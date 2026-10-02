using ClosedXML.Excel;
using System.Globalization;
using System.Reflection;

namespace Partnerly.Models.DataTable
{
    public class DataTableExcelExporter
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

            using var workbook =
                new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add(
                    "Data"
                );

            // Header
            for (var i = 0; i < exportColumns.Count; i++)
            {
                var column =
                    exportColumns[i];

                var cell =
                    worksheet.Cell(
                        1,
                        i + 1
                    );

                cell.Value =
                    column.Title;

                cell.Style.Font.Bold = true;
            }

            // Data
            var row = 2;

            foreach (var item in items)
            {
                for (var i = 0; i < exportColumns.Count; i++)
                {
                    var column =
                        exportColumns[i];

                    var value =
                        GetValue(
                            item,
                            column.Field
                        );

                    var formattedValue =
                        FormatValue(
                            value,
                            column
                        );

                    worksheet.Cell(
                        row,
                        i + 1
                    ).Value =
                        formattedValue;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
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
    }
}
