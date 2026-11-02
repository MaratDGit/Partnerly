using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationListViewModel
    {
        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry registry)
        {
            var options = tableID switch
            {
                DataTableNames.automationsTable => new DataTableOptions
                {
                    Id = tableID,
                    Name = "My Automations",
                    Icon = "bx bx-bot",
                    KeyField = "Id",
                    AjaxUrl = "/Automation/Data",
                    AutoLoad = true,
                    ReloadEnabled = true,
                    RememberState = true,
                    Columns = new List<DataTableColumn>
                    {
                        new DataTableColumn
                        {
                            Field = "Id",
                            Title = "ID",
                            Type = DataTableColumnType.Text,
                            Visible = false,
                            Permission = PermissionCodes.DataTableViewIds
                        },
                        DataTableColumn.HiddenSelected(),
                        new DataTableColumn
                        {
                            Field = nameof(AutomationDefinition.Name),
                            Title = FieldsDisplayNames.Name,
                            Type = DataTableColumnType.Link,
                            UrlTemplate = "/Automation/Builder/{Id}"
                        },
                        new DataTableColumn
                        {
                            Field = nameof(AutomationDefinition.IsActive),
                            Title = FieldsDisplayNames.IsActive,
                            Type = DataTableColumnType.Boolean
                        },
                        new DataTableColumn
                        {
                            Field = nameof(AutomationDefinition.UpdatedDate),
                            Title = FieldsDisplayNames.UpdatedDate,
                            Type = DataTableColumnType.DateTime,
                            Format = "relative"
                        },
                        new DataTableColumn
                        {
                            Field = nameof(AutomationDefinition.CreatedDate),
                            Title = FieldsDisplayNames.CreatedDate,
                            Type = DataTableColumnType.DateTime,
                            Format = "dd.MM.yyyy HH:mm"
                        },
                        DataTableColumn.HiddenNote()
                    },
                    Sorting = new DataTableSortOptions
                    {
                        Enabled = true,
                        DefaultField = nameof(AutomationDefinition.UpdatedDate)
                    },
                    Paging = new DataTablePagingOptions { Enabled = true },
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
                                Url = "/Automation/Create",
                                Method = "Get",
                                CssClass = "data-table-grid-action",
                                Permission = PermissionCodes.AutomationCreate
                            },
                            new DataTableAction
                            {
                                Name = "edit",
                                Title = FieldsDisplayNames.Edit,
                                Icon = "bx bx-edit",
                                Type = DataTableActionType.Link,
                                Placement = DataTableActionPlacement.Row,
                                Url = "/Automation/Builder/{Id}",
                                Method = "GET",
                                Permission = PermissionCodes.AutomationEdit
                            },
                            new DataTableAction
                            {
                                Name = "history",
                                Title = FieldsDisplayNames.History,
                                Icon = "bx bx-history",
                                Type = DataTableActionType.Link,
                                Placement = DataTableActionPlacement.Row,
                                Url = "/Automation/History/{Id}",
                                Method = "GET",
                                Permission = PermissionCodes.AutomationHistory
                            },
                            new DataTableAction
                            {
                                Name = "settings",
                                Title = "Settings",
                                Icon = "bx bx-cog",
                                Type = DataTableActionType.Link,
                                Placement = DataTableActionPlacement.Row,
                                Url = "/Automation/Settings/{Id}",
                                Method = "GET",
                                Permission = PermissionCodes.AutomationEdit
                            },
                            new DataTableAction
                            {
                                Name = "delete",
                                Title = FieldsDisplayNames.Delete,
                                Icon = "bx bx-trash",
                                Type = DataTableActionType.Link,
                                Placement = DataTableActionPlacement.Row,
                                Url = "/Automation/Delete/{Id}",
                                Method = "GET",
                                CssClass = "btn btn-sm btn-outline-danger",
                                Permission = PermissionCodes.AutomationDelete
                            }
                        }
                    }
                },
                _ => throw new ArgumentException($"Unknown DataTable: {tableID}")
            };

            registry.Register(options);
            return options;
        }
    }
}
