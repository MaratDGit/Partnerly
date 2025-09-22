using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = ErrorMessages.EmailRequired)]
        [EmailAddress()]
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }
    }
}
