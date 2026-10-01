namespace Partnerly.Models.DataTable
{
    public class DataTableSearchOptions
    {
        public bool Enabled { get; set; } = true;

        public string Placeholder { get; set; } = "Search...";

        public int MinLength { get; set; } = 0;

        public int DebounceMs { get; set; } = 300;
    }
}
