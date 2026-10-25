using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Partnerly.Models
{
    public class Role : IAuditableEntity
    {
        #region Id
        [Required]
        public Guid Id { get; set; }
        #endregion
        #region Name
        [Required]
        [Display(Name = "Name")]
        public string? Name { get; set; }
        #endregion

        public bool IsActive { get; set; } = true;

        #region Unbound fields
        [NotMapped]
        public bool Selected { get; set; }
        #endregion

        #region System Columns
        public string? Note { get; set; }
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime? CreatedDate { get; set; }
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        #endregion

        public ICollection<User>? Users { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
