namespace Partnerly.Automation.Persistence.Entities
{
    // "Data store" of one agent: a key / value that survives between runs. Also used by the
    // "Only once" card (key prefix "once:") to remember what was already handled.
    public class AutomationDataItem
    {
        public Guid Id { get; set; }
        public Guid AutomationId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
