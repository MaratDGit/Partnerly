namespace Partnerly.Models.DataTable
{
    public class DataTablePagingOptions
    {
        public bool Enabled { get; set; } = true;

        public int PageSize { get; set; } = 10;

        public int[] PageSizeOptions { get; set; } = new[] { 10, 25, 50, 100 };

        public bool ShowInfo { get; set; } = true;

        public bool ShowPageSize { get; set; } = true;
    }
}
