using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class EmailTemplateViewModel : IAuditableViewModel
    {
        public string? Title { get; set; } = "Email Templaete";

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
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Edit",
                        toAction: "Index") { Permission = PermissionCodes.EmailTemplatesView },

                        Undo = new Partnerly.Models.ViewModels.ActionUndo(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Edit",
                        toAction: "Edit") { Permission = PermissionCodes.EmailTemplatesEdit },

                        Save = new Partnerly.Models.ViewModels.ActionSave(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Edit",
                        toAction: "Edit") { Permission = PermissionCodes.EmailTemplatesEdit },

                        Create = new Partnerly.Models.ViewModels.ActionCreate(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Edit",
                        toAction: "Create") { Permission = PermissionCodes.EmailTemplatesCreate },

                        Delete = new Partnerly.Models.ViewModels.ActionDelete(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Edit",
                        toAction: "Delete") { Permission = PermissionCodes.EmailTemplatesDelete },

                        Note = new Partnerly.Models.ViewModels.ActionNote(
                        visible: true,
                        note: Note,
                        id: Id) { Permission = PermissionCodes.EmailTemplatesEdit },

                        Attachements = new Partnerly.Models.ViewModels.ActionAttachements(
                        entityType: "EmailTemplate",
                        entityId: Id,
                        visible: true) { Permission = PermissionCodes.AttachmentsView },

                        Menu = new Partnerly.Models.ViewModels.ActionMenu(
                        visible: true),

                        MenuActions = new List<Partnerly.Models.ViewModels.MenuItemAction>
                        {
                            new Partnerly.Models.ViewModels.MenuItemAction(
                                visible: true,
                                icon: "bx bx-show",
                                displayName: FieldsDisplayNames.Preview,
                                toController: "EmailTemplates",
                                //toAction: isEdit ? "Edit" : "Create",
                                isModal: true,
                                modalTarget: "#previewModal",
                                onClick: "showPreview()")
                        }
                    };
                }

                return new Partnerly.Models.ViewModels.FormActionsViewModel
                {
                    Back = new Partnerly.Models.ViewModels.ActionBack(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Create",
                        toAction: "Index") { Permission = PermissionCodes.EmailTemplatesView },

                    Undo = new Partnerly.Models.ViewModels.ActionUndo(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Create",
                        toAction: "Create") { Permission = PermissionCodes.EmailTemplatesCreate },

                    Save = new Partnerly.Models.ViewModels.ActionSave(
                        id: Id,
                        visible: true,
                        fromController: "EmailTemplates",
                        toController: "EmailTemplates",
                        fromAction: "Create",
                        toAction: "Create") { Permission = PermissionCodes.EmailTemplatesCreate },

                    Menu = new Partnerly.Models.ViewModels.ActionMenu(
                        visible: true),

                    MenuActions = new List<Partnerly.Models.ViewModels.MenuItemAction>
                        {
                            new Partnerly.Models.ViewModels.MenuItemAction(
                                visible: true,
                                icon: "bx bx-show",
                                displayName: FieldsDisplayNames.Preview,
                                toController: "EmailTemplates",
                                //toAction: isEdit ? "Edit" : "Create",
                                isModal: true,
                                modalTarget: "#previewModal",
                                onClick: "showPreview()")
                        }
                };
            }
        }

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
                        Export = new DataTableExportOptions
                        {
                            Enabled = true,
                            Permission = PermissionCodes.EmailTemplatesView
                        },

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
                                Permission = PermissionCodes.DataTableViewIds,
                            },
                            DataTableColumn.HiddenSelected(),
                            new DataTableColumn
                            {
                                Field = nameof(EmailTemplate.Subject),
                                Title = FieldsDisplayNames.Subject,
                                Type = DataTableColumnType.Link,
                                UrlTemplate = "/EmailTemplates/Edit/{Id}",
                                Editable = true,
                                Permission = PermissionCodes.EmailTemplatesEdit,
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
                            },
                            DataTableColumn.HiddenNote()
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
                                    CssClass = "data-table-grid-action",
                                    Permission = PermissionCodes.EmailTemplatesCreate
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
                                    Permission = PermissionCodes.EmailTemplatesEdit
                                },

                                new DataTableAction
                                {
                                    Name = "edit",
                                    Title = FieldsDisplayNames.Edit,
                                    Icon = "bx bx-edit",
                                    Type = DataTableActionType.InlineEdit,
                                    Placement = DataTableActionPlacement.Row,
                                    Permission = PermissionCodes.EmailTemplatesEdit
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
                                    CssClass = "btn btn-sm btn-outline-danger",
                                    Permission = PermissionCodes.EmailTemplatesDelete
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
