using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        #region Email
        [Required(ErrorMessage = ErrorMessages.EmailRequired)]
        [EmailAddress]
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }
        #endregion
        #region Password
        [Required(ErrorMessage = ErrorMessages.PasswordRequired)]
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Display(Name = FieldsDisplayNames.Password)]
        public string? Password { get; set; }
        #endregion

        #region ConfirmPassword
        [Required(ErrorMessage = $"{FieldsDisplayNames.ConfirmPassword} {ErrorMessages.FieldRequired}")]
        [DataType(DataType.Password)]
        [Display(Name = FieldsDisplayNames.ConfirmPassword)]
        [Compare("Password", ErrorMessage = ErrorMessages.CompareConfirmPassword)]
        public string? ConfirmPassword { get; set; }
        #endregion
    }
}
