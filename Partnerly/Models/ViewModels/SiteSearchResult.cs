namespace Partnerly.Models.ViewModels
{
    public class SiteSearchResult
    {
        public List<SiteSearchGroup> Groups { get; set; } = new();
    }

    public class SiteSearchGroup
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<SiteSearchItem> Items { get; set; } = new();
    }

    public class SiteSearchItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = "bx bx-search";
    }
}
