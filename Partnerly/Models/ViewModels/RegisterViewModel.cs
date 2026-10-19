using System.ComponentModel.DataAnnotations;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;

namespace Partnerly.Models.ViewModels
{
    public class RegisterViewModel
    {
        #region FullName
        [CustomStringLenght(min: 4, max: 50)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]    
        [Display(Name = nameof(FieldsDisplayNames.FullName), ResourceType = typeof(FieldsDisplayNames))]
        public string? FullName { get; set; }
        #endregion
        #region FirstName
        [CustomStringLenght(min:4, max: 20)]
        //[Required(ErrorMessage = $"{FieldsDisplayNames.FirstName} {ErrorMessages.FieldRequired}")]
        [Display(Name = nameof(FieldsDisplayNames.FirstName), ResourceType = typeof(FieldsDisplayNames))]
        public string? FirstName { get; set; }
        #endregion
        #region LastName
        [CustomStringLenght(min: 4, max: 20)]
        //[Required(ErrorMessage = $"{FieldsDisplayNames.LastName} {ErrorMessages.FieldRequired}")]
        [Display(Name = nameof(FieldsDisplayNames.LastName), ResourceType = typeof(FieldsDisplayNames))]
        public string? LastName { get; set; }
        #endregion

        #region ReferrerCode
        [CustomStringLenght(max: 8)]
        [Display(Name = nameof(FieldsDisplayNames.ReferrerCode), ResourceType = typeof(FieldsDisplayNames))]
        public string? ReferrerCode { get; set; }
        public bool FromReferrerLink { get; set; } = false;
        #endregion

        #region Email
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.EmailRequired))]
        [EmailAddress]
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }
        #endregion

        #region Phone
        //[Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [ArmenianPhone(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.TypeValidPhoneNumber))]
        [Display(Name = nameof(FieldsDisplayNames.Phone), ResourceType = typeof(FieldsDisplayNames))]
        public string? Phone { get; set; }
        #endregion

        #region Password
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Password), ResourceType = typeof(FieldsDisplayNames))]
        public string? Password { get; set; }
        #endregion

        #region ConfirmPassword
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [DataType(DataType.Password)]
        [Display(Name = nameof(FieldsDisplayNames.ConfirmPassword), ResourceType = typeof(FieldsDisplayNames))]
        [Compare("Password", ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.CompareConfirmPassword))]
        public string? ConfirmPassword { get; set; }
        #endregion

        #region TermsConditions
        [Display(Name = nameof(FieldsDisplayNames.TermsConditions), ResourceType = typeof(FieldsDisplayNames))]
        public bool TermsConditions { get; set; }
        #endregion
    }
}
