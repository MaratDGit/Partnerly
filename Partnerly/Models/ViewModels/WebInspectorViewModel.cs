namespace Partnerly.Models.ViewModels
{
    public class WebInspectRequest
    {
        public string? Url { get; set; }
    }

    public class WebPageInspectResult
    {
        public const string ErrorInvalidUrl = "InvalidUrl";
        public const string ErrorBlockedUrl = "BlockedUrl";
        public const string ErrorTimeout = "Timeout";
        public const string ErrorTooLarge = "TooLarge";
        public const string ErrorFetchFailed = "FetchFailed";

        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? RequestedUrl { get; set; }
        public string? FinalUrl { get; set; }
        public int StatusCode { get; set; }
        public string? ContentType { get; set; }
        public int ByteCount { get; set; }
        public string Html { get; set; } = string.Empty;
        public WebPageAnalysis Analysis { get; set; } = new();
    }

    public class WebPageAnalysis
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VisibleText { get; set; } = string.Empty;
        public List<string> Headings { get; set; } = new();
        public List<WebPageLinkItem> Links { get; set; } = new();
        public List<WebPageImageItem> Images { get; set; } = new();
        public List<WebPageFormItem> Forms { get; set; } = new();
        public List<WebPageTableItem> Tables { get; set; } = new();
        public List<string> Meta { get; set; } = new();
    }

    public class WebPageLinkItem
    {
        public string Href { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class WebPageImageItem
    {
        public string Src { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
    }

    public class WebPageFormItem
    {
        public string Action { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
    }

    public class WebPageTableItem
    {
        public List<string> Headers { get; set; } = new();
        public int RowCount { get; set; }
        public List<List<string>> SampleRows { get; set; } = new();
    }
}
