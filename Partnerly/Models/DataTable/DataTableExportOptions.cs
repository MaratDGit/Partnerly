namespace Partnerly.Models.DataTable
{
    public class DataTableExportOptions
    {
        public bool Enabled { get; set; } = false;

        public bool Csv { get; set; } = true;

        public bool Excel { get; set; } = false;

        public bool Print { get; set; } = false;
    }
}
