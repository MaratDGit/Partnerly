namespace Partnerly.Models.DataTable
{
    public class DataTableActionOptions
    {
        public bool Enabled { get; set; } = true;

        public List<DataTableAction> Items { get; set; }
            = new List<DataTableAction>();
    }

    public class DataTableAction
    {
        public string Name { get; set; } = "";
        public string? Text { get; set; } = "";

        public string Title { get; set; } = "";

        public string Icon { get; set; } = "";

        public DataTableActionType Type { get; set; }
            = DataTableActionType.Link;

        public DataTableActionPlacement Placement { get; set; }
            = DataTableActionPlacement.Row;

        public string? Url { get; set; }

        public string Method { get; set; } = "POST";

        public bool Confirm { get; set; } = false;

        public string? ConfirmMessage { get; set; }

        public string? CssClass { get; set; }

        public bool RequiresSelection { get; set; } = false;

        /// <summary>
        /// Permission code such as users.edit. Empty = visible to anyone who can open the table.
        /// </summary>
        public string? Permission { get; set; }
    }


    public enum DataTableActionType
    {
        Link,
        Ajax,
        InlineEdit,
        InlineDelete,
        InlineAdd
    }


    public enum DataTableActionPlacement
    {
        Toolbar,
        Row
    }
}