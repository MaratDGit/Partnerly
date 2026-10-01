namespace Partnerly.Models.DataTable
{
    public sealed class DataTableRequest
    {
        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 10;

        public string? Search { get; init; }

        public string? SortField { get; init; }

        public string SortDirection { get; init; } = "asc";

        public Dictionary<string, string[]> Filters { get; init; } = new Dictionary<string, string[]>();
    }
}
