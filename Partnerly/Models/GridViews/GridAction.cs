namespace Partnerly.Models.GridViews
{
    public class GridAction
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? UrlTemplate { get; set; }
        public string CssClass { get; set; } = "dropdown-item";
        public string? Icon { get; set; }
        public string? Attr { get; set; }
        public bool IsVisible { get; set; } = false;
    }
}
