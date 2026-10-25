using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.DataTable
{
    public class DataTableColumn
    {
        public string Field { get; set; } = "";
        public bool IsKey { get; set; } = false;

        public string Title { get; set; } = "";

        public DataTableColumnType Type { get; set; } = DataTableColumnType.Text;

        public bool Visible { get; set; } = true;
        public bool Required { get; set; } = false;
        public bool Sortable { get; set; } = true;
        public bool Searchable { get; set; } = true;
        public bool Exportable { get; set; } = true;
        public bool Editable { get; set; } = false;
        public bool Resizable { get; set; } = true;
        public string? Width { get; set; }       
        public string? Format { get; set; }
        public string? UrlTemplate { get; set; }
        public string EmptyText { get; set; } = "—";
        public DataTableAlignment Alignment { get; set; } = DataTableAlignment.Left;
        public string? CssClass { get; set; }
        public List<DataTableConditionalStyle> ConditionalStyles { get; set; }  = new List<DataTableConditionalStyle>();
        public List<DataTableColumnValueMapping> ValueMappings { get; set; } = new();
        public DataTableLookupOptions? Lookup { get; set; }

        /// <summary>
        /// Applied to Editable and UrlTemplate. Empty = no extra check.
        /// </summary>
        public string? Permission { get; set; }
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
        Link,
        Size
    }

    public enum DataTableAlignment
    {
        Left,
        Center,
        Right
    }

    public class DataTableColumnValueMapping
    {
        public string Value { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }

    public class DataTableLookupOptions
    {
        public string Url { get; set; } = string.Empty;
        public string IdField { get; set; } = string.Empty;
        public string RecordIdField { get; set; } = "id";
        public string DisplayField { get; set; } = "name";
        public string? SecondaryField { get; set; }

        public string Title { get; set; } = "Lookup";
        public string SearchPlaceholder { get; set; } = FieldsDisplayNames.DataTableSearch;
        public string SearchParameter { get; set; } = FieldsDisplayNames.DataTableSearch;

        public Dictionary<string, string> ParentKeyParameters { get; set; } = new();

        public List<DataTableLookupMapping> Mappings { get; set; } = new();
        public List<DataTableLookupColumn> Columns { get; set; }
        = new();
    }

    public class DataTableLookupMapping
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
    }

    public class DataTableLookupColumn
    {
        public string Field { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Width { get; set; }

        public bool DisplayAsCheckbox { get; set; }
    }
}
