namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationRunStep
    {
        public Guid Id { get; set; }
        public Guid RunId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public int Attempt { get; set; } = 1;
        public string? InputJson { get; set; }
        public string? OutputJson { get; set; }
        public string? Error { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
