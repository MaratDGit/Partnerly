using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Web
{
    public class IntegrationsViewModel
    {
        public List<IntegrationCardItem> Telegram { get; set; } = new();
        public List<IntegrationCardItem> Google { get; set; } = new();
        public List<IntegrationCardItem> WhatsApp { get; set; } = new();
        public bool GoogleConfigured { get; set; }
        public string GoogleRedirectUri { get; set; } = string.Empty;
        public string? Flash { get; set; }
        public bool FlashIsError { get; set; }
    }

    // Safe to render: no tokens, only what the user needs to recognise a connection.
    public class IntegrationCardItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public List<string> Badges { get; set; } = new();
        public IntegrationStatus Status { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public string? WebhookUrl { get; set; }
        public string? VerifyToken { get; set; }
        public string? DefaultChatId { get; set; }
    }
}
