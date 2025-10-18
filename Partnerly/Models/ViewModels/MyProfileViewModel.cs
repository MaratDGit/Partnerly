using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class MyProfileViewModel
    {
        #region ID
        public Guid Id { get; set; }
        #endregion

        #region UserName
        [Display(Name = FieldsDisplayNames.FirstName)]
        public string? UserName { get; set; }
        #endregion

        #region Email
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [Required(ErrorMessage = $"{FieldsDisplayNames.Phone} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.Phone)]
        public string? Phone { get; set; }
        #endregion

        #region PhotoUrl
        public string? PhotoUrl { get; set; }
        #endregion

        #region RoleName
        [Display(Name = FieldsDisplayNames.Role)]
        public string? RoleName { get; set; }
        #endregion

        #region MyReferralCode
        [Display(Name = FieldsDisplayNames.ReferrerCode)]
        public string? MyReferralCode { get; set; }
        #endregion

        #region ReferrerName
        [Display(Name = FieldsDisplayNames.ReffererName)]
        public string? ReferrerName { get; set; }
        #endregion
        public DateTime? CreatedDate { get; set; }

        public List<UserViewModel> Refferals { get; set; } = new List<UserViewModel>();
    }
}
