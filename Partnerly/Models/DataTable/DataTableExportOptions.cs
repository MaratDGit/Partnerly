namespace Partnerly.Models.DataTable
{
    public class DataTableExportOptions
    {
        public bool Enabled { get; set; } = true;

        public bool Csv { get; set; } = true;

        public bool Excel { get; set; } = true;

        public bool Print { get; set; } = false;
    }
}
