namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationTrigger
    {
        public Guid Id { get; set; }
        public Guid VersionId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string ConfigJson { get; set; } = "{}";
        public string? WebhookPublicId { get; set; }
        public string? WebhookSecretHash { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
