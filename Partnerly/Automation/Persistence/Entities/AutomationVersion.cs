namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationVersion
    {
        public Guid Id { get; set; }
        public Guid AutomationId { get; set; }
        public int Number { get; set; }
        public string GraphJson { get; set; } = "{}";
        public int GraphSchemaVersion { get; set; } = 1;
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? Note { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
