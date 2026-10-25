using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.Authorization
{
    public class UserPermission
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid PermissionId { get; set; }

        public bool IsGranted { get; set; }

        public User? User { get; set; }

        public Permission? Permission { get; set; }
    }
}
