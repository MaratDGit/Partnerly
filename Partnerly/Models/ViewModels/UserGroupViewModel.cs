using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserGroupViewModel : IAuditableViewModel
    {
        public string? Title { get; set; } = " User Group";
        public Guid Id { get; set; }

        [CustomStringLenght(min: 2, max: 15)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Name), ResourceType = typeof(FieldsDisplayNames))]
        public string? Name { get; set; }

        [CustomStringLenght(max: 100)]
        [Display(Name = nameof(FieldsDisplayNames.Description), ResourceType = typeof(FieldsDisplayNames))]
        public string? Description { get; set; }

        public List<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();

        #region System Columns
        public string? Note { get; set; } 
        #endregion
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
                bool IsEdit = Id != Guid.Empty;

                if (IsEdit)
                {
                    return new Partnerly.Models.ViewModels.FormActionsViewModel
                    {
                        Back = new Partnerly.Models.ViewModels.ActionBack(
                        id: Id,
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Edit",
                        toAction: "Index") { Permission = PermissionCodes.UserGroupsView },

                        Undo = new Partnerly.Models.ViewModels.ActionUndo(
                        id: Id,
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Edit",
                        toAction: "Edit") { Permission = PermissionCodes.UserGroupsEdit },

                        Save = new Partnerly.Models.ViewModels.ActionSave(
                        id: Id,
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Edit",
                        toAction: "Edit") { Permission = PermissionCodes.UserGroupsEdit },

                        Create = new Partnerly.Models.ViewModels.ActionCreate(
                        id: Id,
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Edit",
                        toAction: "Create") { Permission = PermissionCodes.UserGroupsCreate },

                        Delete = new Partnerly.Models.ViewModels.ActionDelete(
                        id: Id,
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Edit",
                        toAction: "Delete") { Permission = PermissionCodes.UserGroupsDelete },

                        Attachements = new Partnerly.Models.ViewModels.ActionAttachements(
                        entityType: "UserGroup",
                        entityId: Id,
                        visible: true) { Permission = PermissionCodes.AttachmentsView },

                        Note = new Partnerly.Models.ViewModels.ActionNote(
                        visible: true,
                        note: Note,
                        id: Id) { Permission = PermissionCodes.UserGroupsEdit },

                        Menu = new Partnerly.Models.ViewModels.ActionMenu(
                        visible: true),

                        MenuActions = new List<MenuItemAction>
                        {
                            new Partnerly.Models.ViewModels.MenuItemAction(
                                visible: true,
                                icon: "bx bx-bell",
                                displayName: FieldsDisplayNames.Sendnotification,
                                isModal: true,
                                modalTarget: "#sendGroupMessageModal",
                                onClick: "prepareGroupSend('notification')") { Permission = PermissionCodes.NotificationsCreate },

                            new Partnerly.Models.ViewModels.MenuItemAction(
                                visible: true,
                                icon: "bx bx-envelope-open",
                                displayName: FieldsDisplayNames.Sendanemail,
                                isModal: true,
                                modalTarget: "#sendGroupMessageModal",
                                onClick: "prepareGroupSend('email')") { Permission = PermissionCodes.EmailTemplatesView }
                        }
                    };      
                }

                return new Partnerly.Models.ViewModels.FormActionsViewModel
                {
                    Back = new Partnerly.Models.ViewModels.ActionBack(
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Create",
                        toAction: "Index") { Permission = PermissionCodes.UserGroupsView },

                    Undo = new Partnerly.Models.ViewModels.ActionUndo(
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Create",
                        toAction: "Create") { Permission = PermissionCodes.UserGroupsCreate },

                    Save = new Partnerly.Models.ViewModels.ActionSave(
                        visible: true,
                        fromController: "UserGroups",
                        toController: "UserGroups",
                        fromAction: "Create",
                        toAction: "Create") { Permission = PermissionCodes.UserGroupsCreate }
                };
            }
        }

        public DataTableOptions groups { get; set; } = new DataTableOptions();
        public DataTableOptions groupMembers { get; set; } = new DataTableOptions();

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.userGroupsTable =>
                    new DataTableOptions
                    {
                        Id = DataTableNames.userGroupsTable,
                        Name = FieldsDisplayNames.Group,
                        Icon = "bx bx-group",

                        KeyField = "Id",
                        AjaxUrl = "/UserGroups/Data",
                        ExportUrl = "/UserGroups/Export",

                        AutoLoad = true,
                        ReloadEnabled = true,
                        RememberState = true,

                        InlineEditing = true,
                        InlineUpdateUrl = "/UserGroups/UpdateInline",

                        Columns = new List<DataTableColumn>
                            {
                            new DataTableColumn
                            {
                                Field = "Id",
                                Title = "ID",
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Required = true,
                                Permission = PermissionCodes.DataTableViewIds,
                            },
                            DataTableColumn.HiddenSelected(),

                            new DataTableColumn
                            {
                                Field = nameof(Models.UserGroup.Name),
                                Title = FieldsDisplayNames.Name,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/UserGroups/Edit/{Id}",
                                Permission = PermissionCodes.UserGroupsEdit,

                                Editable = true,
                                Visible = true,
                                Required = true,
                                Sortable = true,
                                Searchable = true,
                                Exportable = true,
                                Width = "",
                                Format = "",
                                EmptyText = "-",
                                Alignment = DataTableAlignment.Left,
                                CssClass = "",
                                Resizable = true
                            },

                            new DataTableColumn
                            {
                                Field = nameof(Models.UserGroup.Description),
                                Title = FieldsDisplayNames.Description,
                                Type = DataTableColumnType.Text,
                                Editable = true,
                                Permission = PermissionCodes.UserGroupsEdit,
                                Resizable = true
                            },

                            new DataTableColumn
                            {
                                Field = nameof(Models.UserGroup.UpdatedDate),
                                Title = FieldsDisplayNames.UpdatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "relative",
                                Resizable = true
                            },

                            new DataTableColumn
                            {
                                Field = nameof(Models.User.CreatedDate),
                                Title = FieldsDisplayNames.CreatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "dd.MM.yyyy HH:mm",
                                Resizable = true
                            },
                            DataTableColumn.HiddenNote()
                        },

                        Sorting = new DataTableSortOptions
                        {
                            Enabled = true,
                            DefaultField = "CreatedDate",
                            DefaultDirection = "desc"
                        },

                        Filters = new DataTableFilterOptions
                        {
                            Enabled = true,
                            Items = new List<DataTableFilter>
                    {
                        new DataTableFilter
                        {
                            Field = nameof(Models.User.CreatedDate),
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
                                    Title = FieldsDisplayNames.Create,
                                    Icon = "bx bx-plus",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Toolbar,
                                    Url = "/UserGroups/Create",
                                    Method = "Get",
                                    CssClass = "data-table-grid-action",
                                    Permission = PermissionCodes.UserGroupsCreate
                                },

                                //new DataTableAction
                                //{
                                //    Name = "add",
                                //    Title = "",
                                //    Icon = "bx bx-plus",
                                //    Type = DataTableActionType.InlineAdd,
                                //    Url = "/UserGroups/CreateInline",
                                //    Placement = DataTableActionPlacement.Toolbar,
                                //    CssClass = "data-table-grid-action"
                                //},

                                new DataTableAction
                                {
                                    Name = "view",
                                    Title = FieldsDisplayNames.View,
                                    Icon = "bx bx-show",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/UserGroups/Edit/{Id}",
                                    Method = "GET",
                                    Permission = PermissionCodes.UserGroupsEdit
                                },

                                new DataTableAction
                                {
                                    Name = "edit",
                                    Title = FieldsDisplayNames.Edit,
                                    Icon = "bx bx-edit",
                                    Type = DataTableActionType.InlineEdit,
                                    Placement = DataTableActionPlacement.Row,
                                    Permission = PermissionCodes.UserGroupsEdit
                                },

                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/UserGroups/Delete/{Id}",
                                    Method = "GET",
                                    CssClass = "btn btn-sm btn-outline-danger",
                                    Permission = PermissionCodes.UserGroupsDelete
                                },
                            }
                        },

                        Export = new DataTableExportOptions
                        {
                            Enabled = true,
                            Csv = true,
                            Excel = true,
                            Print = false,
                            Permission = PermissionCodes.UserGroupsView
                        }
                    },

                DataTableNames.userGroupsMembersTable =>
                    new DataTableOptions
                    {
                        Id = DataTableNames.userGroupsMembersTable,
                        KeyField = "UserId",
                        AjaxUrl = "/UserGroups/Data",
                        ExportUrl = "/UserGroups/Export",

                        AutoLoad = true,
                        ReloadEnabled = true,
                        RememberState = true,

                        InlineEditing = false,
                        InlineUpdateUrl = "",

                        Columns = new List<DataTableColumn>
                        {
                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.UserId),
                                Title = "UserId",
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Required = true,
                                IsKey = true,
                            },

                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.GroupId),
                                Title = "GroupId",
                                Type = DataTableColumnType.Text,
                                Visible = false,
                                Required = true,
                                IsKey = true,
                            },

                            DataTableColumn.VisibleSelected(),

                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.UserFirstName),
                                Title = FieldsDisplayNames.FirstName,
                                Type = DataTableColumnType.Text,
                                Required = true,
                                Editable = true,
                                Permission = PermissionCodes.UserGroupsEdit,

                                Lookup = new DataTableLookupOptions
                                {
                                    Url = "/UserGroups/LookupUsers",
                                    IdField = nameof(UserGroupMemberExt.UserId),

                                    DisplayField = "name",
                                    SecondaryField = "email",

                                    Title = FieldsDisplayNames.Users,
                                    SearchPlaceholder = FieldsDisplayNames.DataTableSearch,

                                    SearchParameter = "search",

                                    ParentKeyParameters = new Dictionary<string, string>
                                    {
                                        ["groupId"] = "GroupID"
                                    },

                                    Columns = new List<DataTableLookupColumn>
                                    {
                                        new()
                                        {
                                            Field = "name",
                                            Title = FieldsDisplayNames.User,
                                            Width = "160px"
                                        },
                                        new()
                                        {
                                            Field = "email",
                                            Title = FieldsDisplayNames.Email,
                                            Width = "240px"
                                        },
                                        new()
                                        {
                                            Field = "isBlocked",
                                            Title = FieldsDisplayNames.IsBlocked,
                                            Width = "50px",
                                            DisplayAsCheckbox = true
                                        },
                                    },

                                    Mappings = new List<DataTableLookupMapping>
                                    {
                                        new()
                                        {
                                            SourceField = "lastName",
                                            TargetField = nameof(UserGroupMemberExt.UserLastName)
                                        },
                                        new()
                                        {
                                            SourceField = "email",
                                            TargetField = nameof(UserGroupMemberExt.UserEmail)
                                        }
                                    }
                                }


                            },
                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.UserLastName),
                                Title = FieldsDisplayNames.LastName,
                                Type = DataTableColumnType.Text,
                                Required = true,
                            },

                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.UserEmail),
                                Title = FieldsDisplayNames.Email,
                                Type = DataTableColumnType.Text,
                            },

                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.UpdatedDate),
                                Title = FieldsDisplayNames.UpdatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "relative",
                            },

                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.CreatedDate),
                                Title = FieldsDisplayNames.CreatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "dd.MM.yyyy HH:mm",
                            },
                            DataTableColumn.HiddenNote()
                        },

                        Paging = new DataTablePagingOptions
                        { 
                            Enabled = true,
                            PageSize = 6,
                        },

                        Sorting = new DataTableSortOptions
                        {
                            Enabled = true,
                            DefaultField = nameof(UserGroupMemberExt.CreatedDate),
                        },

                        Filters = new DataTableFilterOptions
                        {
                            Enabled = true,
                            Items = new List<DataTableFilter>
                            {
                                new DataTableFilter
                                {
                                    Field = nameof(UserGroupMemberExt.CreatedDate),
                                    Label = FieldsDisplayNames.CreatedDate,
                                    Type = DataTableFilterType.DateRange
                                }
                            }
                        },

                        Export = new DataTableExportOptions
                        {
                            Enabled = false,
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
                                    Url = "/UserGroups/CreateInline",
                                    Placement = DataTableActionPlacement.Toolbar,
                                    CssClass = "data-table-grid-action",
                                    Permission = PermissionCodes.UserGroupsEdit
                                },
                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.InlineDelete,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/UserGroups/DeleteInline",
                                    Method = "POST",
                                    Confirm = true,
                                    ConfirmMessage = ErrorMessages.ConfirmDelete,
                                    CssClass = "btn btn-sm btn-outline-danger",
                                    Permission = PermissionCodes.UserGroupsEdit
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
