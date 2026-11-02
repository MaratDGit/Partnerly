namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationDefinition
    {
        public Guid Id { get; set; }
        public Guid OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public Guid? PublishedVersionId { get; set; }
        public Guid? DraftVersionId { get; set; }
        public string TimeZoneId { get; set; } = "Asia/Yerevan";
        public int MaxConcurrentRuns { get; set; } = 1;
        public int MaxRunsPerHour { get; set; } = 60;
        public int StepTimeoutSeconds { get; set; } = 60;
        public int RunTimeoutSeconds { get; set; } = 3600;
        public int RetryLimit { get; set; } = 3;
        public string? Note { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
