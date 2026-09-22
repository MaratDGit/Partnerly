using System.ComponentModel.DataAnnotations;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.ViewModels
{
    public class LoginViewModel
    {
        [EmailAddress()]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.EmailRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Password), ResourceType = typeof(FieldsDisplayNames))]
        public string? Password { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.RememberMe), ResourceType = typeof(FieldsDisplayNames))]
        public bool RememberMe { get; set; }
    }
}
