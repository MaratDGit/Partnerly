using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Partnerly.Models.ViewModels
{
    public class RolesViewModel : IAuditableViewModel
    {
        public string? Title { get; set; } = "Roles";

        #region Id
        [Required]
        public Guid Id { get; set; }
        #endregion
        #region Name
        [Required]
        [Display(Name = "Name")]
        public string? Name { get; set; }
        #endregion
       
        public bool IsActive { get; set; }

        #region Unbound fields
        [NotMapped]
        public bool Selected { get; set; }
        #endregion

        #region System Columns
        public string? Note { get; set; }
        public string? CreatedByUserName { get; set; }
        public Guid? CreatedBy { get; set; }       
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedByUserName { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
        #endregion

        public int PermissionCount => RolePermissions.Count;
        public int UsersCount => Users?.Count ?? 0;
        public bool IsEdit => Id != Guid.Empty;

        public ICollection<User>? Users { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public FormActionsViewModel StickyButtons
        {
            get
            {
                if (!IsEdit)
                {
                    return new FormActionsViewModel
                    {
                        Back = new ActionBack(
                            visible: true,
                            fromController: "Roles",
                            toController: "Roles",
                            fromAction: "Create",
                            toAction: "Index") { Permission = PermissionCodes.RolesView },

                        Undo = new ActionUndo(
                            visible: true,
                            fromController: "Roles",
                            toController: "Roles",
                            fromAction: "Create",
                            toAction: "Create") { Permission = PermissionCodes.RolesCreate },

                        Save = new ActionSave(
                            visible: true,
                            fromController: "Roles",
                            toController: "Roles",
                            fromAction: "Create",
                            toAction: "Edit") { Permission = PermissionCodes.RolesCreate }
                    };
                }

                return new FormActionsViewModel
                {
                    Back = new ActionBack(
                        id: Id,
                        visible: true,
                        fromController: "Roles",
                        toController: "Roles",
                        fromAction: "Edit",
                        toAction: "Index") { Permission = PermissionCodes.RolesView },

                    Undo = new ActionUndo(
                        id: Id,
                        visible: true,
                        fromController: "Roles",
                        toController: "Roles",
                        fromAction: "Edit",
                        toAction: "Edit") { Permission = PermissionCodes.RolesEdit },

                    Save = new ActionSave(
                        id: Id,
                        visible: true,
                        fromController: "Roles",
                        toController: "Roles",
                        fromAction: "Edit",
                        toAction: "Edit") { Permission = PermissionCodes.RolesEdit },

                    Create = new ActionCreate(
                        id: Id,
                        visible: true,
                        fromController: "Roles",
                        toController: "Roles",
                        fromAction: "Edit",
                        toAction: "Create") { Permission = PermissionCodes.RolesCreate },

                    Delete = new ActionDelete(
                        id: Id,
                        visible: true,
                        fromController: "Roles",
                        toController: "Roles",
                        fromAction: "Edit",
                        toAction: "Delete") { Permission = PermissionCodes.RolesDelete },

                    Attachements = new ActionAttachements(
                        visible: true,
                        entityType: "Role",
                        entityId: Id) { Permission = PermissionCodes.AttachmentsView },

                    Note = new ActionNote(
                        visible: true,
                        note: Note,
                        id: Id) { Permission = PermissionCodes.RolesEdit },

                    Menu = new ActionMenu(visible: false),
                };
            }
        }

        public DataTableOptions roles { get; set; } = new DataTableOptions();

        public DataTableOptions rolePermissions { get; set; } = new DataTableOptions();
        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.rolesTable =>
                   new DataTableOptions
                   {
                       Id = tableID,
                       Name = FieldsDisplayNames.Roles,
                       Icon = "bx bx-shield-quarter",

                       KeyField = "Id",
                       AjaxUrl = "/Roles/Data",
                       ExportUrl = "/Roles/Export",
                       Export = new DataTableExportOptions
                       {
                           Enabled = true,
                           Permission = PermissionCodes.RolesView
                       },

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
                                Title = "ID",
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Permission = PermissionCodes.DataTableViewIds,
                            },
                            DataTableColumn.HiddenSelected(),
                            new DataTableColumn
                            {
                                Field = nameof(Models.Role.Name),
                                Title = FieldsDisplayNames.Name,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/Roles/Edit/{Id}",
                                Editable = true,
                                Permission = PermissionCodes.RolesEdit,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.Role.IsActive),
                                Title = FieldsDisplayNames.IsActive,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center,
                                Editable = true,
                                Permission = PermissionCodes.RolesEdit,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.Role.IsDeleted),
                                Title = FieldsDisplayNames.IsDeleted,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center,
                            },
                            
                            new DataTableColumn
                            {
                                Field = nameof(Models.Role.UpdatedDate),
                                Title = FieldsDisplayNames.UpdatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "relative",
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.Role.CreatedDate),
                                Title = FieldsDisplayNames.CreatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "dd.MM.yyyy HH:mm",
                            },
                            DataTableColumn.HiddenNote()
                        },

                       Sorting = new DataTableSortOptions
                       {
                           DefaultField = "CreatedDate",
                       },

                       Filters = new DataTableFilterOptions
                       {
                           Enabled = true,
                           Items = new List<DataTableFilter>
                            {
                                new DataTableFilter
                                {
                                    Field = nameof(Models.Role.IsActive),
                                    Label = FieldsDisplayNames.IsActive,
                                    Type = DataTableFilterType.Boolean
                                },
                                new DataTableFilter
                                {
                                    Field = nameof(Models.Role.IsDeleted),
                                    Label = FieldsDisplayNames.IsDeleted,
                                    Type = DataTableFilterType.Boolean
                                },
                                new DataTableFilter
                                {
                                    Field = "CreatedDate",
                                    Label = FieldsDisplayNames.CreatedDate,
                                    Type = DataTableFilterType.DateRange
                                }
                            }
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
                                    Permission = PermissionCodes.RolesCreate
                                },

                                new DataTableAction
                                {
                                    Name = "edit",
                                    Title = FieldsDisplayNames.Edit,
                                    Icon = "bx bx-edit",
                                    Type = DataTableActionType.InlineEdit,
                                    Placement = DataTableActionPlacement.Row,
                                    Permission = PermissionCodes.RolesEdit
                                },

                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Roles/Delete/{Id}",
                                    Method = "GET",
                                    CssClass = "btn btn-sm btn-outline-danger",
                                    Permission = PermissionCodes.RolesDelete
                                },

                                new DataTableAction
                                {
                                    Name = "permissions",
                                    Text = FieldsDisplayNames.Permissions,
                                    Title = FieldsDisplayNames.Permissions,
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Roles/Edit/{Id}",
                                    Method = "GET",
                                    CssClass = "btn btn-sm btn-outline-primary data-table-action-text",
                                    Permission = PermissionCodes.RolesManagePermissions
                                }
                            }
                       },
                   },

                DataTableNames.rolePermissionsTable =>
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

                   InlineEditing = false,

                   Columns = new List<DataTableColumn>
                    {
                            new DataTableColumn
                            {
                                Field = "Id",
                                Title = "ID",
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Permission = PermissionCodes.DataTableViewIds,
                            },
                            DataTableColumn.HiddenSelected(),
                            new DataTableColumn
                            {
                                Field = nameof(Models.Authorization.RolePermissionExt.RoleId),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                IsKey = true,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Models.Authorization.RolePermissionExt.PermissionId),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                IsKey = true,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(RolePermissionExt.RoleName),
                                Type = DataTableColumnType.Text,
                                Visible = false,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(RolePermissionExt.PermissionName),
                                Title = FieldsDisplayNames.Permission,
                                Type = DataTableColumnType.Text,
                                Editable = true,
                                Permission = PermissionCodes.RolesManagePermissions,

                                Lookup = new DataTableLookupOptions
                                {
                                    Url = "/Roles/LookupPermissions",
                                    IdField = nameof(RolePermissionExt.PermissionId),

                                    DisplayField = "name",
                                    Title = FieldsDisplayNames.Permission,

                                    SearchPlaceholder = FieldsDisplayNames.DataTableSearch,

                                    SearchParameter = "search",

                                    ParentKeyParameters = new Dictionary<string, string>
                                    {
                                        ["roleId"] = "RoleId"
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
                                            SourceField = "module",
                                            TargetField = nameof(RolePermissionExt.Module)
                                        },
                                        new()
                                        {
                                            SourceField = "isActive",
                                            TargetField = nameof(RolePermissionExt.IsActive)
                                        },
                                        new()
                                        {
                                            SourceField = "isDeleted",
                                            TargetField = nameof(RolePermissionExt.IsDeleted)
                                        },
                                    }
                                }
                            },
                            new DataTableColumn
                            {
                                Field = nameof(RolePermissionExt.Module),
                                Title = FieldsDisplayNames.Module,
                                Type = DataTableColumnType.Text,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(RolePermissionExt.IsActive),
                                Title = FieldsDisplayNames.IsActive,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center
                            },
                            new DataTableColumn
                            {
                                Field = nameof(RolePermissionExt.IsDeleted),
                                Title = FieldsDisplayNames.IsDeleted,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center
                            },
                    },

                   Sorting = new DataTableSortOptions
                   {
                       DefaultField = nameof(RolePermissionExt.PermissionName),
                   },

                   Filters = new DataTableFilterOptions
                   {
                       Enabled = true,
                       Items = new List<DataTableFilter>
                        {
                            new DataTableFilter
                            {
                                Field = nameof(RolePermissionExt.IsActive),
                                Label = FieldsDisplayNames.IsActive,
                                Type = DataTableFilterType.Boolean
                            },
                            new DataTableFilter
                            {
                                Field = nameof(RolePermissionExt.IsDeleted),
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
                                    Permission = PermissionCodes.RolesManagePermissions
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
                                    Permission = PermissionCodes.RolesManagePermissions
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
