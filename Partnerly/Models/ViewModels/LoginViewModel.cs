using System.ComponentModel.DataAnnotations;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = ErrorMessages.EmailRequired)]
        [EmailAddress()]
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }

        [Required(ErrorMessage = ErrorMessages.PasswordRequired)]
        [DataType(DataType.Password)]
        [Display(Name = FieldsDisplayNames.Password)]
        public string? Password { get; set; }

        [Display(Name = FieldsDisplayNames.RememberMe)]
        public bool RememberMe { get; set; }
    }
}
