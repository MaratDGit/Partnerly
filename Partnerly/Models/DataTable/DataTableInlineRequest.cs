namespace Partnerly.Models.DataTable
{
    public sealed class DataTableInlineUpdateRequest
    {
        public string Id { get; set; } = "";

        public Dictionary<string, string?> Fields { get; set; }
            = new Dictionary<string, string?>();
    }

    public class DataTableInlineCreateRequest
    {
        public Dictionary<string, string?> Fields { get; set; }
            = new();
    }

    public class DataTableInlineDeleteRequest
    {
        public string Id { get; set; } = "";
    }
}
