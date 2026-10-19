using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class UserGroupViewModel : IAuditableViewModel
    {

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
                            },

                            new DataTableColumn
                            {
                                Field = nameof(Models.UserGroup.Name),
                                Title = FieldsDisplayNames.Name,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/UserGroups/Edit/{Id}",

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
                            }
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
                                    CssClass = "data-table-grid-action"
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
                                    // CssClass = "btn btn-sm btn-outline-secondary"
                                },

                                new DataTableAction
                                {
                                    Name = "edit",
                                    Title = FieldsDisplayNames.Edit,
                                    Icon = "bx bx-edit",
                                    Type = DataTableActionType.InlineEdit,
                                    Placement = DataTableActionPlacement.Row,                            
                                    //CssClass = "btn btn-sm btn-outline-primary"
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
                                    CssClass = "btn btn-sm btn-outline-danger"
                                },
                            }
                        },

                        Export = new DataTableExportOptions
                        {
                            Enabled = true,
                            Csv = true,
                            Excel = true,
                            Print = false
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

                            new DataTableColumn
                            {
                                Field = nameof(Models.UserGroupMemberExt.Selected),
                                Type = DataTableColumnType.Boolean,
                                Sortable = true,
                                Searchable = false,
                                Exportable = false,
                                Editable = false,
                                Alignment = DataTableAlignment.Center,
                            },

                            new DataTableColumn
                            {
                                Field = nameof(UserGroupMemberExt.UserFirstName),
                                Title = FieldsDisplayNames.FirstName,
                                Type = DataTableColumnType.Text,
                                Required = true,
                                Editable = true,

                                LookupUrl = "/UserGroups/LookupUsers",
                                LookupIdField = nameof(UserGroupMemberExt.UserId),
                                LookupTitle = FieldsDisplayNames.Users,
                                LookupSearchPlaceholder = FieldsDisplayNames.DataTableSearch
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
                            }
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
                                    CssClass = "data-table-grid-action"
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
                                    ConfirmMessage = ErrorMessages.Areyousureyouwanttodeletethisfile,
                                    CssClass = "btn btn-sm btn-outline-danger"
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
