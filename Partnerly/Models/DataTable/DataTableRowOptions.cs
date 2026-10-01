namespace Partnerly.Models.DataTable
{
    public class DataTableRowOptions
    {
        public bool Selectable { get; set; }

        public bool Clickable { get; set; }

        public bool Hover { get; set; } = true;

        public bool Striped { get; set; } = true;
    }
}
