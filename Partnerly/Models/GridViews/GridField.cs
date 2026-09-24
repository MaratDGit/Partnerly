namespace Partnerly.Models.GridViews
{
    public class GridField
    {
        public string? FieldName { get; set; }
        public string? DisplayName { get; set; }
        public bool IsSortable { get; set; } = true;
        public bool IsFilterable { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        public string? Attr { get; set; }
        public string? LinkTemplate { get; set; }
        public string? Format { get; set; }
        public object? DefaultValue { get; set; }
        public string? Type { get; set; }
        public string? Data { get; set; }

        public string? CssClass { get; set; }
        public string? Style { get; set; }
        public Dictionary<string, GridValueStyle>? ValueStyles { get; set; }
    }

    public class GridValueStyle
    {
        public string? CssClass { get; set; }
        public string? Style { get; set; }
    }
}
