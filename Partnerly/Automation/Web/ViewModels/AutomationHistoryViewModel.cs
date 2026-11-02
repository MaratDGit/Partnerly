using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Web.ViewModels
{
    public class AutomationHistoryViewModel
    {
        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry registry)
        {
            var options = tableID switch
            {
                DataTableNames.automationRunsTable => new DataTableOptions
                {
                    Id = tableID,
                    Name = FieldsDisplayNames.History,
                    Icon = "bx bx-history",
                    KeyField = "Id",
                    AjaxUrl = "/Automation/RunData",
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
                            Field = nameof(AutomationRun.Status),
                            Title = FieldsDisplayNames.Status,
                            Type = DataTableColumnType.Link,
                            UrlTemplate = "/Automation/RunDetails/{Id}"
                        },
                        new DataTableColumn
                        {
                            Field = nameof(AutomationRun.TriggerType),
                            Title = FieldsDisplayNames.Type,
                            Type = DataTableColumnType.Text
                        },
                        new DataTableColumn
                        {
                            Field = nameof(AutomationRun.IsDryRun),
                            Title = FieldsDisplayNames.Preview,
                            Type = DataTableColumnType.Boolean
                        },
                        new DataTableColumn
                        {
                            Field = nameof(AutomationRun.CreatedDate),
                            Title = FieldsDisplayNames.CreatedDate,
                            Type = DataTableColumnType.DateTime,
                            Format = "dd.MM.yyyy HH:mm"
                        }
                    },
                    Sorting = new DataTableSortOptions
                    {
                        Enabled = true,
                        DefaultField = nameof(AutomationRun.CreatedDate)
                    },
                    Paging = new DataTablePagingOptions { Enabled = true },
                    Actions = new DataTableActionOptions
                    {
                        Enabled = true,
                        Items = new List<DataTableAction>
                        {
                            new DataTableAction
                            {
                                Name = "view",
                                Title = FieldsDisplayNames.View,
                                Icon = "bx bx-show",
                                Type = DataTableActionType.Link,
                                Placement = DataTableActionPlacement.Row,
                                Url = "/Automation/RunDetails/{Id}",
                                Method = "GET",
                                Permission = PermissionCodes.AutomationHistory
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
