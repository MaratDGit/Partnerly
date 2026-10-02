namespace Partnerly.Models.DataTable
{
    public class DataTableConditionalStyle
    {
        public List<DataTableConditionalCondition> Conditions { get; set; }
            = new List<DataTableConditionalCondition>();

        public string? CssClass { get; set; }

        public string? Style { get; set; }
    }

    public class DataTableConditionalCondition
    {
        public DataTableConditionalOperator Operator { get; set; }

        public string Value { get; set; } = "";
    }

    public enum DataTableConditionalOperator
    {
        Equals,
        NotEquals,

        GreaterThan,
        GreaterThanOrEqual,

        LessThan,
        LessThanOrEqual,

        Contains,
        StartsWith,
        EndsWith
    }
}
