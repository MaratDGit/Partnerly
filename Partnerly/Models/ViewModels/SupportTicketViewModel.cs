using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class SupportTicketViewModel : IAuditableViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid? UserId { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.User), ResourceType = typeof(FieldsDisplayNames))]
        public string? CreatorName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.TaskNumber), ResourceType = typeof(FieldsDisplayNames))]
        public string? TicketID { get; set; }

        [CustomStringLenght(max:50)]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Subject), ResourceType = typeof(FieldsDisplayNames))]
        public string? Subject { get; set; }

        [SupportTicketStatus]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Message), ResourceType = typeof(FieldsDisplayNames))]
        public string? Message { get; set; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Status), ResourceType = typeof(FieldsDisplayNames))]
        public string? Status { get; set; }
        public string? StatusAsView { get => AttributeDropdownHelper.GetValue<SupportTicketStatusAttribute>(Status);}
        public List<SelectListItem> TicketStatuses { get => AttributeDropdownHelper.FromAttribute<SupportTicketStatusAttribute>(); }

        public Guid? AssignedTo { get; set; }
        [Display(Name = nameof(FieldsDisplayNames.AssignedTo), ResourceType = typeof(FieldsDisplayNames))]
        public string? AssignedToName { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.IsRead), ResourceType = typeof(FieldsDisplayNames))]
        public bool? IsRead { get; set; } = false;
        public bool IsReadBool { get => IsRead ?? false; set => IsRead = value; }

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

        public DataTableOptions tickets { get; set; } = new DataTableOptions();
        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.supportTicketsTable =>
                    new DataTableOptions
                    {
                        Id = tableID,
                        Name = FieldsDisplayNames.Tasks,
                        Icon = "bx bx-task",

                        KeyField = "Id",
                        AjaxUrl = "/SupportTickets/Data",
                        ExportUrl = "/SupportTickets/Export",

                        AutoLoad = true,
                        ReloadEnabled = true,
                        RememberState = true,

                        InlineEditing = true,
                        InlineUpdateUrl = "/SupportTickets/UpdateInline",

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
                                Field = nameof(SupportTicket.TicketID),
                                Title = FieldsDisplayNames.TaskNumber,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/SupportTickets/Edit/{Id}",
                            },
                            new DataTableColumn
                            {
                                Field = nameof(SupportTicket.Subject),
                                Title = FieldsDisplayNames.Subject,
                                Type = DataTableColumnType.Text,
                                Editable = true,
                            },
                            
                            new DataTableColumn
                            {
                                Field = nameof(SupportTicket.Status),
                                Title = FieldsDisplayNames.Status,
                                Type = DataTableColumnType.Text,
                                ValueMappings = new List<DataTableColumnValueMapping>
                                {
                                    new()
                                    {
                                        Value = SupportTicketStatusAttribute.New,
                                        Label = FieldsDisplayNames.New
                                    },
                                    new()
                                    {
                                        Value = SupportTicketStatusAttribute.InProgress,
                                        Label = FieldsDisplayNames.InProgress
                                    },
                                    new()
                                    {
                                        Value = SupportTicketStatusAttribute.Closed,
                                        Label = FieldsDisplayNames.Closed
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
                                                Value = SupportTicketStatusAttribute.InProgress
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
                                                Value = SupportTicketStatusAttribute.New
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
                                                Value = SupportTicketStatusAttribute.Closed
                                            },
                                        },

                                        CssClass = "badge bg-label-success fs-7 rounded-pill ms-auto text-uppercase",
                                    },
                                },

                            },
                            new DataTableColumn
                            {
                                Field = nameof(SupportTicketExt.CreatorName),
                                Title = FieldsDisplayNames.CreatorName,
                                Type = DataTableColumnType.Text,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(SupportTicketExt.AssignedToName),
                                Title = FieldsDisplayNames.AssignedTo,
                                Type = DataTableColumnType.Text,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(SupportTicket.UpdatedDate),
                                Title = FieldsDisplayNames.UpdatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "relative",
                            },
                            new DataTableColumn
                            {
                                Field = nameof(SupportTicket.CreatedDate),
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
                                    Field = nameof(SupportTicket.Status),
                                    Label = FieldsDisplayNames.Status,
                                    Type = DataTableFilterType.MultiSelect,
                                    DefaultValue = "",
                                    Options = new List<DataTableFilterOption>
                                    {
                                        new DataTableFilterOption(SupportTicketStatusAttribute.New, FieldsDisplayNames.New),
                                        new DataTableFilterOption(SupportTicketStatusAttribute.InProgress, FieldsDisplayNames.InProgress),
                                        new DataTableFilterOption(SupportTicketStatusAttribute.Closed, FieldsDisplayNames.Closed)
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
                                    Url = "/SupportTickets/Create",
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
                                    Url = "/SupportTickets/ViewCase/{Id}",
                                    Method = "GET",
                                },

                                new DataTableAction
                                {
                                    Name = "edit",
                                    Title = FieldsDisplayNames.Edit,
                                    Icon = "bx bx-edit",
                                    Type = DataTableActionType.InlineEdit,
                                    Placement = DataTableActionPlacement.Row,
                                },

                                new DataTableAction
                                {
                                    Name = "delete",
                                    Title = FieldsDisplayNames.Delete,
                                    Icon = "bx bx-trash",
                                    Type = DataTableActionType.Link,
                                    Placement = DataTableActionPlacement.Row,
                                    Url = "/SupportTickets/Delete/{Id}",
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
