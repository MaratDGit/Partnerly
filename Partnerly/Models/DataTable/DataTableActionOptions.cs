namespace Partnerly.Models.DataTable
{
    public class DataTableActionOptions
    {
        public bool Enabled { get; set; } = true;

        public bool Add { get; set; }
        public string? AddUrl { get; set; }

        public bool View { get; set; }

        public bool Edit { get; set; }

        public bool Delete { get; set; }
       

        public string? ViewUrl { get; set; }

        public string? EditUrl { get; set; }

        public string? DeleteUrl { get; set; }

        public bool BulkDelete { get; set; }

        

        public bool InlineAdd { get; set; } = false;
        public bool InlineDelete { get; set; } = false;
    }
}
