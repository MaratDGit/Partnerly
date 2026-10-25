using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.Authorization
{
    public class Permission
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();

        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
    }
}
