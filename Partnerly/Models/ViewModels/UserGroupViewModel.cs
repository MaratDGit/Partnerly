using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Models.GridViews;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserGroupViewModel
    {
        public Guid Id { get; set; }

        [CusromStringLenght(min: 2, max: 15)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Name), ResourceType = typeof(FieldsDisplayNames))]
        public string? Name { get; set; }

        [CusromStringLenght(max: 100)]
        [Display(Name = nameof(FieldsDisplayNames.Description), ResourceType = typeof(FieldsDisplayNames))]
        public string? Description { get; set; }

        public List<string> MembersGuids { get; set; } = new List<string>();

        #region System Columns
        public string? Note { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
