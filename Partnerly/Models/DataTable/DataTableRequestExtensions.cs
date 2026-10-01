namespace Partnerly.Models.DataTable
{
    public static class DataTableRequestExtensions
    {
        public static DataTableRequest Normalize(
            this DataTableRequest request)
        {
            var page = request.Page < 1
                ? 1
                : request.Page;

            var pageSize = request.PageSize switch
            {
                < 1 => 25,
                > 500 => 500,
                _ => request.PageSize
            };

            var sortDirection =
                string.Equals(
                    request.SortDirection,
                    "desc",
                    StringComparison.OrdinalIgnoreCase)
                    ? "desc"
                    : "asc";

            return new DataTableRequest
            {
                Page = page,
                PageSize = pageSize,
                Search = request.Search?.Trim(),
                SortField = request.SortField?.Trim(),
                SortDirection = sortDirection,
                Filters = request.Filters
            };
        }
    }
}
