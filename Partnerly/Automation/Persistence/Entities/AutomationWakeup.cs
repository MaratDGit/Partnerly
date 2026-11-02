namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationWakeup
    {
        public Guid Id { get; set; }
        public string Kind { get; set; } = string.Empty;
        public DateTime DueAtUtc { get; set; }
        public Guid? AutomationId { get; set; }
        public Guid? VersionId { get; set; }
        public Guid? TriggerId { get; set; }
        public Guid? RunId { get; set; }
        public string? NodeId { get; set; }
        public string? PayloadJson { get; set; }
        public DateTime? LeaseUntil { get; set; }
        public string? LeaseOwner { get; set; }
        public int Attempt { get; set; }
        public string Status { get; set; } = "pending";
    }
}
