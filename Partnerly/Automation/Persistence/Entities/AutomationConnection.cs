namespace Partnerly.Automation.Persistence.Entities
{
    public class AutomationConnection
    {
        public Guid Id { get; set; }
        public Guid OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "http";
        public string EncryptedPayload { get; set; } = "{}";
        public bool IsDeleted { get; set; }
        public string? Note { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
