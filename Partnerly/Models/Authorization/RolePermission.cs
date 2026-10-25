using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.Authorization
{
    public class RolePermission
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RoleId { get; set; }

        [Required]
        public Guid PermissionId { get; set; }

        public Role? Role { get; set; }

        public Permission? Permission { get; set; }
    }

    public class RolePermissionExt : RolePermission
    {
        public string RoleName { get; set; }
        public string Module { get; set; }
        public string PermissionCode { get; set; }
        public string PermissionName { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

    }
}
