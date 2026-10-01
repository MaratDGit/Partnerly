namespace Partnerly.Models.DataTable
{
    public class DataTableSortOptions
    {
        public bool Enabled { get; set; } = true;

        public string? DefaultField { get; set; }

        public string DefaultDirection { get; set; } = "asc";
    }
}
