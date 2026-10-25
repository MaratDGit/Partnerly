using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
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

        public DataTableOptions userPermissions { get; set; } = new DataTableOptions();

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
                    Back = new Partnerly.Models.ViewModels.ActionBack(id: Id, visible: true, fromController: "Roles", toController: "Users", fromAction: "UserRoleEdit", toAction: "Index") { Permission = PermissionCodes.UsersView },
                    Undo = new Partnerly.Models.ViewModels.ActionUndo(id: Id, visible: true, fromController: "Roles", toController: "Roles", fromAction: "UserRoleEdit", toAction: "UserRoleEdit") { Permission = PermissionCodes.UsersManageRoles },
                    Save = new Partnerly.Models.ViewModels.ActionSave(id: Id, visible: true, fromController: "Roles", toController: "Roles", fromAction: "UserRoleEdit", toAction: "UserRoleEdit") { Permission = PermissionCodes.UsersManageRoles },
                    Note = new Partnerly.Models.ViewModels.ActionNote(visible: true, note: Note, id: Id) { Permission = PermissionCodes.UsersManageRoles },
                    Menu = new Partnerly.Models.ViewModels.ActionMenu(visible: true),
                    MenuActions = new List<Partnerly.Models.ViewModels.MenuItemAction> {
                        new Partnerly.Models.ViewModels.MenuItemAction(visible: true, icon: "bx bx-show", displayName: FieldsDisplayNames.Viewuser, toController: "Users", toAction: "Edit", routID: Id) { Permission = PermissionCodes.UsersEdit },
                    }
                };
            }
        }

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.userPermissionsTable =>
                    new DataTableOptions
                    {
                        Id = tableID,
                        //Name = FieldsDisplayNames.Permissions,
                        //Icon = "bx bx-shield-quarter",

                        KeyField = "Id",
                        AjaxUrl = "/Roles/Data",
                        ExportUrl = "/Roles/Export",

                        AutoLoad = true,
                        ReloadEnabled = true,
                        RememberState = true,

                        InlineEditing = true,
                        InlineUpdateUrl = "/Roles/UpdateInline",

                        Columns = new List<DataTableColumn>
                        {
                            new DataTableColumn
                            {
                                Field = "Id",
                                Type = DataTableColumnType.Text,
                                Visible = false,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.UserId),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Required = true,
                                IsKey = true,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.PermissionId),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Required = true,
                                IsKey = true,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.UserName),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.PermissionCode),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.PermissionName),
                                Title = FieldsDisplayNames.Permission,
                                Type = DataTableColumnType.Text,
                                Required = true,
                                Editable = true,
                                Permission = PermissionCodes.UsersManageRoles,

                                Lookup = new DataTableLookupOptions
                                {
                                    Url = "/Roles/LookupPermissions",
                                    IdField = nameof(UserPermissionExt.PermissionId),
                                    RecordIdField = "id",
                                    DisplayField = "name",
                                    Title = FieldsDisplayNames.Permission,
                                    SearchPlaceholder = FieldsDisplayNames.DataTableSearch,
                                    SearchParameter = "search",

                                    ParentKeyParameters = new Dictionary<string, string>
                                    {
                                        ["userId"] = "UserId"
                                    },

                                    Columns = new List<DataTableLookupColumn>
                                    {
                                        new()
                                        {
                                            Field = "name",
                                            Title = FieldsDisplayNames.Permission,
                                            Width = "160px"
                                        },
                                        new()
                                        {
                                            Field = "module",
                                            Title = FieldsDisplayNames.Module,
                                            Width = "240px"
                                        },
                                        new()
                                        {
                                            Field = "isActive",
                                            Title = FieldsDisplayNames.IsActive,
                                            Width = "50px",
                                            DisplayAsCheckbox = true
                                        },
                                    },

                                    Mappings = new List<DataTableLookupMapping>
                                    {
                                        new()
                                        {
                                            SourceField = "code",
                                            TargetField = nameof(UserPermissionExt.PermissionCode)
                                        },
                                        new()
                                        {
                                            SourceField = "module",
                                            TargetField = nameof(UserPermissionExt.Module)
                                        },
                                        new()
                                        {
                                            SourceField = "isActive",
                                            TargetField = nameof(UserPermissionExt.IsActive)
                                        },
                                        new()
                                        {
                                            SourceField = "isDeleted",
                                            TargetField = nameof(UserPermissionExt.IsDeleted)
                                        },
                                    }
                                }
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.Module),
                                Title = FieldsDisplayNames.Module,
                                Type = DataTableColumnType.Text,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.IsGranted),
                                Title = FieldsDisplayNames.IsGranted,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center,
                                Editable = true,
                                Permission = PermissionCodes.UsersManageRoles,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.IsActive),
                                Title = FieldsDisplayNames.IsActive,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center
                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserPermissionExt.IsDeleted),
                                Title = FieldsDisplayNames.IsDeleted,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center
                            },
                        },

                        Search = new DataTableSearchOptions
                        {
                            Enabled = true,
                            Placeholder = FieldsDisplayNames.DataTableSearch,
                        },

                        Sorting = new DataTableSortOptions
                        {
                            DefaultField = nameof(UserPermissionExt.PermissionName),
                        },

                        Filters = new DataTableFilterOptions
                        {
                            Enabled = true,
                            Items = new List<DataTableFilter>
                            {
                                new DataTableFilter
                                {
                                    Field = nameof(UserPermissionExt.IsGranted),
                                    Label = FieldsDisplayNames.IsGranted,
                                    Type = DataTableFilterType.Boolean
                                },
                                new DataTableFilter
                                {
                                    Field = nameof(UserPermissionExt.IsActive),
                                    Label = FieldsDisplayNames.IsActive,
                                    Type = DataTableFilterType.Boolean
                                },
                                new DataTableFilter
                                {
                                    Field = nameof(UserPermissionExt.IsDeleted),
                                    Label = FieldsDisplayNames.IsDeleted,
                                    Type = DataTableFilterType.Boolean
                                }
                            }
                        },

                        Export = new DataTableExportOptions
                        {
                            Enabled = false,
                        },

                        Paging = new DataTablePagingOptions
                        {
                            Enabled = true,
                            PageSize = 5,
                        },

                        Actions = new DataTableActionOptions
                        {
                            Enabled = true,
                            Items = new List<DataTableAction>
                            {
                                new DataTableAction
                                {
                                    Name = "add",
                                    Title = FieldsDisplayNames.Add,
                                    Icon = "bx bx-plus",
                                    Type = DataTableActionType.InlineAdd,
                                    Url = "/Roles/CreateInline",
                                    Placement = DataTableActionPlacement.Toolbar,
                                    CssClass = "data-table-grid-action",
                                    Permission = PermissionCodes.UsersManageRoles
                                },
                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.InlineDelete,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Roles/DeleteInline",
                                    Method = "POST",
                                    Confirm = true,
                                    ConfirmMessage = ErrorMessages.ConfirmDelete,
                                    CssClass = "btn btn-sm btn-outline-danger",
                                    Permission = PermissionCodes.UsersManageRoles
                                }
                            }
                        },
                    },

                _ => throw new ArgumentException(
                    $"Unknown DataTable: {tableID}")
            };

            _dataTableRegistry.Register(options);

            return options;
        }
    }
}
