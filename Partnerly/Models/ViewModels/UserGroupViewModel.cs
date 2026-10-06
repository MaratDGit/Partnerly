using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.GridViews;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserGroupViewModel : IAuditableViewModel
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
        #endregion

        #region Audit fields
        [Display(Name = nameof(FieldsDisplayNames.CreatorName), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatedByUserName { get; set; }
        public Guid? CreatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.CreatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdaterName), ResourceType = typeof(FieldsDisplayNames))]
        public string? UpdatedByUserName { get; set; }
        public Guid? UpdatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
