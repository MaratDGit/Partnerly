using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class MySettingsViewModel
    {
        #region ID
        [Required]
        public Guid Id { get; set; }
        #endregion

        #region FirstName
        [CusromStringLenght(min: 4, max: 20)]
        [Required(ErrorMessage = $"{FieldsDisplayNames.FirstName} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.FirstName)]
        public string? FirstName { get; set; }
        #endregion

        #region LastName
        [CusromStringLenght(min: 4, max: 20)]
        [Required(ErrorMessage = $"{FieldsDisplayNames.LastName} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.LastName)]
        public string? LastName { get; set; }
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

        #region PhotoUrl
        public string? PhotoUrl { get; set; }
        #endregion

        #region Balance
        [Display(Name = FieldsDisplayNames.Balance)]
        public decimal? Balance { get; set; }
        #endregion

        #region RoleId
        [Display(Name = FieldsDisplayNames.Role)]
        public Guid? RoleId { get; set; }
        #endregion

        #region MyReferralCode
        [Required]
        [Display(Name = FieldsDisplayNames.ReferrerCode)]
        public string? MyReferralCode { get; set; }
        #endregion

        #region ReferrerId
        [Display(Name = "Referrer")]
        public Guid? ReferrerId { get; set; }
        #endregion

        #region LastActivity
        [Display(Name = "Last Activity")]
        public DateTime? LastActivity { get; set; }
        #endregion

        #region IsOnlayn
        [Display(Name = "Is Onlayn")]
        public bool? IsOnlayn { get; set; }
        #endregion

        #region IsBlocked
        [Display(Name = FieldsDisplayNames.IsBlocked)]
        public bool? IsBlocked { get; set; }
        public bool IsBlockedBool { get => IsBlocked ?? false; set => IsBlocked = value; }
        #endregion

        #region EmailConfirmed
        [Display(Name = FieldsDisplayNames.EmailConfirmed)]
        public bool? EmailConfirmed { get; set; }
        public bool EmailConfirmedBool { get => EmailConfirmed ?? false; set => EmailConfirmed = value; }
        #endregion

        #region AllowSendNotifications
        [Display(Name = FieldsDisplayNames.AllowSendNotifications)]
        public bool? AllowSendNotifications { get; set; }
        public bool AllowSendNotificationsBool { get => AllowSendNotifications ?? false; set => AllowSendNotifications = value; }
        #endregion
        #region AllowSendEmails
        [Display(Name = FieldsDisplayNames.AllowSendEmails)]
        public bool? AllowSendEmails { get; set; }
        public bool AllowSendEmailsBool { get => AllowSendEmails ?? false; set => AllowSendEmails = value; }
        #endregion

        #region OldPassword
        [Required(ErrorMessage = ErrorMessages.PasswordRequired)]
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Display(Name = FieldsDisplayNames.Password)]
        public string? OldPassword { get; set; }
        #endregion

        #region Password
        [Required(ErrorMessage = ErrorMessages.PasswordRequired)]
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Display(Name = FieldsDisplayNames.NewPassword)]
        public string? Password { get; set; }
        #endregion

        #region ConfirmPassword
        [Required(ErrorMessage = $"{FieldsDisplayNames.ConfirmPassword} {ErrorMessages.FieldRequired}")]
        [DataType(DataType.Password)]
        [Display(Name = FieldsDisplayNames.ConfirmPassword)]
        [Compare("Password", ErrorMessage = ErrorMessages.CompareConfirmPassword)]
        public string? ConfirmPassword { get; set; }
        #endregion

        public string? ActiveTab { get; set; }

        #region System Columns
        public bool IsDeleted { get; set; } = false;
        //[Required]
        public Guid? CreatedBy { get; set; }
        //[Required]
        public DateTime? CreatedDate { get; set; }
        //[Required]
        public Guid? UpdatedBy { get; set; }
        //[Required]
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
