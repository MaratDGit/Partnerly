using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class GoogleRegisterViewModel
    {
        public string GoogleId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string Email { get; set; } = string.Empty;

        [Display(Name = nameof(FieldsDisplayNames.FirstName), ResourceType = typeof(FieldsDisplayNames))]
        public string? FirstName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.LastName), ResourceType = typeof(FieldsDisplayNames))]
        public string? LastName { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
