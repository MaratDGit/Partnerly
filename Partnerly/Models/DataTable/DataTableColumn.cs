using Partnerly.Models.GridViews;

namespace Partnerly.Models.DataTable
{
    public class DataTableColumn
    {
        public string Field { get; set; } = "";

        public string Title { get; set; } = "";

        public DataTableColumnType Type { get; set; } = DataTableColumnType.Text;

        public bool Visible { get; set; } = true;

        public bool Sortable { get; set; } = true;

        public bool Searchable { get; set; } = true;

        public bool Exportable { get; set; } = true;

        public bool Editable { get; set; } = false;

        public string? Width { get; set; }

        public string? Format { get; set; }
        public string? UrlTemplate { get; set; }

        public string EmptyText { get; set; } = "—";

        public DataTableAlignment Alignment { get; set; } = DataTableAlignment.Left;

        public string? CssClass { get; set; }

        public List<DataTableConditionalStyle> ConditionalStyles { get; set; }  = new List<DataTableConditionalStyle>();

    }

    public enum DataTableColumnType
    {
        Text,
        Number,
        Date,
        DateTime,
        Currency,
        Boolean,
        Badge,
        Email,
        Link
    }

    public enum DataTableAlignment
    {
        Left,
        Center,
        Right
    }
}
