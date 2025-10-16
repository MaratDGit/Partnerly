using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserGroupViewModel
    {
        public Guid Id { get; set; }

        [StringLenght(min: 2, max: 15)]
        [Required(ErrorMessage = $"{FieldsDisplayNames.Name} {ErrorMessages.FieldRequired}")]
        //[Unique(typeof(UserGroup), nameof(UserGroup.Name), ErrorMessage = $"{FieldsDisplayNames.Group} {ErrorMessages.UniqueValue}")]
        [Display(Name = FieldsDisplayNames.Name)]
        public string? Name { get; set; }

        [StringLenght(max: 100)]
        [Display(Name = FieldsDisplayNames.Description)]
        public string? Description { get; set; }

        public ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();

        #region System Columns
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
