using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class EmailTemplateViewModel : IAuditableViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [EmailTemplateName]
        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.TemplateType), ResourceType = typeof(FieldsDisplayNames))]
        public string? Name { get; set; }
        public string? NameView { get => AttributeDropdownHelper.GetValue<EmailTemplateNameAttribute>(Name) ?? Name; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.Subject), ResourceType = typeof(FieldsDisplayNames))]
        public string? Subject { get; set; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FieldRequired))]
        [Display(Name = nameof(FieldsDisplayNames.BodyHTML), ResourceType = typeof(FieldsDisplayNames))]
        public string? BodyHtml { get; set; }

        public string? BodyPlain { get; set; }
        public string? AttachmentsMeta { get; set; }
        public string? Note { get; set; }

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

        public DataTableOptions templates { get; set; } = new DataTableOptions();

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.emailTemplatesTable =>
                    new DataTableOptions
                    {
                        Id = tableID,
                        Name = FieldsDisplayNames.Templates,
                        Icon = "bx bx-envelope",

                        KeyField = "Id",
                        AjaxUrl = "/EmailTemplates/Data",
                        ExportUrl = "/EmailTemplates/Export",

                        AutoLoad = true,
                        ReloadEnabled = true,
                        RememberState = true,

                        InlineEditing = true,
                        InlineUpdateUrl = "/EmailTemplates/UpdateInline",

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
                                Field = nameof(EmailTemplate.Subject),
                                Title = FieldsDisplayNames.Subject,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/EmailTemplates/Edit/{Id}",
                                Editable = true,
                            },
                            
                            new DataTableColumn
                            {
                                Field = nameof(EmailTemplate.UpdatedDate),
                                Title = FieldsDisplayNames.UpdatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "relative",
                            },
                            new DataTableColumn
                            {
                                Field = nameof(EmailTemplate.CreatedDate),
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
                                    Url = "/EmailTemplates/Create",
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
                                    Url = "/EmailTemplates/Edit/{Id}",
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
                                    Url = "/EmailTemplates/Delete/{Id}",
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
