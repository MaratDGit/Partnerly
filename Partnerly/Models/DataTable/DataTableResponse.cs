namespace Partnerly.Models.DataTable
{
    public sealed class DataTableResponse<T>
    {
        public IReadOnlyList<T> Items { get; init; }

        public int Total { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }
    }
}
