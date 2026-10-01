namespace Partnerly.Models.DataTable
{
    public static class DataTableFilterExtensions
    {
        public static bool TryGetFilter(
            this DataTableRequest request,
            string field,
            out string[] values)
        {
            values = new string[0];

            if (request.Filters == null)
            {
                return false;
            }

            if (!request.Filters.TryGetValue(field, out var result))
            {
                return false;
            }

            if (result == null || result.Length == 0)
            {
                return false;
            }

            values = result;

            return true;
        }
    }
}
