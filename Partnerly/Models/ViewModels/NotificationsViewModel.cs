using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class NotificationsViewModel : IAuditableViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.CreatorName), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatorName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.Owner), ResourceType = typeof(FieldsDisplayNames))]
        public string? OwnerName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.Message), ResourceType = typeof(FieldsDisplayNames))]
        public string? Message { get; set; }

        [NotificationType]
        [Display(Name = nameof(FieldsDisplayNames.Type), ResourceType = typeof(FieldsDisplayNames))]
        public string? Type { get; set; }
        //public List<SelectListItem> TypesList { get => AttributeDropdownHelper.ToSelectedList(NotificationTypeAttribute.TypeDisplayNames); }

        [Display(Name = nameof(FieldsDisplayNames.IsRead), ResourceType = typeof(FieldsDisplayNames))]
        public bool IsRead { get; set; } = false;
        public bool IsReadBool { get => IsRead; set => IsRead = value; }

        [Display(Name = nameof(FieldsDisplayNames.TaskNumber), ResourceType = typeof(FieldsDisplayNames))]
        public Guid? TicketID { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.Link), ResourceType = typeof(FieldsDisplayNames))]
        public string? Link { get; set; }

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

        public DataTableOptions notifications { get; set; } = new DataTableOptions();

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.notificationsTable =>
                    new DataTableOptions
                    {
                        Id = tableID,
                        Name = FieldsDisplayNames.Notifications,
                        Icon = "bx bx-bell",

                        KeyField = "Id",
                        AjaxUrl = "/Notification/Data",
                        ExportUrl = "/Notification/Export",

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
                                Field = nameof(Notification.Message),
                                Title = FieldsDisplayNames.Action,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/Notification/Edit/{Id}",
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Notification.Type),
                                Title = FieldsDisplayNames.Type,
                                Type = DataTableColumnType.Text,
                                ValueMappings = new List<DataTableColumnValueMapping>
                                {
                                    new()
                                    {
                                        Value = NotificationTypeAttribute.Information,
                                        Label = FieldsDisplayNames.Information
                                    },
                                    new()
                                    {
                                        Value = NotificationTypeAttribute.Error,
                                        Label = FieldsDisplayNames.Error
                                    },
                                    new()
                                    {
                                        Value = NotificationTypeAttribute.Warning,
                                        Label = FieldsDisplayNames.Warning
                                    },
                                    new()
                                    {
                                        Value = NotificationTypeAttribute.Success,
                                        Label = FieldsDisplayNames.Success
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
                                                Value = NotificationTypeAttribute.Warning
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
                                                Value = NotificationTypeAttribute.Error
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
                                                Value = NotificationTypeAttribute.Success
                                            },
                                        },
                                        CssClass = "badge bg-label-success fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                    new DataTableConditionalStyle
                                    {
                                        Conditions = new List<DataTableConditionalCondition>
                                        {
                                            new DataTableConditionalCondition
                                            {
                                                Operator = DataTableConditionalOperator.Equals,
                                                Value = NotificationTypeAttribute.Information
                                            },
                                        },

                                        CssClass = "badge bg-label-success fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                },

                            },
                            new DataTableColumn
                            {
                                Field = nameof(Notification.IsRead),
                                Title = FieldsDisplayNames.IsRead,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Notification.CreatedDate),
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
                                    Field = nameof(Notification.IsRead),
                                    Label = FieldsDisplayNames.IsRead,
                                    Type = DataTableFilterType.Boolean
                                },
                                new DataTableFilter
                                {
                                    Field = nameof(Notification.Type),
                                    Label = FieldsDisplayNames.Type,
                                    Type = DataTableFilterType.Select,
                                    DefaultValue = "",
                                    Options = new List<DataTableFilterOption>
                                    {
                                        new DataTableFilterOption(NotificationTypeAttribute.Success, FieldsDisplayNames.Success),
                                        new DataTableFilterOption(NotificationTypeAttribute.Information, FieldsDisplayNames.Information),
                                        new DataTableFilterOption(NotificationTypeAttribute.Warning, FieldsDisplayNames.Warning),
                                        new DataTableFilterOption(NotificationTypeAttribute.Error, FieldsDisplayNames.Error),
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
                                    Name = "add",
                                    Title = FieldsDisplayNames.Create,
                                    Icon = "bx bx-plus",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Toolbar,
                                    Url = "/Notification/Create",
                                    Method = "Get",
                                    CssClass = "data-table-grid-action"
                                },

                                new DataTableAction
                                {
                                    Name = "view",
                                    Title = FieldsDisplayNames.View,
                                    Icon = "bx bx-show",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Notification/Edit/{Id}",
                                    Method = "GET",
                                },

                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/Notification/Delete/{Id}",
                                    Method = "GET",
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
