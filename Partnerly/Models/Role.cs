using Partnerly.Descriptors.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models
{
    public class Role
    {
        #region Id
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        #endregion
        #region Name
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }
        #endregion
        #region Type
        [Required]
        [Display(Name = "Type")]
        [RoleType]
        public string Type { get; set; }
        #endregion

        #region System Columns
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        #endregion

        public ICollection<User> Users { get; set; }
    }
}
