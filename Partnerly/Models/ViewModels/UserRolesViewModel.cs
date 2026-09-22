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
        [Display(Name = nameof(FieldsDisplayNames.User), ResourceType = typeof(FieldsDisplayNames))]
        public string? UserName { get; set; }
        #endregion

        #region Email
        [Display(Name = nameof(FieldsDisplayNames.Email), ResourceType = typeof(FieldsDisplayNames))]
        public string? Email { get; set; }
        #endregion

        #region Phone
        [Display(Name = nameof(FieldsDisplayNames.Phone), ResourceType = typeof(FieldsDisplayNames))]
        public string? Phone { get; set; }
        #endregion

        #region OldRoleId
        [Display(Name = nameof(FieldsDisplayNames.Role), ResourceType = typeof(FieldsDisplayNames))]
        public Guid? OldRoleId { get; set; }
        #endregion

        #region OldRoleName
        [Display(Name = nameof(FieldsDisplayNames.Role), ResourceType = typeof(FieldsDisplayNames))]
        public string? OldRoleName { get; set; }
        #endregion

        #region NewRoleName
        [Required]
        [Display(Name = nameof(FieldsDisplayNames.NewRole), ResourceType = typeof(FieldsDisplayNames))]
        public string? NewRoleName { get; set; }
        #endregion
        public string? Note { get; set; }
        public List<SelectListItem> RoleNamesList { get => AttributeDropdownHelper.ToSelectedList(RoleTypeAttribute.RoleNames); }
    }
}
