using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.DataTable
{
    public class DataTableSearchOptions
    {
        public bool Enabled { get; set; } = true;
        public string Placeholder { get; set; } = FieldsDisplayNames.DataTableSearch;
        public int MinLength { get; set; } = 3;
        public int DebounceMs { get; set; } = 300;
    }
}
