namespace Partnerly.Models.DataTable
{
    public class DataTableFilterOptions
    {
        public bool Enabled { get; set; } = true;

        public List<DataTableFilter> Items { get; set; } = new List<DataTableFilter>();
    }
}
