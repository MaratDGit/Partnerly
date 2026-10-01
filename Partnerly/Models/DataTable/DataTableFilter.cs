namespace Partnerly.Models.DataTable
{
    public class DataTableFilter
    {
        public string Field { get; set; } = "";

        public string Label { get; set; } = "";

        public DataTableFilterType Type { get; set; }

        public string? Placeholder { get; set; }

        public string? DefaultValue { get; set; }

        public List<DataTableFilterOption> Options { get; set; } = new List<DataTableFilterOption>();
    }

    public class DataTableFilterOption
    {
        public string Value { get; set; } = "";

        public string Label { get; set; } = "";

        public DataTableFilterOption()
        {
        }

        public DataTableFilterOption(string value, string label)
        {
            Value = value;
            Label = label;
        }
    }

    public enum DataTableFilterType
    {
        Text,
        Select,
        MultiSelect,
        Date,
        DateRange,
        Number,
        NumberRange,
        Boolean
    }
}
