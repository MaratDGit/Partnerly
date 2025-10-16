using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Partnerly.Models
{
    public class UserGroupMember : IAuditableEntity
    {
        [Key, Column(Order = 0)]
        public Guid UserId { get; set; }
        [Key, Column(Order = 1)]
        public Guid GroupId { get; set; }
        
        #region System Columns
        public bool IsDeleted { get; set; } = false;
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime? CreatedDate { get; set; }
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
        [ForeignKey(nameof(GroupId))]
        public UserGroup Group { get; set; } = null!;
    }
}
