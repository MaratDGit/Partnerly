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
        [Display(Name = nameof(FieldsDisplayNames.FirstName), ResourceType = typeof(FieldsDisplayNames))]
        public string? UserName { get; set; }
        #endregion

        #region Email
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Phone), ResourceType = typeof(FieldsDisplayNames))]
        public string? Phone { get; set; }
        #endregion

        #region PhotoUrl
        public string? PhotoUrl { get; set; }
        #endregion

        #region RoleName
        [Display(Name = nameof(FieldsDisplayNames.Role), ResourceType = typeof(FieldsDisplayNames))]
        public string? RoleName { get; set; }
        #endregion

        #region MyReferralCode
        [Display(Name = nameof(FieldsDisplayNames.ReferrerCode), ResourceType = typeof(FieldsDisplayNames))]
        public string? MyReferralCode { get; set; }
        #endregion

        #region ReferrerName
        [Display(Name = nameof(FieldsDisplayNames.ReffererName), ResourceType = typeof(FieldsDisplayNames))]
        public string? ReferrerName { get; set; }
        #endregion
        public DateTime? CreatedDate { get; set; }

        public List<UserViewModel> Refferals { get; set; } = new List<UserViewModel>();
    }
}
