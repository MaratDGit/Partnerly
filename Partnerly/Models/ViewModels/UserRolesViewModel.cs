using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
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
        [Display(Name = FieldsDisplayNames.User)]
        public string? UserName { get; set; }
        #endregion

        #region Email
        [Display(Name = FieldsDisplayNames.Email)]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [Display(Name = FieldsDisplayNames.Phone)]
        public string? Phone { get; set; }
        #endregion

        #region OldRoleId
        [Display(Name = FieldsDisplayNames.Role)]
        public Guid? OldRoleId { get; set; }
        #endregion

        #region OldRoleName
        [Display(Name = FieldsDisplayNames.Role)]
        public string? OldRoleName { get; set; }
        #endregion

        #region NewRoleName
        [Required]
        [Display(Name = FieldsDisplayNames.NewRole)]
        public string? NewRoleName { get; set; }
        #endregion
        public string? Note { get; set; }
        public List<SelectListItem> RoleNamesList { get => AttributeDropdownHelper.ToSelectedList(RoleTypeAttribute.RoleNames); }
    }
}
