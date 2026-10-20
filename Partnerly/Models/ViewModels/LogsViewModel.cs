using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class LogsViewModel : IAuditableViewModel
    {
        public Guid Id { get; set; }

        #region Action
        [Display(Name = nameof(FieldsDisplayNames.Action), ResourceType = typeof(FieldsDisplayNames))]
        public string? Action { get; set; }
        public string? ActionView { get => AttributeDropdownHelper.GetValue<LogActionsAttribute>(Action); }
        #endregion
        #region Type
        [Display(Name = nameof(FieldsDisplayNames.Type), ResourceType = typeof(FieldsDisplayNames))]
        [LogType]
        public string? Type { get; set; }
        public string? TypeView { get => AttributeDropdownHelper.GetValue<LogTypeAttribute>(Type);}
        #endregion
        #region LogMessage
        [Display(Name = nameof(FieldsDisplayNames.Message), ResourceType = typeof(FieldsDisplayNames))]
        public string? LogMessage { get; set; }
        #endregion
        #region FilePath 
        [Display(Name = nameof(FieldsDisplayNames.FilePath), ResourceType = typeof(FieldsDisplayNames))]
        public string? FilePath { get; set; }
        #endregion
        #region Method
        [Display(Name = nameof(FieldsDisplayNames.Method), ResourceType = typeof(FieldsDisplayNames))]
        public string? Method { get; set; }
        #endregion
        #region LineNumber 
        [Display(Name = nameof(FieldsDisplayNames.LineNumber), ResourceType = typeof(FieldsDisplayNames))]
        public int? LineNumber { get; set; }
        #endregion

        #region System Columns
        public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;
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

        public DataTableOptions logs { get; set; } = new DataTableOptions();

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.logsTable =>
                    new DataTableOptions
                    {
                        Id = tableID,
                        Name = FieldsDisplayNames.Logs,
                        Icon = "bx bx-task",

                        KeyField = "Id",
                        AjaxUrl = "/LogRecords/Data",
                        ExportUrl = "/LogRecords/Export",

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
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Log.Action),
                                Title = FieldsDisplayNames.Action,
                                Type = DataTableColumnType.Text,
                                UrlTemplate = "/LogRecords/View/{Id}",

                                ValueMappings = new List<DataTableColumnValueMapping>
                                {
                                    new() { Value = LogActionsAttribute.UserSignIn, Label = FieldsDisplayNames.UserSignIn },
                                    new() { Value = LogActionsAttribute.UserSignOut, Label = FieldsDisplayNames.UserSignOut },

                                    new() { Value = LogActionsAttribute.UserCreated, Label = FieldsDisplayNames.UserCreated },
                                    new() { Value = LogActionsAttribute.UserDeleted, Label = FieldsDisplayNames.UserDeleted },
                                    new() { Value = LogActionsAttribute.UserUpdated, Label = FieldsDisplayNames.UserUpdated },

                                    new() { Value = LogActionsAttribute.RoleCreated, Label = FieldsDisplayNames.RoleCreated },
                                    new() { Value = LogActionsAttribute.RoleDeleted, Label = FieldsDisplayNames.RoleDeleted },
                                    new() { Value = LogActionsAttribute.RoleUpdated, Label = FieldsDisplayNames.RoleUpdated },

                                    new() { Value = LogActionsAttribute.PaymentCreated, Label = FieldsDisplayNames.PaymentCreated },
                                    new() { Value = LogActionsAttribute.PaymentDeleted, Label = FieldsDisplayNames.PaymentDeleted },
                                    new() { Value = LogActionsAttribute.PaymentUpdated, Label = FieldsDisplayNames.PaymentUpdated },

                                    new() { Value = LogActionsAttribute.SystemSettingsCreated, Label = FieldsDisplayNames.SystemSettingsCreated },
                                    new() { Value = LogActionsAttribute.SystemSettingsDeleted, Label = FieldsDisplayNames.SystemSettingsDeleted },
                                    new() { Value = LogActionsAttribute.SystemSettingsUpdated, Label = FieldsDisplayNames.SystemSettingsUpdated },

                                    new() { Value = LogActionsAttribute.EmailConfirmationTokenCreated, Label = FieldsDisplayNames.EmailConfirmationTokenCreated },
                                    new() { Value = LogActionsAttribute.EmailConfirmationTokenDeleted, Label = FieldsDisplayNames.EmailConfirmationTokenDeleted },
                                    new() { Value = LogActionsAttribute.EmailConfirmationTokenUpdated, Label = FieldsDisplayNames.EmailConfirmationTokenUpdated },

                                    new() { Value = LogActionsAttribute.EmailTemplateCreated, Label = FieldsDisplayNames.EmailTemplateCreated },
                                    new() { Value = LogActionsAttribute.EmailTemplateDeleted, Label = FieldsDisplayNames.EmailTemplateDeleted },
                                    new() { Value = LogActionsAttribute.EmailTemplateUpdated, Label = FieldsDisplayNames.EmailTemplateUpdated },

                                    new() { Value = LogActionsAttribute.NotificationCreated, Label = FieldsDisplayNames.NotificationCreated },
                                    new() { Value = LogActionsAttribute.NotificationDeleted, Label = FieldsDisplayNames.NotificationDeleted },
                                    new() { Value = LogActionsAttribute.NotificationUpdated, Label = FieldsDisplayNames.NotificationUpdated },

                                    new() { Value = LogActionsAttribute.UserGroupCreated, Label = FieldsDisplayNames.UserGroupCreated },
                                    new() { Value = LogActionsAttribute.UserGroupDeleted, Label = FieldsDisplayNames.UserGroupDeleted },
                                    new() { Value = LogActionsAttribute.UserGroupUpdated, Label = FieldsDisplayNames.UserGroupUpdated },

                                    new() { Value = LogActionsAttribute.SupportTicketCreated, Label = FieldsDisplayNames.SupportTicketCreated },
                                    new() { Value = LogActionsAttribute.SupportTicketDeleted, Label = FieldsDisplayNames.SupportTicketDeleted },
                                    new() { Value = LogActionsAttribute.SupportTicketUpdated, Label = FieldsDisplayNames.SupportTicketUpdated },

                                    new() { Value = LogActionsAttribute.EmailSending, Label = FieldsDisplayNames.EmailSending }
                                },

                            },
                            new DataTableColumn
                            {
                                Field = nameof(Log.Type),
                                Title = FieldsDisplayNames.Type,
                                Type = DataTableColumnType.Text,
                                ValueMappings = new List<DataTableColumnValueMapping>
                                {
                                    new()
                                    {
                                        Value = LogTypeAttribute.Information,
                                        Label = FieldsDisplayNames.Information
                                    },
                                    new()
                                    {
                                        Value = LogTypeAttribute.Error,
                                        Label = FieldsDisplayNames.Error
                                    },
                                    new()
                                    {
                                        Value = LogTypeAttribute.Warning,
                                        Label = FieldsDisplayNames.Warning
                                    },
                                    new()
                                    {
                                        Value = LogTypeAttribute.Critical,
                                        Label = FieldsDisplayNames.Critical
                                    }
                                },
                                ConditionalStyles = new List<DataTableConditionalStyle>
                                {
                                    new DataTableConditionalStyle
                                    {
                                        Conditions = new List<DataTableConditionalCondition>
                                        {
                                            new DataTableConditionalCondition
                                            {
                                                Operator = DataTableConditionalOperator.Equals,
                                                Value = FieldsDisplayNames.Warning
                                            },
                                        },
                                        CssClass = "badge bg-label-warning fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                    new DataTableConditionalStyle
                                    {
                                        Conditions = new List<DataTableConditionalCondition>
                                        {
                                            new DataTableConditionalCondition
                                            {
                                                Operator = DataTableConditionalOperator.Equals,
                                                Value = LogTypeAttribute.Error
                                            },
                                        },
                                        CssClass = "badge bg-label-warning fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                    new DataTableConditionalStyle
                                    {
                                        Conditions = new List<DataTableConditionalCondition>
                                        {
                                            new DataTableConditionalCondition
                                            {
                                                Operator = DataTableConditionalOperator.Equals,
                                                Value = LogTypeAttribute.Critical
                                            },
                                        },
                                        CssClass = "badge bg-label-danger fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                    new DataTableConditionalStyle
                                    {
                                        Conditions = new List<DataTableConditionalCondition>
                                        {
                                            new DataTableConditionalCondition
                                            {
                                                Operator = DataTableConditionalOperator.Equals,
                                                Value = LogTypeAttribute.Information
                                            },
                                        },

                                        CssClass = "badge bg-label-success fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                },

                            },
                            new DataTableColumn
                            {
                                Field = nameof(Log.CreatedDate),
                                Title = FieldsDisplayNames.CreatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "dd.MM.yyyy HH:mm",
                            }
                        },

                        Sorting = new DataTableSortOptions
                        {
                            Enabled = true,
                            DefaultField = "CreatedDate",
                        },

                        Filters = new DataTableFilterOptions
                        {
                            Enabled = true,
                            Items = new List<DataTableFilter>
                            {
                                new DataTableFilter
                                {
                                    Field = "CreatedDate",
                                    Label = FieldsDisplayNames.CreatedDate,
                                    Type = DataTableFilterType.DateRange
                                },
                                new DataTableFilter
                                {
                                    Field = nameof(Log.Type),
                                    Label = FieldsDisplayNames.Type,
                                    Type = DataTableFilterType.Select,
                                    DefaultValue = "",
                                    Options = new List<DataTableFilterOption>
                                    {
                                        new DataTableFilterOption(LogTypeAttribute.Information, FieldsDisplayNames.Information),
                                        new DataTableFilterOption(LogTypeAttribute.Warning, FieldsDisplayNames.Warning),
                                        new DataTableFilterOption(LogTypeAttribute.Error, FieldsDisplayNames.Error),
                                         new DataTableFilterOption(LogTypeAttribute.Critical, FieldsDisplayNames.Critical)
                                    }
                                }
                            }
                        },

                        Paging = new DataTablePagingOptions
                        {
                            Enabled = true,
                        },

                        Actions = new DataTableActionOptions
                        {
                            Enabled = true,
                            Items = new List<DataTableAction>
                            {
                                new DataTableAction
                                {
                                    Name = "view",
                                    Title = FieldsDisplayNames.Details,
                                    Icon = "bx bx-show",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/LogRecords/View/{Id}",
                                    Method = "GET",
                                },
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
