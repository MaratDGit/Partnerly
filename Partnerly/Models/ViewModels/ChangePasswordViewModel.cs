using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        #region Email
        [EmailAddress]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.EmailRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }
        #endregion
        #region Password
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Password), ResourceType = typeof(FieldsDisplayNames))]
        public string? Password { get; set; }
        #endregion

        #region ConfirmPassword
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.CompareConfirmPassword))]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.ConfirmPassword), ResourceType = typeof(FieldsDisplayNames))]
        public string? ConfirmPassword { get; set; }
        #endregion
    }
}
