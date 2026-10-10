namespace Partnerly.Models.DataTable
{
    public class DataTableOptions
    {
        public string Id { get; set; } = "dataTable";
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string AjaxUrl { get; set; } = "";
        public string KeyField { get; set; } = "Id";
        public string ExportUrl { get; set; } = "";

        public bool ReloadEnabled { get; set; } = true;
        public bool AutoLoad { get; set; } = true;
        public bool RememberState { get; set; } = true;

        public bool InlineEditing { get; set; } = false;
        public string InlineUpdateUrl { get; set; } = "";

        public bool ColumnResizeEnabled { get; set; } = true;
        public int ColumnResizeMinWidth { get; set; } = 60;

        public List<DataTableColumn> Columns { get; set; } = new List<DataTableColumn>();

        public DataTableSearchOptions Search { get; set; } = new();

        public DataTablePagingOptions Paging { get; set; } = new();

        public DataTableFilterOptions Filters { get; set; } = new();

        public DataTableSortOptions Sorting { get; set; } = new();

        public DataTableExportOptions Export { get; set; } = new();

        public DataTableActionOptions Actions { get; set; } = new();

        public DataTableRowOptions Rows { get; set; } = new();
    }
}