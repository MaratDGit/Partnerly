using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserRolesViewModel
    {
        #region ID
        [Required]
        public Guid Id { get; set; }
        #endregion

        #region UserName
        [Required(ErrorMessage = $"{FieldsDisplayNames.User} {ErrorMessages.FieldRequired}")]
        [Display(Name = FieldsDisplayNames.User)]
        public string? UserName { get; set; }
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

        #region OldRoleId
        [Required]
        [Display(Name = FieldsDisplayNames.Role)]
        public Guid? OldRoleId { get; set; }
        #endregion

        #region OldRoleName
        [Required]
        [Display(Name = FieldsDisplayNames.Role)]
        public Guid? OldRoleName { get; set; }
        #endregion

        #region NewRoleId
        [Required]
        [Display(Name = FieldsDisplayNames.NewRole)]
        public Guid? NewRoleId { get; set; }
        #endregion

        #region NewRoleName
        [Required]
        [Display(Name = FieldsDisplayNames.NewRole)]
        public Guid? NewRoleName { get; set; }
        #endregion
    }
}
