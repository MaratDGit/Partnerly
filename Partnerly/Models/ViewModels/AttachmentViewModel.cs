using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class AttachmentViewModel
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = nameof(FieldsDisplayNames.FileName), ResourceType = typeof(FieldsDisplayNames))]
        public string FileName { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        [Display(Name = nameof(FieldsDisplayNames.StoredFileName), ResourceType = typeof(FieldsDisplayNames))]
        public string StoredFileName { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        [Display(Name = nameof(FieldsDisplayNames.ContentType), ResourceType = typeof(FieldsDisplayNames))]
        public string ContentType { get; set; } = null!;

       
        [Required]
        [Display(Name = nameof(FieldsDisplayNames.FileSize), ResourceType = typeof(FieldsDisplayNames))]
        public long FileSize { get; set; }

      
        [Required]
        [MaxLength(500)]
        [Display(Name = nameof(FieldsDisplayNames.StoragePath), ResourceType = typeof(FieldsDisplayNames))]
        public string StoragePath { get; set; } = null!;

       
        [Required]
        [MaxLength(100)]
        [Display(Name = nameof(FieldsDisplayNames.EntityType), ResourceType = typeof(FieldsDisplayNames))]
        public string EntityType { get; set; } = null!;

       
        [Required]
        public Guid EntityId { get; set; }

       
        public int SortOrder { get; set; }

        #region System Columns

        public string? Note { get; set; }

        [Display(Name = nameof(FieldsDisplayNames.IsDeleted), ResourceType = typeof(FieldsDisplayNames))]
        public bool IsDeleted { get; set; }

        [Required]

        public Guid? CreatedBy { get; set; }

        [Required]
        public DateTime? CreatedDate { get; set; }

        [Required]
        public Guid? UpdatedBy { get; set; }

        [Required]
        public DateTime? UpdatedDate { get; set; }

        #endregion

        public DataTableOptions attachments { get; set; } = new DataTableOptions();

        public static DataTableOptions CreateTableOptions(string tableID, IDataTableRegistry _dataTableRegistry)
        {
            var options = new DataTableOptions();

            options = tableID switch
            {
                DataTableNames.attachmentsTable =>
                   new DataTableOptions
                   {
                       Id = tableID,
                       Name = FieldsDisplayNames.Attachments,
                       Icon = "bx bx-paperclip",

                       KeyField = "Id",
                       AjaxUrl = "/Attachments/Data",
                       ExportUrl = "/Attachments/Export",

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
                                Field = nameof(Attachment.FileName),
                                Title = FieldsDisplayNames.FileName,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Attachment.ContentType),
                                Title = FieldsDisplayNames.ContentType,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Attachment.FileSize),
                                Title = FieldsDisplayNames.FileSize,
                                Type = DataTableColumnType.Size
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Attachment.StoragePath),
                                Title = FieldsDisplayNames.StoragePath,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Attachment.EntityType),
                                Title = FieldsDisplayNames.EntityType,
                            },
                            new DataTableColumn
                            {
                                Field = nameof(Attachment.IsDeleted),
                                Title = FieldsDisplayNames.IsDeleted,
                                Type = DataTableColumnType.Boolean,
                                Alignment = DataTableAlignment.Center,
                            },

                            new DataTableColumn
                            {
                                Field = nameof(Attachment.CreatedDate),
                                Title = FieldsDisplayNames.CreatedDate,
                                Type = DataTableColumnType.DateTime,
                                Format = "dd.MM.yyyy HH:mm",
                                Resizable = true
                            }
                        },

                       Sorting = new DataTableSortOptions
                       {
                           Enabled = true,
                           DefaultField = nameof(Attachment.CreatedDate),
                           DefaultDirection = "desc"
                       },

                       Filters = new DataTableFilterOptions
                       {
                           Enabled = true,
                           Items = new List<DataTableFilter>
                            {
                                new DataTableFilter
                                {
                                    Field = nameof(Attachment.FileSize),
                                    Label = FieldsDisplayNames.FileSize,
                                    Type = DataTableFilterType.NumberRange
                                },
                                new DataTableFilter
                                {
                                    Field =  nameof(Attachment.CreatedDate),
                                    Label = FieldsDisplayNames.CreatedDate,
                                    Type = DataTableFilterType.DateRange
                                },
                                new DataTableFilter
                                {
                                    Field =  nameof(Attachment.IsDeleted),
                                    Label = FieldsDisplayNames.IsDeleted,
                                    Type = DataTableFilterType.Boolean,
                                }
                            }
                       },

                       Actions = new DataTableActionOptions
                       {
                           Enabled = false,
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
