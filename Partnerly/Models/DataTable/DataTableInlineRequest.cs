namespace Partnerly.Models.DataTable
{
    public sealed class DataTableInlineUpdateRequest
    {
        public string TableID { get; set; } = "";
        public string Id { get; set; } = "";

        public Dictionary<string, string?> Keys { get; set; }
            = new();

        public Dictionary<string, string?> Fields { get; set; }
            = new Dictionary<string, string?>();
    }

    public class DataTableInlineCreateRequest
    {
        public string TableID { get; set; } = "";

        public Dictionary<string, string?> ParentKeys { get; set; } 
            = new();

        public Dictionary<string, string?> Fields { get; set; }
            = new();
    }

    public class DataTableInlineDeleteRequest
    {
        public string TableID { get; set; } = "";
        public string Id { get; set; } = "";

        public Dictionary<string, string?> Keys { get; set; }
            = new();
    }
}
