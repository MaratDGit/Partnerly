using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.Authorization
{
    public class AccessControlAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? PerformedByUserId { get; set; }

        public Guid? TargetUserId { get; set; }

        public Guid? TargetRoleId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? PermissionCode { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    }

    public class AccessControlAuditLogExt
    {
        public Guid Id { get; set; }

        public Guid? TargetUserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? PermissionCode { get; set; }

        public string? Details { get; set; }

        public DateTime CreatedDateUtc { get; set; }

        public string? PerformedByUserName { get; set; }
    }
}
