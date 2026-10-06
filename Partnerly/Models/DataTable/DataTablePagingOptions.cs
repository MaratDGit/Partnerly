namespace Partnerly.Models.DataTable
{
    public class DataTablePagingOptions
    {
        public bool Enabled { get; set; } = true;

        public int PageSize { get; set; } = 14;

        public int[] PageSizeOptions { get; set; } = new[] { 14, 25, 50, 100 };

        public bool ShowInfo { get; set; } = true;

        public bool ShowPageSize { get; set; } = true;
    }
}
