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
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.FirstName), ResourceType = typeof(FieldsDisplayNames))]
        public string? FirstName { get; set; }
        #endregion

        #region LastName
        [CusromStringLenght(min: 4, max: 20)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.LastName), ResourceType = typeof(FieldsDisplayNames))]
        public string? LastName { get; set; }
        #endregion
        #region Email
        [EmailAddress]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.EmailRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [ArmenianPhone(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.TypeValidPhoneNumber))]
        //[Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Phone), ResourceType = typeof(FieldsDisplayNames))]
        public string? Phone { get; set; }
        #endregion

        #region PhotoUrl
        public string? PhotoUrl { get; set; }
        #endregion

        #region Balance
        [Display(Name = nameof(FieldsDisplayNames.Balance), ResourceType = typeof(FieldsDisplayNames))]
        public decimal? Balance { get; set; }
        #endregion

        #region RoleId
        [Display(Name = nameof(FieldsDisplayNames.Role), ResourceType = typeof(FieldsDisplayNames))]
        public Guid? RoleId { get; set; }
        #endregion

        #region MyReferralCode
        [Required]
        [Display(Name = nameof(FieldsDisplayNames.ReferrerCode), ResourceType = typeof(FieldsDisplayNames))]
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
        [Display(Name = nameof(FieldsDisplayNames.IsBlocked), ResourceType = typeof(FieldsDisplayNames))]
        public bool? IsBlocked { get; set; }
        public bool IsBlockedBool { get => IsBlocked ?? false; set => IsBlocked = value; }
        #endregion

        #region EmailConfirmed
        [Display(Name = nameof(FieldsDisplayNames.EmailConfirmed), ResourceType = typeof(FieldsDisplayNames))]
        public bool? EmailConfirmed { get; set; }
        public bool EmailConfirmedBool { get => EmailConfirmed ?? false; set => EmailConfirmed = value; }
        #endregion

        #region AllowSendNotifications
        [Display(Name = nameof(FieldsDisplayNames.AllowSendNotifications), ResourceType = typeof(FieldsDisplayNames))]
        public bool? AllowSendNotifications { get; set; }
        public bool AllowSendNotificationsBool { get => AllowSendNotifications ?? false; set => AllowSendNotifications = value; }
        #endregion
        #region AllowSendEmails
        [Display(Name = nameof(FieldsDisplayNames.AllowSendEmails), ResourceType = typeof(FieldsDisplayNames))]
        public bool? AllowSendEmails { get; set; }
        public bool AllowSendEmailsBool { get => AllowSendEmails ?? false; set => AllowSendEmails = value; }
        #endregion

        #region OldPassword
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Password), ResourceType = typeof(FieldsDisplayNames))]
        public string? OldPassword { get; set; }
        #endregion

        #region Password
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordRequired))]
        [DataType(DataType.Password)]
        [PasswordValidation]
        [Display(Name = nameof(FieldsDisplayNames.NewPassword), ResourceType = typeof(FieldsDisplayNames))]
        public string? Password { get; set; }
        #endregion

        #region ConfirmPassword
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [DataType(DataType.Password)]
        [Display(Name = nameof(FieldsDisplayNames.ConfirmPassword), ResourceType = typeof(FieldsDisplayNames))]
        [Compare("Password", ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.CompareConfirmPassword))]
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
