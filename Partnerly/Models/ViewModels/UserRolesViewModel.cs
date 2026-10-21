using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserRolesViewModel : IAuditableViewModel
    {
        public string? Title { get; set; } = " User Role";

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

        #region Audit fields
        [Display(Name = nameof(FieldsDisplayNames.CreatorName), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatedByUserName { get; set; }
        public Guid? CreatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.CreatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdaterName), ResourceType = typeof(FieldsDisplayNames))]
        public string? UpdatedByUserName { get; set; }
        public Guid? UpdatedBy { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.UpdatedDate), ResourceType = typeof(FieldsDisplayNames))]
        public DateTime? UpdatedDate { get; set; }
        #endregion

        public FormActionsViewModel StickyButtons
        {
            get
            {
                return new Partnerly.Models.ViewModels.FormActionsViewModel
                {
                    Back = new Partnerly.Models.ViewModels.ActionBack(id: Id, visible: true, fromController: "Roles", toController: "Users", fromAction: "Edit", toAction: "Index"),
                    Undo = new Partnerly.Models.ViewModels.ActionUndo(id: Id, visible: true, fromController: "Roles", toController: "Roles", fromAction: "Edit", toAction: "Edit"),
                    Save = new Partnerly.Models.ViewModels.ActionSave(id: Id, visible: true, fromController: "Roles", toController: "Roles", fromAction: "Edit", toAction: "Edit"),
                    Note = new Partnerly.Models.ViewModels.ActionNote(visible: true, note: Note, id: Id),
                    Menu = new Partnerly.Models.ViewModels.ActionMenu(visible: true),
                    MenuActions = new List<Partnerly.Models.ViewModels.MenuItemAction> {
                        new Partnerly.Models.ViewModels.MenuItemAction(visible: true, icon: "bx bx-show", displayName: FieldsDisplayNames.Viewuser, toController: "Users", toAction: "Edit", routID: Id),
                    }
                };
            }
        }
    }
}
