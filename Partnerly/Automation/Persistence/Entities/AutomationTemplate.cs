namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationTemplate
    {
        public Guid Id { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string GraphJson { get; set; } = "{}";
        public bool IsDeleted { get; set; }
        public string? Note { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
