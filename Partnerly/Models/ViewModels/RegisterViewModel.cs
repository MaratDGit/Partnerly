using System.ComponentModel.DataAnnotations;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.ViewModels
{
    public class RegisterViewModel
    {
        #region FirstName
        [StringLenght(min:4, max: 20)]
        [Required(ErrorMessage = $"{FieldsDisplayNames.FirstName} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.FirstName)]
        public string? FirstName { get; set; }
        #endregion
        #region LastName
        [StringLenght(min: 4, max: 20)]
        [Required(ErrorMessage = $"{FieldsDisplayNames.LastName} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.LastName)]
        public string? LastName { get; set; }
        #endregion

        #region ReferrerCode
        [StringLenght(max: 8)]
        [Display(Name = FieldsDisplayNames.ReferrerCode)]
        public string? ReferrerCode { get; set; }
        #endregion

        #region Email
        [Required(ErrorMessage = ErrorMessages.EmailRequired)]
        [EmailAddress]
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [Required(ErrorMessage = $"{FieldsDisplayNames.Phone} {ErrorMessages.FieldRequired}")]
        [ArmenianPhone(ErrorMessage = ErrorMessages.TypeValidPhoneNumber)]
        [Display(Name = FieldsDisplayNames.Phone)]
        public string? Phone { get; set; }
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

        #region TermsConditions
        [Display(Name = FieldsDisplayNames.TermsConditions)]
        public bool TermsConditions { get; set; }
        #endregion
    }
}
