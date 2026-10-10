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

        #region Unbound fields
        [NotMapped]
        public bool Selected { get; set; }
        #endregion

        #region System Columns
        public string? Note { get; set; }
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

    public class UserGroupMemberExt : UserGroupMember
    {
        public string? UserEmail { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? GroupName { get; set; }
    }
}
