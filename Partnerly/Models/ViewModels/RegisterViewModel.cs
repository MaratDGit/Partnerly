using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.ViewModels
{
    public class RegisterViewModel
    {
        #region FirstName
        [Required(ErrorMessage = $"{FieldsDisplayNames.FirstName} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.FirstName)]
        public string? FirstName { get; set; }
        #endregion
        #region LastName
        [Required(ErrorMessage = $"{FieldsDisplayNames.LastName} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.LastName)]
        public string? LastName { get; set; }
        #endregion

        #region ReferrerCode
        [Display(Name = FieldsDisplayNames.ReferrerCode)]
        public string? ReferrerCode { get; set; }
        #endregion

        [Required(ErrorMessage = ErrorMessages.EmailRequired)]
        [EmailAddress]
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }

        [Required(ErrorMessage = $"{FieldsDisplayNames.Phone} {ErrorMessages.FieldRequired}")]
        [ArmenianPhone(ErrorMessage = ErrorMessages.TypeValidPhoneNumber)]
        [Display(Name = FieldsDisplayNames.Phone)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = ErrorMessages.PasswordRequired)]
        [DataType(DataType.Password)]
        [Display(Name = FieldsDisplayNames.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = $"{FieldsDisplayNames.ConfirmPassword} {ErrorMessages.FieldRequired}")]
        [DataType(DataType.Password)]
        [Display(Name = FieldsDisplayNames.ConfirmPassword)]
        [Compare("Password", ErrorMessage = ErrorMessages.CompareConfirmPassword)]
        public string? ConfirmPassword { get; set; }
    }
}
