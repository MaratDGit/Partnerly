using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class EmailTemplateViewModel
    {
        [Required]
        public Guid Id { get; set; }
        [EmailTemplateName]
        [Required(ErrorMessage = $"{FieldsDisplayNames.TemplateType} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.TemplateType)]
        public string? Name { get; set; }
        [Required(ErrorMessage = $"{FieldsDisplayNames.Subject} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.Subject)]
        public string? Subject { get; set; }
        [Required(ErrorMessage = $"{FieldsDisplayNames.BodyHTML} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.BodyHTML)]
        public string? BodyHtml { get; set; }
        public string? BodyPlain { get; set; }
        public string? AttachmentsMeta { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
