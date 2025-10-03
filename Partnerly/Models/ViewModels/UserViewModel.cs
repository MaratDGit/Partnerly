using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserViewModel
    {
        #region ID
        [Required]
        public Guid Id { get; set; }
        #endregion
        #region Email
        [Required]
        public string? Email { get; set; }
        #endregion
        #region Phone
        [Required]
        [ArmenianPhone(ErrorMessage = ErrorMessages.TypeValidPhoneNumber)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }
        #endregion
        #region FirstName
        [Required]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }
        #endregion
        #region LastName
        [Required]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }
        #endregion
        #region PhotoUrl
        public string? PhotoUrl { get; set; }
        #endregion
        #region Balance
        [Display(Name = "Balance")]
        public decimal? Balance { get; set; }
        #endregion
        #region RoleId
        [Required]
        [Display(Name = "Role")]
        public Guid? RoleId { get; set; }
        #endregion
        #region MyReferralCode
        [Required]
        [Display(Name = "My Referral Code")]
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
        [Display(Name = "Is Blocked")]
        public bool? IsBlocked { get; set; }
        #endregion
        #region EmailConfirmed
        [Display(Name = "Email Confirmed")]
        public bool? EmailConfirmed { get; set; }
        #endregion

        #region System Columns
        public bool IsDeleted { get; set; } = false;
        [Required]
        public Guid? CreatedBy { get; set; }
        [Required]
        public DateTime? CreatedDate { get; set; }
        [Required]
        public Guid? UpdatedBy { get; set; }
        [Required]
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
