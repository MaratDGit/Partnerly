using Partnerly.Descriptors.Messages;
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
    }
}
