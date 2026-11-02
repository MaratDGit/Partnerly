namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationRun
    {
        public Guid Id { get; set; }
        public Guid AutomationId { get; set; }
        public Guid VersionId { get; set; }
        public Guid OwnerUserId { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public string? TriggerReason { get; set; }
        public string Status { get; set; } = "queued";
        public bool IsDryRun { get; set; }
        public int Depth { get; set; }
        public string? IdempotencyKey { get; set; }
        public string ContextJson { get; set; } = "{}";
        public string ChosenPortsJson { get; set; } = "{}";
        public string CompletedNodeIdsJson { get; set; } = "[]";
        public string? Error { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime? HeartbeatAt { get; set; }
        public Guid? CancelledBy { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
