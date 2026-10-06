using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class EmailTemplateViewModel : IAuditableViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [EmailTemplateName]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.TemplateType), ResourceType = typeof(FieldsDisplayNames))]
        public string? Name { get; set; }
        public string? NameView { get => AttributeDropdownHelper.GetValue<EmailTemplateNameAttribute>(Name) ?? Name; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Subject), ResourceType = typeof(FieldsDisplayNames))]
        public string? Subject { get; set; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.BodyHTML), ResourceType = typeof(FieldsDisplayNames))]
        public string? BodyHtml { get; set; }

        public string? BodyPlain { get; set; }
        public string? AttachmentsMeta { get; set; }
        public string? Note { get; set; }

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
