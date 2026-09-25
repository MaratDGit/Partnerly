using Microsoft.AspNetCore.Mvc.Rendering;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Partnerly.Models.ViewModels
{
    public class NotificationsViewModel
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

        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        #endregion
    }
}
