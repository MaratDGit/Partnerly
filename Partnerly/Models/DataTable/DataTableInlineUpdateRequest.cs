namespace Partnerly.Models.DataTable
{
    public sealed class DataTableInlineUpdateRequest
    {
        public string Id { get; set; } = "";

        public Dictionary<string, string?> Fields { get; set; }
            = new Dictionary<string, string?>();
    }
}
